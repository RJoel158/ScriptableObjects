using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossPhase
{
    Phase1_MeleeAssault,     // 100% -> 70% HP: Tanque cuerpo a cuerpo y pisotón sísmico
    Phase2_SpikeStorm,       // 70% -> 35% HP: Ataques de espinas a distancia y flanqueo táctico
    Phase3_BerserkerEnraged  // < 35% HP: Furia Carmesí, velocidad extrema y Nova de espinas 360°
}

public class MutantBossController : EnemyController
{
    [Header("Boss Phase System")]
    [SerializeField] private BossPhase currentPhase = BossPhase.Phase1_MeleeAssault;
    [SerializeField] private float jumpAttackCooldown = 7.5f;
    [SerializeField] private float spikeAttackCooldown = 3.5f;

    [Header("Spike Launch Points (Big Spikes & Arms)")]
    [Tooltip("Punto de origen de la espina gigante izquierda")]
    [SerializeField] private Transform leftBigSpike;
    [Tooltip("Punto de origen de la espina gigante derecha")]
    [SerializeField] private Transform rightBigSpike;
    [Tooltip("Punto de origen de la mano/brazo izquierdo")]
    [SerializeField] private Transform leftHandSpike;
    [Tooltip("Punto de origen de la mano/brazo derecho")]
    [SerializeField] private Transform rightHandSpike;
    [Tooltip("Todos los puntos de espinas detectados en el modelo")]
    [SerializeField] private List<Transform> allSpikePoints = new List<Transform>();

    private bool isPerformingSpecialAction = false;
    private float nextJumpTime = 0f;
    private float nextSpikeTime = 0f;
    private float strafeTimer = 0f;
    private float strafeDirection = 1f;

    // Animator Hashes
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RageHash = Animator.StringToHash("Rage");
    private static readonly int SpikeAttackHash = Animator.StringToHash("SpikeAttack");
    private static readonly int StrafeHash = Animator.StringToHash("Strafe");
    private static readonly int PhaseHash = Animator.StringToHash("Phase");
    private static readonly int DeathIndexHash = Animator.StringToHash("DeathIndex");

    public BossPhase CurrentPhase => currentPhase;

    [ContextMenu("⚙️ Auto-Vincular Puntos de Espinas y Animator")]
    public void AutoBindComponentsAndSpikePoints()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (animator == null) animator = GetComponent<Animator>();

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        allSpikePoints.Clear();

        foreach (var t in allChildren)
        {
            string n = t.name.ToLower();
            if (leftBigSpike == null && (n.Contains("left_big_spike4") || n.Contains("left_big_spike3") || n.Contains("left_big_spike") || n.Contains("lspike") || n.Contains("spike_l")))
            {
                leftBigSpike = t;
            }
            if (rightBigSpike == null && (n.Contains("right_big_spike4") || n.Contains("right_big_spike3") || n.Contains("right_big_spike") || n.Contains("rspike") || n.Contains("spike_r")))
            {
                rightBigSpike = t;
            }
            if (leftHandSpike == null && (n.Contains("left_hand") || n.Contains("hand_l") || n.Contains("left_forearm")))
            {
                leftHandSpike = t;
            }
            if (rightHandSpike == null && (n.Contains("right_hand") || n.Contains("hand_r") || n.Contains("right_forearm")))
            {
                rightHandSpike = t;
            }

            if (n.Contains("big_spike") || n.Contains("small_spike") || n.Contains("spike"))
            {
                if (!allSpikePoints.Contains(t)) allSpikePoints.Add(t);
            }
        }

