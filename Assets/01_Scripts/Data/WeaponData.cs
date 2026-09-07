using UnityEngine;

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    SMG,
    RocketLauncher,
    Sword,
    Bow,
    MagicWand
}

[CreateAssetMenu(fileName = "Item_Weapon", menuName = "RPG/Item/Weapon")]
public class WeaponData : ItemData
{
    [Header("Weapon Stats")]
    public WeaponType weaponCategory = WeaponType.Pistol;
    [Min(1)] public int damage = 20;
    [Tooltip("Ataques/disparos por segundo")]
    [Min(0.1f)] public float attackSpeed = 2.5f;
    [Min(1f)] public float projectileSpeed = 35f;
    [Min(1f)] public float attackRange = 40f;
    public bool isRanged = true;

    [Header("Sistema de Munición (Ammo)")]
    public bool usesAmmo = true;
    [Tooltip("Capacidad del cargador")]
    [Min(1)] public int magazineCapacity = 12;
    [Tooltip("Munición máxima en reserva")]
    [Min(0)] public int maxReserveAmmo = 120;
    [Tooltip("Tiempo de recarga en segundos")]
    [Min(0.1f)] public float reloadDuration = 1.5f;

    [Header("Animaciones Específicas del Arma")]
    [Tooltip("Animator Override opcional para cambiar automáticamente las animaciones al equipar esta arma")]
    public AnimatorOverrideController animatorOverride;

    [Header("Visual & Prefabs")]
    public GameObject weaponModelPrefab; // Modelo que va en la mano del jugador
    public GameObject projectilePrefab;  // Proyectil que se dispara
    public Color projectileColor = Color.yellow;
    public Vector3 weaponEquipOffset = Vector3.zero;
    public Vector3 weaponEquipRotation = Vector3.zero;
    public Vector3 weaponScale = Vector3.one;

    private void OnValidate()
    {
        itemType = ItemType.Weapon;
    }
}
