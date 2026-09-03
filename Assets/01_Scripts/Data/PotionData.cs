using UnityEngine;

public enum PotionType
{
    Health,
    SpeedBuff,
    DamageBuff
}

[CreateAssetMenu(fileName = "Item_Potion", menuName = "RPG/Item/Potion")]
public class PotionData : ItemData
{
    [Header("Potion Stats")]
    public PotionType potionType = PotionType.Health;
    [Min(1)] public int healthRestoreAmount = 50;
    [Min(0f)] public float buffDuration = 0f;
    [Min(0f)] public float buffMultiplier = 1f;

    private void OnValidate()
    {
        itemType = ItemType.Potion;
    }
}
