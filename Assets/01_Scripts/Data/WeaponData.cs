using UnityEngine;

public enum WeaponType
{
    Sword,
    Bow,
    MagicWand,
    Rifle,
    Staff
}

[CreateAssetMenu(fileName = "Item_Weapon", menuName = "RPG/Item/Weapon")]
public class WeaponData : ItemData
{
    [Header("Weapon Stats")]
    public WeaponType weaponCategory = WeaponType.Sword;
    [Min(1)] public int damage = 20;
    [Tooltip("Ataques/disparos por segundo")]
    [Min(0.1f)] public float attackSpeed = 1.5f;
    [Min(1f)] public float projectileSpeed = 20f;
    [Min(1f)] public float attackRange = 25f;
    public bool isRanged = true;

    [Header("Visual & Prefabs")]
    public GameObject weaponModelPrefab; // Modelo que va en la mano del jugador
    public GameObject projectilePrefab;  // Proyectil que se dispara
    public Color projectileColor = Color.yellow;
    public Vector3 weaponEquipOffset = Vector3.zero;
    public Vector3 weaponEquipRotation = Vector3.zero;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
    }
}
