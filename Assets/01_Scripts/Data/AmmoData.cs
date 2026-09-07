using UnityEngine;

[CreateAssetMenu(fileName = "Item_Ammo", menuName = "RPG/Item/Ammo Pack")]
public class AmmoData : ItemData
{
    [Header("Ammo Properties")]
    public WeaponType targetWeaponCategory = WeaponType.Rifle;
    [Min(1)] public int ammoAmount = 60;

    private void OnValidate()
    {
        itemType = ItemType.Consumable;
    }
}
