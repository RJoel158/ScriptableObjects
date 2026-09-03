using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUI : MonoBehaviour
{
    [Header("Player Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Gold & Economy")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Weapon Status")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image weaponIcon;

    [Header("Wave HUD")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesRemainingText;
    [SerializeField] private TextMeshProUGUI intermissionText;

    [Header("Notification Toast")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;

    [Header("Quick Buttons")]
    [SerializeField] private Button inventoryButton;
    [SerializeField] private Button storeButton;

    private Coroutine currentNotificationRoutine;

    void Start()
    {
        // Suscribirse a salud del jugador
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        // Suscribirse al inventario y oro
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnGoldChanged += UpdateGoldUI;
            Inventory.Instance.OnWeaponEquipped += UpdateWeaponUI;
            Inventory.Instance.OnInventoryNotification += ShowNotification;

            UpdateGoldUI(Inventory.Instance.CurrentGold);
            if (Inventory.Instance.equippedWeapon != null)
            {
                UpdateWeaponUI(Inventory.Instance.equippedWeapon);
            }
        }

        // Suscribirse al WaveManager
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnEnemyCountChanged += OnEnemyCountChanged;
            WaveManager.Instance.OnWaveCountdownChanged += OnWaveCountdown;

            OnWaveStarted(WaveManager.Instance.CurrentWave);
        }

        // Botones rápidos
        UIManager uiMgr = FindFirstObjectByType<UIManager>();
        if (uiMgr != null)
        {
            if (inventoryButton != null) inventoryButton.onClick.AddListener(uiMgr.ToggleInventory);
            if (storeButton != null) storeButton.onClick.AddListener(uiMgr.ToggleStore);
        }

        if (intermissionText != null) intermissionText.text = "";
    }

    void OnDestroy()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnGoldChanged -= UpdateGoldUI;
            Inventory.Instance.OnWeaponEquipped -= UpdateWeaponUI;
            Inventory.Instance.OnInventoryNotification -= ShowNotification;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            WaveManager.Instance.OnEnemyCountChanged -= OnEnemyCountChanged;
            WaveManager.Instance.OnWaveCountdownChanged -= OnWaveCountdown;
        }
    }

    private void UpdateHealthUI(int current, int max)
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
            healthSlider.value = current;
        }
        if (healthText != null)
        {
            healthText.text = $"HP: {current} / {max}";
        }
    }

    private void UpdateGoldUI(int gold)
    {
        if (goldText != null)
        {
            goldText.text = $"💰 {gold}";
        }
    }

    private void UpdateWeaponUI(WeaponData weapon)
    {
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon != null ? $"Arma: {weapon.itemName}" : "Arma: Puños";
        }
        if (weaponIcon != null)
        {
            weaponIcon.enabled = weapon != null && weapon.itemIcon != null;
            if (weapon != null) weaponIcon.sprite = weapon.itemIcon;
        }
    }

    private void OnWaveStarted(int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"⚔ Oleada: {wave}";
        }
        if (intermissionText != null)
        {
            intermissionText.text = "";
        }
        ShowNotification($"¡Comienza la Oleada {wave}!");
    }

    private void OnEnemyCountChanged(int alive, int total)
    {
        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.text = $"👾 Enemigos: {alive}";
        }
    }

    private void OnWaveCountdown(float seconds)
    {
        if (intermissionText != null)
        {
            intermissionText.text = seconds > 0 ? $"Próxima Oleada en: {Mathf.CeilToInt(seconds)}s (Visita la tienda 'T')" : "";
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;

        if (currentNotificationRoutine != null)
        {
            StopCoroutine(currentNotificationRoutine);
        }

        currentNotificationRoutine = StartCoroutine(NotificationRoutine(message));
    }

    private IEnumerator NotificationRoutine(string message)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        notificationPanel.SetActive(false);
    }
}
