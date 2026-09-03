using UnityEngine;

public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Data & Configuration")]
    [SerializeField] private EnemyData enemyData;
    [SerializeField] private Transform targetPlayer;

    [Header("Scaled Stats")]
    private int currentHealth;
    private int maxHealth;
    private int currentDamage;
    private float currentMoveSpeed;
    private float attackRange;
    private float attackCooldown;

    private float nextAttackTime = 0f;
    private bool isDead = false;
    private Renderer enemyRenderer;
    private Color originalColor;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    void Awake()
    {
        enemyRenderer = GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            originalColor = enemyRenderer.material.color;
        }
    }

    void Start()
    {
        if (targetPlayer == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
        }

        if (enemyData != null && maxHealth == 0)
        {
            Initialize(enemyData, 1f, 1f, 1f);
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

        transform.localScale = data.scale;

        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = data.bodyColor;
            originalColor = data.bodyColor;
        }
    }

    void Update()
    {
        if (isDead) return;

        if (targetPlayer == null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null) targetPlayer = player.transform;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);

        // Rotar hacia el jugador
        Vector3 dir = (targetPlayer.position - transform.position).normalized;
        dir.y = 0;
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // Persecución o Ataque
        if (distanceToPlayer > attackRange)
        {
            transform.position += dir * (currentMoveSpeed * Time.deltaTime);
        }
        else
        {
            TryAttackPlayer();
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(currentDamage, transform.position, transform.forward);
            Debug.Log($"[Enemigo] {enemyData?.enemyName ?? name} atacó al jugador por {currentDamage} de daño.");
        }
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        // Feedback visual de golpe
        StartCoroutine(HitFlashCoroutine());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator HitFlashCoroutine()
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            if (!isDead && enemyRenderer != null)
            {
                enemyRenderer.material.color = originalColor;
            }
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        SpawnLoot();

        WaveManager waveMgr = FindFirstObjectByType<WaveManager>();
        if (waveMgr != null)
        {
            waveMgr.OnEnemyDefeated(this);
        }

        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        if (enemyData == null) return;

        // 1. Soltar Oro
        int goldToDrop = Random.Range(enemyData.minGoldDrop, enemyData.maxGoldDrop + 1);
        if (goldToDrop > 0)
        {
            GameObject goldObj = CreateLootPickupObject(LootType.Gold);
            goldObj.transform.position = transform.position + Vector3.up * 0.5f;
            LootPickup pickup = goldObj.GetComponent<LootPickup>();
            pickup.SetupGold(goldToDrop);
        }

        // 2. Soltar Items según la tabla de botín (Loot Table)
        if (enemyData.lootTable != null)
        {
            foreach (var drop in enemyData.lootTable)
            {
                if (drop.item != null && Random.value <= drop.dropChance)
                {
                    int qty = Random.Range(drop.minQuantity, drop.maxQuantity + 1);
                    GameObject itemObj = CreateLootPickupObject(LootType.Item);
                    itemObj.transform.position = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere * 0.5f;
                    itemObj.transform.position = new Vector3(itemObj.transform.position.x, transform.position.y + 0.5f, itemObj.transform.position.z);
                    LootPickup itemPickup = itemObj.GetComponent<LootPickup>();
                    itemPickup.SetupItem(drop.item, qty);
                }
            }
        }
    }

    private GameObject CreateLootPickupObject(LootType type)
    {
        GameObject loot = GameObject.CreatePrimitive(type == LootType.Gold ? PrimitiveType.Cylinder : PrimitiveType.Cube);
        loot.name = type == LootType.Gold ? "Loot_Gold" : "Loot_Item";
        loot.transform.localScale = type == LootType.Gold ? new Vector3(0.4f, 0.08f, 0.4f) : new Vector3(0.35f, 0.35f, 0.35f);
        
        Collider c = loot.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        Renderer r = loot.GetComponent<Renderer>();
        if (r != null)
        {
            r.material.color = type == LootType.Gold ? new Color(1f, 0.85f, 0.1f) : new Color(0.2f, 0.8f, 1f);
        }

        loot.AddComponent<LootPickup>();
        return loot;
    }
}
