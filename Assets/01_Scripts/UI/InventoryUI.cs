using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Detail Panel")]
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TextMeshProUGUI detailName;
    [SerializeField] private TextMeshProUGUI detailType;
    [SerializeField] private TextMeshProUGUI detailRarity;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailStats;
    [SerializeField] private TextMeshProUGUI detailQuantity;
    [SerializeField] private TextMeshProUGUI detailSellPrice;

    [Header("Action Buttons")]
    [SerializeField] private Button equipButton;
    [SerializeField] private Button useButton;
    [SerializeField] private Button sellButton;
    [SerializeField] private TextMeshProUGUI equipButtonText;

    private InventorySlot selectedSlot;
    private List<InventorySlotUI> spawnedSlots = new List<InventorySlotUI>();

    void Start()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged += RefreshInventory;
        }

        if (equipButton != null) equipButton.onClick.AddListener(OnEquipClicked);
        if (useButton != null) useButton.onClick.AddListener(OnUseClicked);
        if (sellButton != null) sellButton.onClick.AddListener(OnSellClicked);

        RefreshInventory();
        ClearDetailPanel();
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnInventoryChanged -= RefreshInventory;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel != null)
        {
            bool active = !inventoryPanel.activeSelf;
            inventoryPanel.SetActive(active);
            if (active)
            {
                RefreshInventory();
            }
        }
    }

    public void RefreshInventory()
    {
        if (slotContainer == null || Inventory.Instance == null) return;

        // Limpiar slots previos
        foreach (Transform child in slotContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedSlots.Clear();

        // Generar slots con los items del inventario
        foreach (InventorySlot slot in Inventory.Instance.items)
        {
            GameObject slotObj = slotPrefab != null 
                ? Instantiate(slotPrefab, slotContainer) 
                : CreateDefaultSlotObj(slotContainer);

            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();
            if (slotUI == null) slotUI = slotObj.AddComponent<InventorySlotUI>();

            slotUI.Setup(slot, OnSlotSelected);
            spawnedSlots.Add(slotUI);
        }

        // Si el slot seleccionado ya no existe o cambió
        if (selectedSlot != null && !Inventory.Instance.items.Contains(selectedSlot))
        {
            if (Inventory.Instance.items.Count > 0)
            {
                OnSlotSelected(Inventory.Instance.items[0]);
            }
            else
            {
                ClearDetailPanel();
            }
        }
    }

    private void OnSlotSelected(InventorySlot slot)
    {
        selectedSlot = slot;
        if (slot == null || slot.item == null)
        {
            ClearDetailPanel();
            return;
        }

        if (detailPanel != null) detailPanel.SetActive(true);

        ItemData item = slot.item;

        if (detailIcon != null)
        {
            detailIcon.enabled = item.itemIcon != null;
            detailIcon.sprite = item.itemIcon;
        }

        if (detailName != null) detailName.text = item.itemName;
        if (detailType != null) detailType.text = $"Tipo: {item.itemType}";
        if (detailRarity != null) detailRarity.text = $"Rareza: {item.rarity}";
        if (detailDescription != null) detailDescription.text = item.itemDescription;
        if (detailQuantity != null) detailQuantity.text = $"En inventario: {slot.quantity}";
        if (detailSellPrice != null) detailSellPrice.text = $"Precio de Venta: {item.GetSellPrice()} Oro";

        // Mostrar estadísticas según el tipo de objeto
        if (detailStats != null)
        {
            if (item is WeaponData weapon)
            {
                detailStats.text = $"⚔ Daño: {weapon.damage}\n⚡ Cadencia: {weapon.attackSpeed}/s\n🏹 Rango: {weapon.attackRange}";
            }
            else if (item is ArmorData armor)
            {
                detailStats.text = $"🛡 Defensa: {armor.defense}\n❤ Vida Extra: +{armor.bonusMaxHealth}";
            }
            else if (item is PotionData potion)
            {
                detailStats.text = $"🧪 Recupera: +{potion.healthRestoreAmount} HP";
            }
            else
            {
                detailStats.text = "Sin estadísticas adicionales.";
            }
        }

        // Ajustar visibilidad y textos de botones
        if (equipButton != null)
        {
            bool isEquippable = item is WeaponData || item is ArmorData;
            equipButton.gameObject.SetActive(isEquippable);
            if (isEquippable && equipButtonText != null)
            {
                bool isCurrentlyEquipped = (item is WeaponData w && Inventory.Instance.equippedWeapon == w) ||
                                           (item is ArmorData a && Inventory.Instance.equippedArmor == a);
                equipButtonText.text = isCurrentlyEquipped ? "Equipado ✓" : "Equipar";
            }
        }

        if (useButton != null)
        {
            useButton.gameObject.SetActive(item is PotionData || item.itemType == ItemType.Consumable);
        }

        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(true);
        }
    }

    private void ClearDetailPanel()
    {
        selectedSlot = null;
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void OnEquipClicked()
    {
        if (selectedSlot != null && selectedSlot.item != null)
        {
            Inventory.Instance.UseItem(selectedSlot.item);
            OnSlotSelected(selectedSlot);
        }
    }

    private void OnUseClicked()
    {
        if (selectedSlot != null && selectedSlot.item != null)
        {
            Inventory.Instance.UseItem(selectedSlot.item);
        }
    }

    private void OnSellClicked()
    {
        if (selectedSlot != null && selectedSlot.item != null)
        {
            Inventory.Instance.SellItem(selectedSlot.item, 1);
        }
    }

    private GameObject CreateDefaultSlotObj(Transform parent)
    {
        GameObject slot = new GameObject("Slot_Item", typeof(RectTransform), typeof(Image), typeof(Button));
        slot.transform.SetParent(parent, false);
        return slot;
    }
}
