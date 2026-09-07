using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MutantBossController : MonoBehaviour, IDamageable
{
    [Header("Boss Data & Setup")]
    [SerializeField] private EnemyData bossData;
    [SerializeField] private Transform targetPlayer;
    [Tooltip("Ajuste de altura sobre el suelo")]
    public float groundYOffset = 0.65f;

    [Header("Combat Stats")]
    private int currentHealth;
    private int maxHealth;
    private int currentDamage;
    private float currentMoveSpeed;
    private float attackRange = 2.4f;
    private float attackCooldown = 1.6f;

    [Header("Special Boss Mechanics")]
    private bool isEnraged = false;
    private bool isDead = false;
    private bool isPerformingSpecial = false;
    private float nextAttackTime = 0f;
    private float nextJumpTime = 0f;
    private Animator animator;
    private Renderer[] bossRenderers;
    private Color originalColor = Color.white;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Animator Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int RageHash = Animator.StringToHash("Rage");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        bossRenderers = GetComponentsInChildren<Renderer>();
        if (bossRenderers.Length > 0 && bossRenderers[0] != null && bossRenderers[0].material != null)
        {
            originalColor = bossRenderers[0].material.color;
        }
    }

    void Start()
    {
        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
        }

        if (bossData != null && maxHealth == 0)
        {
            Initialize(bossData, 1f, 1f, 1f);
        }

        // Rugido de presentación al aparecer
        StartCoroutine(IntroRoarRoutine());
    }

    public void Initialize(EnemyData data, float healthMult = 1f, float damageMult = 1f, float speedMult = 1f)
    {
        this.bossData = data;
        if (data == null) return;

        maxHealth = Mathf.RoundToInt(data.maxHealth * healthMult);
        currentHealth = maxHealth;
        currentDamage = Mathf.RoundToInt(data.baseDamage * damageMult);
        currentMoveSpeed = data.moveSpeed * speedMult;
        attackRange = data.attackRange > 0 ? data.attackRange : 2.4f;
        attackCooldown = data.attackCooldown > 0 ? data.attackCooldown : 1.6f;
        groundYOffset = data.groundYOffset;

        if (animator != null && data.animatorController != null)
        {
            animator.runtimeAnimatorController = data.animatorController;
        }
    }

    private IEnumerator IntroRoarRoutine()
    {
        isPerformingSpecial = true;
        UpdateAnimation(0f);

        if (animator != null)
        {
            animator.SetTrigger(RageHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("🔥 ¡EL JEFE MUTANTE HA ENTRADO AL COMBATE!");
        }

        yield return new WaitForSeconds(2.2f);
        isPerformingSpecial = false;
        nextJumpTime = Time.time + 4.5f;
    }

    void Update()
    {
        if (isDead || isPerformingSpecial) return;

        if (targetPlayer == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
            UpdateAnimation(0f);
            return;
        }

        // Mantener altura del modelo sobre el suelo
        Vector3 currentPos = transform.position;
        currentPos.y = groundYOffset;

        float distanceToPlayer = Vector3.Distance(currentPos, targetPlayer.position);
        Vector3 dir = (targetPlayer.position - currentPos).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
        }

        // Ataque de Salto / Pisotón Sísmico (Jump Slam) a media distancia (5m - 12m)
        if (distanceToPlayer >= 5f && distanceToPlayer <= 12f && Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpSlamAttackRoutine());
            return;
        }

        // Persecución o Golpe Melee Pesado
        if (distanceToPlayer > attackRange)
        {
            transform.position = currentPos + dir * (currentMoveSpeed * Time.deltaTime);
            UpdateAnimation(currentMoveSpeed);
        }
        else
        {
            transform.position = currentPos;
            UpdateAnimation(0f);
            TryMeleeAttack();
        }
    }

    private void UpdateAnimation(float speed)
    {
        if (animator == null) return;
        animator.SetFloat(SpeedHash, speed);
        animator.SetBool(IsMovingHash, speed > 0.1f);
    }

    private void TryMeleeAttack()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        if (animator != null)
        {
            animator.SetTrigger(AttackHash);
        }

        StartCoroutine(DelayedDamageRoutine(0.45f, currentDamage, 2.5f));
    }

    private IEnumerator JumpSlamAttackRoutine()
    {
        isPerformingSpecial = true;
        nextJumpTime = Time.time + 8f;
        UpdateAnimation(0f);

        if (animator != null)
        {
            animator.SetTrigger(JumpHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.ShowNotification("⚠️ ¡PISOTÓN SÍSMICO DEL JEFE!");

        // Breve pausa mientras salta
        yield return new WaitForSeconds(0.6f);

        // Abalanzarse hacia el jugador
        Vector3 jumpTarget = targetPlayer != null ? targetPlayer.position : transform.position;
        jumpTarget.y = groundYOffset;
        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, jumpTarget, elapsed / 0.5f);
            yield return null;
        }

        // Onda expansiva en área al aterrizar
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 4.5f);
        foreach (var col in hitColliders)
        {
            if (col != null && col.CompareTag("Player"))
            {
                PlayerHealth pHealth = col.GetComponent<PlayerHealth>();
                if (pHealth != null)
                {
                    pHealth.TakeDamage(Mathf.RoundToInt(currentDamage * 1.35f), transform.position, (col.transform.position - transform.position).normalized);
                }
            }
        }

        yield return new WaitForSeconds(0.6f);
        isPerformingSpecial = false;
    }

    private IEnumerator DelayedDamageRoutine(float delay, int damage, float radius)
    {
        yield return new WaitForSeconds(delay);

        if (isDead || targetPlayer == null) yield break;

        float dist = Vector3.Distance(transform.position, targetPlayer.position);
        if (dist <= radius)
        {
            PlayerHealth pHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (pHealth != null && !pHealth.IsDead)
            {
                pHealth.TakeDamage(damage, transform.position, transform.forward);
                Debug.Log($"[Jefe Mutante] Golpeó al jugador por {damage} de daño.");
            }
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null) hud.TriggerHitmarker(false);

        // Fase de Furia (Enrage) al 50% de HP
        if (!isEnraged && currentHealth <= maxHealth * 0.5f && currentHealth > 0)
        {
            isEnraged = true;
            currentMoveSpeed *= 1.3f;
            currentDamage = Mathf.RoundToInt(currentDamage * 1.25f);
            if (hud != null) hud.ShowNotification("🔥 ¡EL JEFE MUTANTE HA ENTRADO EN FASE DE FURIA!");
            if (animator != null) animator.SetTrigger(RageHash);
        }

        if (animator != null && !isPerformingSpecial && Random.value < 0.25f)
        {
            animator.SetTrigger(HitHash);
        }

        StartCoroutine(HitFlashCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlashCoroutine()
    {
        if (bossRenderers != null && bossRenderers.Length > 0 && bossRenderers[0] != null)
        {
            bossRenderers[0].material.color = isEnraged ? Color.red : Color.white;
            yield return new WaitForSeconds(0.08f);
            if (!isDead && bossRenderers[0] != null)
            {
                bossRenderers[0].material.color = isEnraged ? new Color(1f, 0.3f, 0.3f) : originalColor;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.LogWarning("[Jefe Mutante] ¡Ha sido derrotado!");

        UpdateAnimation(0f);

        if (animator != null)
        {
            animator.SetTrigger(DieHash);
        }

        // Drop de botín épico
        SpawnBossLoot();

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("🏆 ¡JEFE MUTANTE ANIQUILADO! Recompensas liberadas");
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 6.0f);
    }

    private void SpawnBossLoot()
    {
        int goldDrop = (bossData != null) ? Random.Range(bossData.minGoldDrop, bossData.maxGoldDrop + 1) : 250;
        if (Inventory.Instance != null)
        {
            Inventory.Instance.AddGold(goldDrop);
        }

        // Spawn de botín físico en el suelo
        if (bossData != null && bossData.lootTable != null)
        {
            foreach (var loot in bossData.lootTable)
            {
                if (loot.item != null && Random.value <= loot.dropChance)
                {
                    int qty = Random.Range(loot.minQuantity, loot.maxQuantity + 1);
                    if (Inventory.Instance != null)
                    {
                        Inventory.Instance.AddItem(loot.item, qty);
                    }
                }
            }
        }
    }
}