        if (leftBigSpike != null && !allSpikePoints.Contains(leftBigSpike)) allSpikePoints.Add(leftBigSpike);
        if (rightBigSpike != null && !allSpikePoints.Contains(rightBigSpike)) allSpikePoints.Add(rightBigSpike);
        if (leftHandSpike != null && !allSpikePoints.Contains(leftHandSpike)) allSpikePoints.Add(leftHandSpike);
        if (rightHandSpike != null && !allSpikePoints.Contains(rightHandSpike)) allSpikePoints.Add(rightHandSpike);
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        if (leftBigSpike == null || rightBigSpike == null || animator == null)
        {
            AutoBindComponentsAndSpikePoints();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        heightOffset = (enemyData != null && enemyData.groundYOffset > 0f) ? enemyData.groundYOffset : 0.65f;
        if (leftBigSpike == null || rightBigSpike == null || animator == null)
        {
            AutoBindComponentsAndSpikePoints();
        }
    }

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BossIntroRoarRoutine());
    }

    private IEnumerator BossIntroRoarRoutine()
    {
        isPerformingSpecialAction = true;
        UpdateAnimation(0f);

        if (HasValidAnimator)
        {
            animator.SetTrigger(RageHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("🔥 ¡EL JEFE MUTANTE HA ENTRADO AL COMBATE!");
        }

        yield return new WaitForSeconds(2.0f);
        isPerformingSpecialAction = false;
        nextJumpTime = Time.time + 4f;
        nextSpikeTime = Time.time + 3f;
    }

    protected override void Update()
    {
        if (isDead || isPerformingSpecialAction) return;

        if (targetPlayer == null)
        {
            AutoAssignPlayerTarget();
            if (targetPlayer == null)
            {
                UpdateAnimation(0f);
                return;
            }
        }

        // Mantener altura sobre el suelo
        float baseOffset = (enemyData != null) ? enemyData.groundYOffset : 0.65f;
        Vector3 currentPos = transform.position;
        currentPos.y = baseOffset + heightOffset;

        float distanceToPlayer = Vector3.Distance(currentPos, targetPlayer.position);
        Vector3 dirToPlayer = (targetPlayer.position - currentPos).normalized;
        dirToPlayer.y = 0;

        // Rotación fluida hacia el jugador
        if (dirToPlayer.sqrMagnitude > 0.001f)
        {
            float rotSpeed = currentPhase == BossPhase.Phase3_BerserkerEnraged ? 12f : 7f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dirToPlayer), Time.deltaTime * rotSpeed);
        }

        // Ejecutar comportamiento según la fase activa
        switch (currentPhase)
        {
            case BossPhase.Phase1_MeleeAssault:
                UpdatePhase1(currentPos, dirToPlayer, distanceToPlayer);
                break;
            case BossPhase.Phase2_SpikeStorm:
                UpdatePhase2(currentPos, dirToPlayer, distanceToPlayer);
                break;
            case BossPhase.Phase3_BerserkerEnraged:
                UpdatePhase3(currentPos, dirToPlayer, distanceToPlayer);
                break;
        }
    }

    #region Phase 1 - Asedio Colosal (100% -> 70% HP)
    private void UpdatePhase1(Vector3 currentPos, Vector3 dirToPlayer, float distanceToPlayer)
    {
        // Si el jugador se aleja a media distancia (6m - 14m), ejecutar Salto Sísmico
        if (distanceToPlayer >= 6f && distanceToPlayer <= 14f && Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpSlamRoutine(novaSpikes: false));
            return;
        }

        // Persecución cuerpo a cuerpo estándar
        if (distanceToPlayer > attackRange)
        {
            transform.position = currentPos + dirToPlayer * (currentMoveSpeed * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed);
        }
        else
        {
            transform.position = currentPos;
            UpdateAnimation(0f);
            TryAttackPlayer();
        }
    }
    #endregion

    #region Phase 2 - Tormenta de Espinas y Flanqueo (70% -> 35% HP)
    private void UpdatePhase2(Vector3 currentPos, Vector3 dirToPlayer, float distanceToPlayer)
    {
        // Lanzamiento de espinas a distancia
        if (distanceToPlayer >= 3.5f && Time.time >= nextSpikeTime)
        {
            StartCoroutine(SpikeAttackRoutine(tripleSpread: Random.value < 0.65f));
            return;
        }

        // Salto sísmico si el jugador se aleja demasiado
        if (distanceToPlayer >= 9f && Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpSlamRoutine(novaSpikes: false));
            return;
        }

        // Si el jugador está en rango óptimo de distancia (4m - 10m), realizar flanqueo lateral (Strafe)
        if (distanceToPlayer >= 4f && distanceToPlayer <= 11f)
        {
            strafeTimer -= Time.deltaTime;
            if (strafeTimer <= 0f)
            {
                strafeTimer = Random.Range(1.8f, 3.2f);
                strafeDirection = (Random.value < 0.5f) ? -1f : 1f;
            }

            Vector3 strafeDir = Vector3.Cross(Vector3.up, dirToPlayer) * strafeDirection;
            Vector3 finalMove = (strafeDir * 0.85f + dirToPlayer * 0.25f).normalized;

            transform.position = currentPos + finalMove * (currentMoveSpeed * 0.85f * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed * 0.85f);
            if (HasValidAnimator) animator.SetFloat(StrafeHash, strafeDirection);
        }
        else if (distanceToPlayer < 3.0f)
        {
            // Si el jugador se pega demasiado, retroceder disparando (Backpedal) o dar golpe cuerpo a cuerpo
            if (Random.value < 0.4f)
            {
                transform.position = currentPos;
                UpdateAnimation(0f);
                TryAttackPlayer();
            }
            else
            {
                transform.position = currentPos - dirToPlayer * (currentMoveSpeed * 0.6f * Time.deltaTime);
                UpdateAnimation(currentMoveSpeed * 0.6f);
            }
        }
        else
        {
            transform.position = currentPos + dirToPlayer * (currentMoveSpeed * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed);
        }
    }
    #endregion

    #region Phase 3 - Berserker Carmesí (< 35% HP)
    private void UpdatePhase3(Vector3 currentPos, Vector3 dirToPlayer, float distanceToPlayer)
    {
        // En Fase Berserker alterna Jump Slam con Nova de espinas 360° y ráfagas rápidas
        if (distanceToPlayer >= 5f && Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpSlamRoutine(novaSpikes: true));
            return;
        }

        if (distanceToPlayer >= 3.5f && Time.time >= nextSpikeTime)
        {
            StartCoroutine(SpikeAttackRoutine(tripleSpread: true));
            return;
        }

        // Sprint desbocado hacia el jugador
        if (distanceToPlayer > attackRange)
        {
            transform.position = currentPos + dirToPlayer * (currentMoveSpeed * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed);
        }
        else
        {
            transform.position = currentPos;
            UpdateAnimation(0f);
            TryAttackPlayer();
        }
    }
    #endregion

    #region Special Boss Abilities (Jump Slam, Spike Attack, Nova)
    private IEnumerator JumpSlamRoutine(bool novaSpikes)
    {
        isPerformingSpecialAction = true;
        nextJumpTime = Time.time + jumpAttackCooldown;
        UpdateAnimation(0f);

        if (HasValidAnimator)
        {
            animator.SetTrigger(JumpHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowNotification(novaSpikes ? "🚨 ¡SALTO SÍSMICO + NOVA DE ESPINAS DEL JEFE!" : "⚠️ ¡SALTO SÍSMICO DEL JEFE!");

        // Preparación del salto
        yield return new WaitForSeconds(0.55f);

        // Desplazamiento en el aire hacia la posición del jugador
        Vector3 jumpTarget = targetPlayer != null ? targetPlayer.position : transform.position;
        jumpTarget.y = (enemyData != null ? enemyData.groundYOffset : 0.65f) + heightOffset;
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        float jumpDuration = 0.45f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, jumpTarget, elapsed / jumpDuration);
            yield return null;
        }

        // Impacto sísmico en el suelo: Onda expansiva en área
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 5f);
        foreach (var col in hitColliders)
        {
            if (col != null && col.CompareTag("Player"))
            {
                PlayerHealth pHealth = col.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(Mathf.RoundToInt(currentDamage * 1.4f), transform.position, (col.transform.position - transform.position).normalized);
                }
            }
        }

        // Si está en fase berserker, lanzar Nova de espinas 360° desde las Big Spikes
        if (novaSpikes)
        {
            FireSpikeNova(12, Mathf.RoundToInt(currentDamage * 0.9f));
        }

        yield return new WaitForSeconds(0.5f);
        isPerformingSpecialAction = false;
    }

    private IEnumerator SpikeAttackRoutine(bool tripleSpread)
    {
        isPerformingSpecialAction = true;
        nextSpikeTime = Time.time + (currentPhase == BossPhase.Phase3_BerserkerEnraged ? 2.2f : spikeAttackCooldown);
        UpdateAnimation(0f);

        if (HasValidAnimator)
        {
            animator.SetTrigger(SpikeAttackHash);
        }

        yield return new WaitForSeconds(0.45f);

        if (!isDead && targetPlayer != null)
        {
            Vector3 leftOrigin = GetLeftSpikeOrigin();
            Vector3 rightOrigin = GetRightSpikeOrigin();

            Vector3 baseDirLeft = (targetPlayer.position + Vector3.up * 1.0f - leftOrigin).normalized;
            Vector3 baseDirRight = (targetPlayer.position + Vector3.up * 1.0f - rightOrigin).normalized;

            if (tripleSpread)
            {
                // Disparo simultáneo desde la Big Spike Izquierda y Big Spike Derecha en cono
                FireSpike(leftOrigin, baseDirLeft);
                FireSpike(rightOrigin, baseDirRight);
                FireSpike(leftOrigin, Quaternion.Euler(0, -18, 0) * baseDirLeft);
                FireSpike(rightOrigin, Quaternion.Euler(0, 18, 0) * baseDirRight);
            }
            else
            {
                // Alternar o disparar doble espina directa desde las dos Big Spikes
                FireSpike(leftOrigin, baseDirLeft);
                FireSpike(rightOrigin, baseDirRight);
            }
        }

        yield return new WaitForSeconds(0.45f);
        isPerformingSpecialAction = false;
    }

    private Vector3 GetLeftSpikeOrigin()
    {
        if (leftBigSpike != null) return leftBigSpike.position;
        if (leftHandSpike != null) return leftHandSpike.position;
        return transform.position + transform.forward * 1.1f - transform.right * 0.6f + Vector3.up * 1.9f;
    }

    private Vector3 GetRightSpikeOrigin()
    {
        if (rightBigSpike != null) return rightBigSpike.position;
        if (rightHandSpike != null) return rightHandSpike.position;
        return transform.position + transform.forward * 1.1f + transform.right * 0.6f + Vector3.up * 1.9f;
    }

    private void FireSpike(Vector3 origin, Vector3 direction)
    {
        Color spikeColor = currentPhase == BossPhase.Phase3_BerserkerEnraged 
            ? new Color(1f, 0.15f, 0.15f) 
            : new Color(0.2f, 0.9f, 0.4f);

        SpikeProjectile.CreateSpike(origin, direction, Mathf.RoundToInt(currentDamage * 0.85f), 19f, spikeColor);
    }

    private void FireSpikeNova(int count, int damage)
    {
        Vector3 leftOrigin = GetLeftSpikeOrigin();
        Vector3 rightOrigin = GetRightSpikeOrigin();
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 origin = (i % 2 == 0) ? leftOrigin : rightOrigin;
            FireSpike(origin, dir);
        }
    }
    #endregion

    #region Phase Transitions & Damage Handling
    public override void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        int finalDamage = amount;
        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.TriggerHitmarker(false);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Verificar cambio de fase
        CheckPhaseTransitions();

        if (HasValidAnimator && !isPerformingSpecialAction && Random.value < 0.2f)
        {
            animator.SetTrigger(HitHash);
        }

        StartCoroutine(HitFlashCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void CheckPhaseTransitions()
    {
        float healthPct = (float)currentHealth / maxHealth;

        if (currentPhase == BossPhase.Phase1_MeleeAssault && healthPct <= 0.70f && healthPct > 0.35f)
        {
            // Transición a Fase 2 (Tormenta de Espinas)
            currentPhase = BossPhase.Phase2_SpikeStorm;
            currentMoveSpeed *= 1.15f;
            attackCooldown = Mathf.Max(attackCooldown * 0.85f, 1.1f);
            if (HasValidAnimator) animator.SetInteger(PhaseHash, 2);
            StartCoroutine(PhaseTransitionRoarRoutine("⚡ ¡FASE 2: EL JEFE MUTANTE DESATA LA TORMENTA DE ESPINAS!"));
        }
        else if (currentPhase != BossPhase.Phase3_BerserkerEnraged && healthPct <= 0.35f && healthPct > 0f)
        {
            // Transición a Fase 3 (Berserker Carmesí)
            currentPhase = BossPhase.Phase3_BerserkerEnraged;
            currentMoveSpeed *= 1.35f;
            currentDamage = Mathf.RoundToInt(currentDamage * 1.3f);
            attackCooldown = Mathf.Max(attackCooldown * 0.7f, 0.8f);
            jumpAttackCooldown = 5.0f;
            if (HasValidAnimator) animator.SetInteger(PhaseHash, 3);
            StartCoroutine(PhaseTransitionRoarRoutine("🔥 ¡FASE 3: FURIA BERSERKER CARMESÍ DESATADA!"));
        }
    }

    private IEnumerator PhaseTransitionRoarRoutine(string notification)
    {
        isPerformingSpecialAction = true;
        UpdateAnimation(0f);

        if (HasValidAnimator)
        {
            animator.SetTrigger(RageHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowNotification(notification);

        // Tinte visual para la fase
        if (enemyRenderers != null && enemyRenderers.Length > 0 && enemyRenderers[0] != null)
        {
            Color tintColor = (currentPhase == BossPhase.Phase3_BerserkerEnraged) 
                ? new Color(1f, 0.25f, 0.25f) 
                : new Color(0.9f, 0.8f, 0.3f);

            enemyRenderers[0].material.color = tintColor;
            originalColor = tintColor;
        }

        yield return new WaitForSeconds(2.0f);
        isPerformingSpecialAction = false;
        nextJumpTime = Time.time + 3.5f;
        nextSpikeTime = Time.time + 1.5f;
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        UpdateAnimation(0f);

        // Selección aleatoria entre 4 animaciones de muerte diferentes
        if (HasValidAnimator)
        {
            int deathChoice = Random.Range(1, 5); // 1..4
            animator.SetInteger(DeathIndexHash, deathChoice);
            animator.SetTrigger(DieHash);
        }

        DropLoot();

        WaveManager waveMgr = FindAnyObjectByType<WaveManager>();
        if (waveMgr != null)
        {
            waveMgr.OnEnemyDefeated(this);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("🏆 ¡JEFE MUTANTE ANIQUILADO! Recompensas Épicas Liberadas");
        }

        Destroy(gameObject, 6.0f);
    }
    #endregion
}
