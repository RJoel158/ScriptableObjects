using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Disparar animación de impacto solo si sigue con vida
        PlayerController ctrl = GetComponent<PlayerController>();
        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null && (ctrl == null || !ctrl.IsInGrappleQTE))
        {
            anim.SetTrigger(HitHash);
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
        Debug.LogWarning("[Jugador] Ha muerto. Ejecutando animación de muerte...");

        PlayerController ctrl = GetComponent<PlayerController>();
        if (ctrl != null)
        {
            ctrl.CanMove = false;
            ctrl.enabled = false;
        }

        PlayerCombat combat = GetComponent<PlayerCombat>();
        if (combat != null)
        {
            combat.enabled = false;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        Animator anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.ResetTrigger(HitHash);
            anim.ResetTrigger(Animator.StringToHash("Shoot"));
            anim.ResetTrigger(Animator.StringToHash("Reload"));
            anim.SetBool(Animator.StringToHash("Struggle"), false);
            anim.SetBool(Animator.StringToHash("isMoving"), false);
            anim.SetFloat(Animator.StringToHash("Speed"), 0f);
            anim.SetTrigger(DieHash);
            anim.CrossFade("Player_Die", 0.08f, 0, 0f);
        }

        HUDUI hud = FindAnyObjectByType<HUDUI>();
        if (hud != null)
        {
            hud.ShowNotification("☠ ¡HAS MUERTO! Reiniciando partida...");
        }

        OnPlayerDied?.Invoke();

        StartCoroutine(RestartSceneAfterDeathRoutine());
    }

    private IEnumerator RestartSceneAfterDeathRoutine()
    {
        // Esperar a que la animación de muerte termine de reproducirse completamente
        yield return new WaitForSeconds(3.8f);

        // Resetear inventario si existe para arrancar desde cero
        if (Inventory.Instance != null)
        {
            Inventory.Instance.ClearInventory();
        }

        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
