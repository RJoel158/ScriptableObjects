 using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Potion,
    Consumable,
    QuestItem,
    Miscellaneous
}

public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[CreateAssetMenu(fileName = "Item_NewItem", menuName = "RPG/Item/Generic Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Information")]
    public string id = System.Guid.NewGuid().ToString();
    public string itemName = "New Item";
    [TextArea(2, 4)]
    public string itemDescription = "Item description";
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Miscellaneous;
    public ItemRarity rarity = ItemRarity.Common;

    [Header("Economy")]
    [Min(0)] public int itemPrice = 10; // Precio de compra
    [Min(0)] public int sellPrice = 5;  // Precio de venta

    [Header("Stack & Prefab")]
    [Min(1)] public int itemStackSize = 99;
    public GameObject itemPrefab; // Prefab en 3D / modelo

    public virtual int GetSellPrice()
    {
        return sellPrice > 0 ? sellPrice : Mathf.Max(1, itemPrice / 2);
    }
}
