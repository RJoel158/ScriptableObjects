using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Data & Configuration")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform targetPlayer;
    [Tooltip("Ajuste manual de altura sobre el suelo: súbelo o bájalo si el modelo se hunde o flota")]
    public float heightOffset = 0f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Scaled Stats")]
    private int currentHealth;
    private int maxHealth;
    private int currentDamage;
    private float currentMoveSpeed;
    private float attackRange;
    private float attackCooldown;

    private float nextAttackTime = 0f;
    private bool isDead = false;
    private bool isCrawling = false;
    private bool isScreaming = false;
    private Renderer[] enemyRenderers;
    private Color originalColor;

    public bool IsDead => isDead;
    public bool IsCrawling => isCrawling;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Animator Param Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsCrawlingHash = Animator.StringToHash("isCrawling");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int BiteHash = Animator.StringToHash("Bite");
    private static readonly int ScreamHash = Animator.StringToHash("Scream");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null && enemyData != null)
        {
            if (enemyData.animatorController != null)
            {
                animator.runtimeAnimatorController = enemyData.animatorController;
            }
            if (enemyData.enemyAvatar != null)
            {
                animator.avatar = enemyData.enemyAvatar;
            }
            animator.applyRootMotion = false;
        }
        enemyRenderers = GetComponentsInChildren<Renderer>();
        if (enemyRenderers.Length > 0 && enemyRenderers[0] != null && enemyRenderers[0].material != null)
        {
            originalColor = enemyRenderers[0].material.color;
        }
    }

    void Start()
    {
        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
        }

        if (enemyData != null && maxHealth == 0)
        {
            Initialize(enemyData, 1f, 1f, 1f);
        }

        // 35% de probabilidad de lanzar un grito de alerta al aparecer
        if (Random.value < 0.35f)
        {
            StartCoroutine(SpawnScreamRoutine());
        }
    }

    public void Initialize(EnemyData data, float healthMultiplier = 1f, float damageMultiplier = 1f, float speedMultiplier = 1f)
    {
        this.enemyData = data;
        if (data == null) return;

        this.maxHealth = Mathf.RoundToInt(data.maxHealth * healthMultiplier);
        this.currentHealth = maxHealth;
        this.currentDamage = Mathf.RoundToInt(data.baseDamage * damageMultiplier);
        this.currentMoveSpeed = data.moveSpeed * speedMultiplier;
        this.attackRange = data.attackRange;
        this.attackCooldown = data.attackCooldown;
        this.isCrawling = false;
        this.isScreaming = false;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator == null) animator = gameObject.AddComponent<Animator>();
        if (animator != null)
        {
            if (data.animatorController != null)
            {
                animator.runtimeAnimatorController = data.animatorController;
            }
            if (data.enemyAvatar != null)
            {
                animator.avatar = data.enemyAvatar;
            }
            animator.applyRootMotion = false;
        }

        // Asegurar CapsuleCollider para recibir impactos y colisiones
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null) col = gameObject.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, (data != null && data.groundYOffset > 0.3f) ? 0f : 0.92f, 0f);
        col.height = 1.85f;
        col.radius = 0.38f;

        // Si no tiene un prefab personalizado con modelo propio, aplica la escala y color base
        if (data.enemyPrefab == null)
        {
            transform.localScale = data.scale;
            if (enemyRenderers != null && enemyRenderers.Length > 0 && enemyRenderers[0] != null)
            {
                enemyRenderers[0].material.color = data.bodyColor;
                originalColor = data.bodyColor;
            }
        }
    }

    private IEnumerator SpawnScreamRoutine()
    {
        isScreaming = true;
        yield return new WaitForSeconds(Random.Range(0.2f, 0.6f));

        if (HasValidAnimator && !isDead)
        {
            animator.SetTrigger(ScreamHash);
        }

        yield return new WaitForSeconds(1.2f);
        isScreaming = false;
    }

    void Update()
    {
        if (isDead || isScreaming) return;

        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
            UpdateAnimation(0f);
            return;
        }

        // Mantener al enemigo firmemente sobre el nivel del suelo según su offset de pivote y ajuste manual
        float baseOffset = (enemyData != null) ? enemyData.groundYOffset : 0f;
        float targetY = baseOffset + heightOffset;
        Vector3 currentPos = transform.position;
        currentPos.y = targetY;

        float distanceToPlayer = Vector3.Distance(currentPos, targetPlayer.position);

        // Dirección hacia el jugador
        Vector3 dir = (targetPlayer.position - currentPos).normalized;
        dir.y = 0;

        // Fuerza de separación entre enemigos para que no se amontonen ni se fusionen en un solo punto
        Vector3 separationForce = Vector3.zero;
        Collider[] nearby = Physics.OverlapSphere(currentPos, 1.3f);
        foreach (var c in nearby)
        {
            if (c != null && c.gameObject != gameObject && c.GetComponent<EnemyController>() != null)
            {
                Vector3 diff = currentPos - c.transform.position;
                diff.y = 0f;
                float sqrDist = diff.sqrMagnitude;
                if (sqrDist > 0.0001f && sqrDist < 1.69f)
                {
                    separationForce += diff.normalized / Mathf.Sqrt(sqrDist);
                }
            }
        }

        Vector3 finalMoveDir = (dir + separationForce * 0.75f).normalized;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
        }

        // Persecución o Ataque
        if (distanceToPlayer > attackRange)
        {
            transform.position = currentPos + finalMoveDir * (currentMoveSpeed * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed);
        }
        else
        {
            transform.position = currentPos;
            UpdateAnimation(0f);
            TryAttackPlayer();
        }
    }

    private bool HasValidAnimator => animator != null && animator.runtimeAnimatorController != null;

    private void UpdateAnimation(float speed)
    {
        if (!HasValidAnimator) return;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsMovingHash, speed > 0.1f);
        animator.SetBool(IsCrawlingHash, isCrawling);
    }

    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        // 25% de probabilidad de realizar mordida especial en el cuello con Aturdimiento
        bool isNeckBite = Random.value < 0.25f && !isCrawling;

        if (HasValidAnimator)
        {
            if (isNeckBite)
            {
                animator.SetTrigger(BiteHash);
            }
            else
            {
                animator.SetTrigger(AttackHash);
            }
        }

        PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
        PlayerController playerController = targetPlayer.GetComponent<PlayerController>();

        if (playerHealth != null && !playerHealth.IsDead)
        {
            int appliedDamage = isNeckBite ? Mathf.RoundToInt(currentDamage * 1.5f) : currentDamage;
            playerHealth.TakeDamage(appliedDamage, transform.position, transform.forward);

            if (isNeckBite && playerController != null)
            {
                // Iniciar forcejeo interactivo QTE donde el jugador debe spamear Espacio
                playerController.StartGrappleQTE(this, 2.5f);
                Debug.Log($"[Enemigo] ¡{enemyData?.enemyName ?? name} mordió el cuello del jugador e inició forcejeo QTE!");
            }
            else
            {
                Debug.Log($"[Enemigo] {enemyData?.enemyName ?? name} atacó al jugador por {appliedDamage} de daño.");
            }
        }
    }

    public void ApplyKnockback(Vector3 direction, float distance = 0.5f)
    {
        if (isDead) return;
        direction.y = 0f;
        transform.position += direction.normalized * distance;
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        bool isHeadshot = (hitPoint.y >= transform.position.y + 1.25f) && !isCrawling;
        bool isLegShot = (hitPoint.y <= transform.position.y + 0.65f) && !isCrawling;

        int finalDamage = amount;
        HUDUI hud = FindAnyObjectByType<HUDUI>();

        if (isHeadshot)
        {
            finalDamage = Mathf.RoundToInt(amount * 2.5f);
            if (hud != null)
            {
                hud.TriggerHitmarker(true);
                hud.ShowNotification("¡HEADSHOT CRÍTICO! (x2.5 Daño)");
            }
        }
        else if (hud != null)
        {
            hud.TriggerHitmarker(false);
        }

        // Si el disparo impacta en las piernas, el zombie tropieza y pasa a modo Crawler Lento
        if (isLegShot && !isCrawling)
        {
            StartCoroutine(TriggerCrawlMode(isFast: false));
            if (hud != null) hud.ShowNotification("¡Tiro en la pierna! El zombie cae y se arrastra");
        }
        else if (!isCrawling && (currentHealth - finalDamage <= maxHealth * 0.35f || Random.value < 0.20f))
        {
            // Gateo rápido y rabioso (Running Crawl) por furia de daño
            StartCoroutine(TriggerCrawlMode(isFast: true));
            if (hud != null) hud.ShowNotification("⚠️ ¡ZOMBIE ENFURECIDO A CUATRO PATAS!");
        }

        // Empuje físico hacia atrás (Knockback)
        ApplyKnockback(hitDirection, isHeadshot ? 0.75f : 0.45f);

        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);

        if (HasValidAnimator)
        {
            animator.SetTrigger(HitHash);
        }

        StartCoroutine(HitFlashCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator TriggerCrawlMode(bool isFast = false)
    {
        if (isCrawling) yield break;
        isCrawling = true;

        // Ajustar el CharacterController al suelo para el gateo
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.height = 0.5f;
            cc.center = new Vector3(0f, 0.25f, 0f);
        }

        CapsuleCollider cap = GetComponent<CapsuleCollider>();
        if (cap != null)
        {
            cap.height = 0.5f;
            cap.center = new Vector3(0f, 0.25f, 0f);
        }

        if (HasValidAnimator)
        {
            animator.SetBool(IsCrawlingHash, true);
            string stateName = isFast ? "Zombie_CrawlFast" : "Zombie_CrawlSlow";
            animator.CrossFade(stateName, 0.12f, 0, 0f);
        }

        if (isFast)
        {
            // Modo Crawler Rápido (Running Crawl)
            currentMoveSpeed = Mathf.Max(currentMoveSpeed * 1.5f, 3.2f);
            currentDamage += 5;
            attackRange = 1.1f;
        }
        else
        {
            // Modo Crawler Lento (Tiro en la pierna / arrastre agonizante)
            currentMoveSpeed = Mathf.Min(currentMoveSpeed * 0.65f, 1.3f);
            attackRange = 0.9f;
        }

        yield return null;
    }

    private IEnumerator HitFlashCoroutine()
    {
        if (enemyRenderers != null && enemyRenderers.Length > 0 && enemyRenderers[0] != null)
        {
            enemyRenderers[0].material.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (!isDead && enemyRenderers[0] != null)
            {
                enemyRenderers[0].material.color = originalColor;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Desactivar colisiones para que no bloquee disparos ni al jugador mientras cae
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        if (HasValidAnimator)
        {
            animator.SetTrigger(DieHash);
        }

        DropLoot();

        WaveManager waveMgr = FindAnyObjectByType<WaveManager>();
        if (waveMgr != null)
        {
            waveMgr.OnEnemyDefeated(this);
        }

        // Permitir que la animación completa de muerte (caída al suelo) se reproduzca limpiamente
        float destroyDelay = HasValidAnimator ? 1.8f : 0.05f;
        Destroy(gameObject, destroyDelay);
    }

    private void DropLoot()
    {
        if (enemyData == null) return;

        int goldDrop = Random.Range(enemyData.minGoldDrop, enemyData.maxGoldDrop + 1);
        if (goldDrop > 0)
        {
            Vector3 dropPos = transform.position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            LootPickup.CreatePickup(dropPos, LootType.Gold, goldDrop, null, 1);
        }

        if (enemyData.lootTable == null || enemyData.lootTable.Count == 0) return;

        foreach (var drop in enemyData.lootTable)
        {
            if (drop.item == null) continue;
            float roll = Random.value;
            if (roll <= drop.dropChance)
            {
                int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                Vector3 itemDropPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(-0.5f, 0.5f));
                LootPickup.CreatePickup(itemDropPos, LootType.Item, 0, drop.item, qty);
            }
        }
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null && !isDead)
        {
            WaveManager.Instance.UnregisterActiveEnemy(this);
        }
    }
}
