using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    private bool isDead = false;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public event Action<int, int> OnHealthChanged;
    public event Action OnPlayerDied;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (isDead) return;

        // Mitigación por armadura si está equipada
        if (Inventory.Instance != null && Inventory.Instance.equippedArmor != null)
        {
            int defense = Inventory.Instance.equippedArmor.defense;
            amount = Mathf.Max(1, amount - defense);
        }

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);

        Debug.Log($"[Jugador] Daño recibido: {amount}. Vida restante: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        Debug.Log($"[Jugador] Curado: +{amount}. Vida actual: {currentHealth}/{maxHealth}");
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        Debug.LogWarning("[Jugador] Ha muerto.");
        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
