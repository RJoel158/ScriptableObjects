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

    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int DieHash = Animator.StringToHash("Die");

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

        // Disparar animación de impacto si el jugador sigue vivo y no está en forcejeo
        PlayerController ctrl = GetComponent<PlayerController>();
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null && currentHealth > 0 && (ctrl == null || !ctrl.IsInGrappleQTE))
        {
            anim.SetTrigger(HitHash);
        }

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

        PlayerController ctrl = GetComponent<PlayerController>();
        if (ctrl != null)
        {
            ctrl.CanMove = false;
        }

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger(DieHash);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("☠ ¡HAS MUERTO! Presiona Reiniciar");
        }

        OnPlayerDied?.Invoke();
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
