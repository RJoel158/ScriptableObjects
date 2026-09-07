using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI buyButtonText;

    private ItemData currentItem;
    private System.Action<ItemData> onBuyCallback;

    public void Setup(ItemData item, System.Action<ItemData> onBuy)
    {
        this.currentItem = item;
        this.onBuyCallback = onBuy;

        if (item == null) return;

        if (iconImage != null)
        {
            iconImage.enabled = item.itemIcon != null;
            iconImage.sprite = item.itemIcon;
            iconImage.color = item.itemIcon != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        }

        if (nameText != null)
        {
            nameText.text = item.itemName;
        }

        if (priceText != null)
        {
            priceText.text = $"{item.itemPrice} G";
        }

        if (statsText != null)
        {
            if (item is WeaponData w)
            {
                statsText.text = $"Daño: {w.damage} | Cadencia: {w.attackSpeed}/s";
            }
            else if (item is ArmorData a)
            {
                statsText.text = $"Defensa: +{a.defense} | Vida: +{a.bonusMaxHealth} HP";
            }
            else if (item is PotionData p)
            {
                statsText.text = $"Restaura: +{p.healthRestoreAmount} HP";
            }
            else
            {
                statsText.text = item.itemDescription;
            }
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuyCallback?.Invoke(currentItem));
        }

        UpdateAffordability();
    }

    public void UpdateAffordability()
    {
        if (buyButton != null && currentItem != null && Inventory.Instance != null)
        {
            bool canAfford = Inventory.Instance.CanAfford(currentItem.itemPrice);
            buyButton.interactable = canAfford;
            if (buyButtonText != null)
            {
                buyButtonText.text = canAfford ? "Comprar" : "Sin Oro";
            }
        }
    }
}
