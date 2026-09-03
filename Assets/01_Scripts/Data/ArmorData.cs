using UnityEngine;

[CreateAssetMenu(fileName = "Item_Armor", menuName = "RPG/Item/Armor")]
public class ArmorData : ItemData
{
    [Header("Armor Stats")]
    [Min(1)] public int defense = 5;
    [Min(0)] public int bonusMaxHealth = 25;
    [Min(1)] public int durability = 100;

    private void OnValidate()
    {
        itemType = ItemType.Armor;
    }
}
