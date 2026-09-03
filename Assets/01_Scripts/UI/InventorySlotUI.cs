using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Button selectButton;
    [SerializeField] private Image borderImage;

    private InventorySlot currentSlot;
    private System.Action<InventorySlot> onClickCallback;

    public void Setup(InventorySlot slot, System.Action<InventorySlot> onClick)
    {
        this.currentSlot = slot;
        this.onClickCallback = onClick;

        if (slot != null && slot.item != null)
        {
            if (iconImage != null)
            {
                iconImage.enabled = slot.item.itemIcon != null;
                iconImage.sprite = slot.item.itemIcon;
            }

            if (nameText != null)
            {
                nameText.text = slot.item.itemName;
            }

            if (quantityText != null)
            {
                quantityText.text = slot.quantity > 1 ? $"x{slot.quantity}" : "";
            }
        }
        else
        {
            Clear();
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onClickCallback?.Invoke(currentSlot));
        }
    }

    public void Clear()
    {
        if (iconImage != null) iconImage.enabled = false;
        if (nameText != null) nameText.text = "";
        if (quantityText != null) quantityText.text = "";
    }

    public void SetSelected(bool selected)
    {
        if (borderImage != null)
        {
            borderImage.color = selected ? Color.yellow : new Color(1, 1, 1, 0.2f);
        }
    }
}
