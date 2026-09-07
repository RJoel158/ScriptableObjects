using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDUI : MonoBehaviour
{
    public static HUDUI Instance { get; private set; }

    [Header("Player Health")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    [Header("Gold & Economy")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Weapon Status & Ammo")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI ammoText;
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

    // Hitmarker System
    private float hitmarkerTimer = 0f;
    private bool isCriticalHit = false;

    // QTE Grapple Struggle System
    private bool showQTE = false;

    private Coroutine currentNotificationRoutine;
    private int cachedHealth = 100;
    private int cachedMaxHealth = 100;
    private int cachedGold = 100;
    private int cachedMag = 12;
    private int cachedReserve = 120;
    private bool cachedReloading = false;
    private string cachedWeaponName = "Pistola M1911";

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthUI;
            UpdateHealthUI(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
        if (combat != null)
        {
            combat.OnAmmoChanged += UpdateAmmoUI;
            if (combat.currentWeapon != null)
            {
                UpdateAmmoUI(combat.currentMagAmmo, combat.currentReserveAmmo, combat.isReloading);
            }
        }

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

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnEnemyCountChanged += OnEnemyCountChanged;
            WaveManager.Instance.OnWaveCountdownChanged += OnWaveCountdown;

            OnWaveStarted(WaveManager.Instance.CurrentWave);
        }

        UIManager uiMgr = FindAnyObjectByType<UIManager>();
        if (uiMgr != null)
        {
            if (inventoryButton != null) inventoryButton.onClick.AddListener(uiMgr.ToggleInventory);
            if (storeButton != null) storeButton.onClick.AddListener(uiMgr.ToggleStore);
        }

        if (intermissionText != null) intermissionText.text = "";
    }

    void OnDestroy()
    {
        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthUI;
        }

        PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
        if (combat != null)
        {
            combat.OnAmmoChanged -= UpdateAmmoUI;
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

    private float targetHealthValue = 100f;
    private Image healthFillImage;

    void Update()
    {
        if (hitmarkerTimer > 0f)
        {
            hitmarkerTimer -= Time.deltaTime;
        }

        // Animación dinámica y suave de la barra de vida
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealthValue, Time.deltaTime * 10f);

            if (healthFillImage == null && healthSlider.fillRect != null)
            {
                healthFillImage = healthSlider.fillRect.GetComponent<Image>();
            }

            if (healthFillImage != null && cachedMaxHealth > 0)
            {
                float ratio = (float)cachedHealth / cachedMaxHealth;
                healthFillImage.color = ratio > 0.5f 
                    ? Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(0.18f, 0.8f, 0.44f), (ratio - 0.5f) * 2f)
                    : Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.8f, 0.2f), ratio * 2f);
            }
        }
    }

    private void UpdateHealthUI(int current, int max)
    {
        cachedHealth = current;
        cachedMaxHealth = max;
        targetHealthValue = current;

        if (healthSlider != null)
        {
            healthSlider.maxValue = max;
        }
        if (healthText != null)
        {
            healthText.text = $"HP: {current} / {max}";
        }
    }

    private void UpdateGoldUI(int gold)
    {
        cachedGold = gold;
        if (goldText != null)
        {
            goldText.text = $"ORO: {gold} G";
        }
    }

    private void UpdateWeaponUI(WeaponData weapon)
    {
        cachedWeaponName = weapon != null ? weapon.itemName : "Arma: Puños";
        if (weaponNameText != null)
        {
            weaponNameText.text = $"{cachedWeaponName}";
        }
        if (weaponIcon != null)
        {
            weaponIcon.enabled = weapon != null && weapon.itemIcon != null;
            if (weapon != null) weaponIcon.sprite = weapon.itemIcon;
        }
    }

    public void UpdateAmmoUI(int mag, int reserve, bool reloading)
    {
        cachedMag = mag;
        cachedReserve = reserve;
        cachedReloading = reloading;

        if (ammoText == null) return;

        if (reloading)
        {
            ammoText.text = "RECARGANDO...";
            ammoText.color = Color.yellow;
        }
        else if (mag < 0)
        {
            ammoText.text = "INF / INF";
            ammoText.color = Color.cyan;
        }
        else
        {
            ammoText.text = $"{mag} / {reserve} [R]";
            ammoText.color = (mag <= 3) ? Color.red : Color.white;
        }
    }

    private void OnWaveStarted(int wave)
    {
        if (waveText != null)
        {
            waveText.text = $"RONDA: {wave}";
        }
        if (intermissionText != null)
        {
            intermissionText.text = "";
        }
        ShowNotification($"¡Comienza la Ronda {wave}!");
    }

    private void OnEnemyCountChanged(int alive, int total)
    {
        if (enemiesRemainingText != null)
        {
            enemiesRemainingText.text = $"ZOMBIES: {alive}";
        }
    }

    private void OnWaveCountdown(float seconds)
    {
        if (intermissionText != null)
        {
            intermissionText.text = seconds > 0 ? $"TIENDA ABIERTA [T] | PRÓXIMA RONDA: {Mathf.CeilToInt(seconds)}s" : "";
        }
    }

    public void TriggerHitmarker(bool isCrit)
    {
        hitmarkerTimer = 0.22f;
        isCriticalHit = isCrit;
    }

    public void ShowQTEPrompt(bool show)
    {
        showQTE = show;
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

    void OnGUI()
    {
        PerspectiveCameraController camCtrl = PerspectiveCameraController.Instance;
        PlayerController player = FindAnyObjectByType<PlayerController>();

        // 1. Mira táctica (+) en modo de apuntado RE4
        if (camCtrl != null && camCtrl.IsInOverTheShoulder)
        {
            float size = 16f;
            float thickness = 2f;
            float screenX = Screen.width / 2f;
            float screenY = Screen.height / 2f;

            Color crosshairColor = new Color(0.2f, 1f, 0.85f, 0.85f);
            Texture2D tex = Texture2D.whiteTexture;
            GUI.color = crosshairColor;

            // Líneas horizontales
            GUI.DrawTexture(new Rect(screenX - size, screenY - thickness / 2f, size - 4f, thickness), tex);
            GUI.DrawTexture(new Rect(screenX + 4f, screenY - thickness / 2f, size - 4f, thickness), tex);

            // Líneas verticales
            GUI.DrawTexture(new Rect(screenX - thickness / 2f, screenY - size, thickness, size - 4f), tex);
            GUI.DrawTexture(new Rect(screenX - thickness / 2f, screenY + 4f, thickness, size - 4f), tex);

            // Punto central
            GUI.DrawTexture(new Rect(screenX - 1.5f, screenY - 1.5f, 3f, 3f), tex);
        }

        // 2. Hitmarker (✕) al impactar enemigos
        if (hitmarkerTimer > 0f)
        {
            float screenX = Screen.width / 2f;
            float screenY = Screen.height / 2f;
            float hmSize = isCriticalHit ? 14f : 9f;
            GUI.color = isCriticalHit ? Color.red : Color.white;

            GUI.Label(new Rect(screenX - 15, screenY - 15, 30, 30), "✕", new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = isCriticalHit ? 22 : 16,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState() { textColor = isCriticalHit ? Color.red : Color.white }
            });
        }

        // 3. Forcejeo interactivo QTE (Struggle Modal)
        if (showQTE && player != null && player.IsInGrappleQTE)
        {
            DrawGrappleQTE(player);
        }

        // 4. Hotbar inferior (Estilo Minecraft / RPG de 5 slots rápidos [1..5])
        DrawQuickHotbar();
    }

    private void DrawGrappleQTE(PlayerController player)
    {
        float boxWidth = 440f;
        float boxHeight = 110f;
        float posX = (Screen.width - boxWidth) / 2f;
        float posY = Screen.height * 0.38f;

        // Fondo oscuro
        GUI.color = new Color(0.1f, 0.02f, 0.02f, 0.92f);
        GUI.DrawTexture(new Rect(posX, posY, boxWidth, boxHeight), Texture2D.whiteTexture);

        // Borde rojo pulsante
        float pulse = Mathf.PingPong(Time.time * 6f, 1f);
        GUI.color = Color.Lerp(Color.red, Color.yellow, pulse);
        GUI.DrawTexture(new Rect(posX, posY, boxWidth, 3), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX, posY + boxHeight - 3, boxWidth, 3), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX, posY, 3, boxHeight), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(posX + boxWidth - 3, posY, 3, boxHeight), Texture2D.whiteTexture);

        // Texto QTE
        GUIStyle titleStyle = new GUIStyle()
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = new GUIStyleState() { textColor = Color.white }
        };
        GUI.Label(new Rect(posX, posY + 10, boxWidth, 30), "⚠️ ¡ZOMBIE MORDIENDO! - ¡SPAMEA [ESPACIO]!", titleStyle);

        // Barra de progreso del forcejeo
        float barWidth = boxWidth - 40;
        float barHeight = 22f;
        float barX = posX + 20;
        float barY = posY + 52;

        GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        GUI.DrawTexture(new Rect(barX, barY, barWidth, barHeight), Texture2D.whiteTexture);

        float fill = Mathf.Clamp01(player.GrappleProgress);
        GUI.color = Color.Lerp(Color.red, Color.green, fill);
        GUI.DrawTexture(new Rect(barX, barY, barWidth * fill, barHeight), Texture2D.whiteTexture);
    }

    private void DrawQuickHotbar()
    {
        if (Inventory.Instance == null) return;

        // HUD de 3 Celdas Dedicadas (Estilo Resident Evil / Survival RPG)
        // Slot 1: Arma Principal [1]
        // Slot 2: Arma Secundaria M1911 [2]
        // Slot 3: Consumible Rápido (Pociones/Botiquines) [3]
        int dedicatedSlots = 3;
        float slotWidth = 90f;
        float slotHeight = 72f;
        float spacing = 12f;
        float totalWidth = dedicatedSlots * slotWidth + (dedicatedSlots - 1) * spacing;
        float startX = (Screen.width - totalWidth) / 2f;
        float startY = Screen.height - slotHeight - 20f;

        WeaponData primary = Inventory.Instance.GetPrimaryWeapon();
        WeaponData secondary = Inventory.Instance.GetSecondaryWeapon();
        PotionData quickPotion = Inventory.Instance.GetFirstConsumable();
        int potionCount = quickPotion != null ? Inventory.Instance.GetQuantity(quickPotion) : 0;

        for (int i = 0; i < dedicatedSlots; i++)
        {
            Rect slotRect = new Rect(startX + i * (slotWidth + spacing), startY, slotWidth, slotHeight);
            bool isSlotActive = false;
            string slotCategory = "";
            string itemName = "Vacío";
            string extraText = "";

            if (i == 0) // Slot 1: Principal
            {
                if (primary != null)
                {
                    slotCategory = "1. PRINCIPAL";
                    itemName = primary.itemName.Split(' ')[0] + " " + (primary.itemName.Split(' ').Length > 1 ? primary.itemName.Split(' ')[1] : "");
                    isSlotActive = Inventory.Instance.equippedWeapon == primary;
                }
                else
                {
                    slotCategory = "1. VACÍO";
                    itemName = "Tienda [T]";
                    extraText = "[BLOQUEADO]";
                }
            }
            else if (i == 1) // Slot 2: Secundaria (M1911)
            {
                slotCategory = "2. SECUNDARIA";
                if (secondary != null)
                {
                    itemName = secondary.itemName;
                    isSlotActive = Inventory.Instance.equippedWeapon == secondary;
                }
            }
            else if (i == 2) // Slot 3: Consumible
            {
                slotCategory = "3. CURACIÓN";
                if (quickPotion != null && potionCount > 0)
                {
                    itemName = quickPotion.itemName;
                    extraText = $"x{potionCount}";
                    isSlotActive = cachedHealth < cachedMaxHealth; // Sugerir verde si necesita curarse
                }
                else
                {
                    itemName = "Sin pociones";
                }
            }

            // Fondo del slot (vidrio oscuro / tintado según estado)
            Color bgColor = isSlotActive 
                ? (i == 2 ? new Color(0.08f, 0.35f, 0.15f, 0.92f) : new Color(0.12f, 0.32f, 0.22f, 0.92f))
                : new Color(0.06f, 0.08f, 0.12f, 0.88f);

            GUI.color = bgColor;
            GUI.DrawTexture(slotRect, Texture2D.whiteTexture);

            // Borde brillante (Dorado si está equipada, verde si es curación disponible, gris oscuro si inactivo)
            Color borderColor = isSlotActive 
                ? (i == 2 ? new Color(0.2f, 1f, 0.4f, 1f) : new Color(1f, 0.85f, 0.2f, 1f))
                : new Color(0.25f, 0.3f, 0.38f, 0.8f);

            GUI.color = borderColor;
            GUI.DrawTexture(new Rect(slotRect.x, slotRect.y, slotRect.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(slotRect.x, slotRect.y + slotRect.height - 2, slotRect.width, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(slotRect.x, slotRect.y, 2, slotRect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(slotRect.x + slotRect.width - 2, slotRect.y, 2, slotRect.height), Texture2D.whiteTexture);

            // Etiqueta superior del slot (ej: 1. PRINCIPAL)
            GUIStyle catStyle = new GUIStyle()
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = new GUIStyleState() { textColor = isSlotActive ? Color.white : new Color(0.7f, 0.8f, 0.9f) }
            };
            GUI.Label(new Rect(slotRect.x, slotRect.y + 4, slotWidth, 16), slotCategory, catStyle);

            // Nombre del item
            GUIStyle nameStyle = new GUIStyle()
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = new GUIStyleState() { textColor = isSlotActive ? (i == 2 ? Color.green : Color.yellow) : Color.white }
            };
            GUI.Label(new Rect(slotRect.x + 2, slotRect.y + 18, slotWidth - 4, 34), itemName, nameStyle);

            // Contador extra (ej: x3)
            if (!string.IsNullOrEmpty(extraText))
            {
                GUIStyle countStyle = new GUIStyle()
                {
                    alignment = TextAnchor.LowerRight,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = new GUIStyleState() { textColor = new Color(0.2f, 1f, 0.5f) }
                };
                GUI.Label(new Rect(slotRect.x, slotRect.y + slotHeight - 20, slotWidth - 6, 18), extraText, countStyle);
            }

            // Click táctil / interactivo sobre el slot
            if (GUI.Button(slotRect, GUIContent.none, GUIStyle.none))
            {
                if (i == 0) Inventory.Instance.EquipPrimary();
                else if (i == 1) Inventory.Instance.EquipSecondary();
                else if (i == 2) Inventory.Instance.UseQuickConsumable();
            }
        }
    }
}
