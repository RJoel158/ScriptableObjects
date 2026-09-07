using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Player Gold & Items")]
    [SerializeField] private int gold = 100;
    public List<InventorySlot> items = new List<InventorySlot>();

    [Header("Currently Equipped")]
    public WeaponData equippedWeapon;
    public ArmorData equippedArmor;

    // Eventos para actualizar la UI y sistemas reactivos
    public event Action OnInventoryChanged;
    public event Action<int> OnGoldChanged;
    public event Action<WeaponData> OnWeaponEquipped;
    public event Action<string> OnInventoryNotification;

    public int CurrentGold => gold;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        InitializeStarterLoadout();
        OnGoldChanged?.Invoke(gold);
        OnInventoryChanged?.Invoke();
    }

    public void InitializeStarterLoadout()
    {
        if (items.Count > 0) return;

        ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
        WeaponData primary = null;
        WeaponData secondary = null;
        PotionData potion = null;
        AmmoData ammo = null;

        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (item is WeaponData weapon)
            {
                if (weapon.weaponCategory == WeaponType.Pistol && secondary == null)
                    secondary = weapon;
                else if (weapon.weaponCategory != WeaponType.Pistol && primary == null)
                    primary = weapon;
            }
            else if (item is PotionData pot && potion == null)
            {
                potion = pot;
            }
            else if (item is AmmoData am && ammo == null)
            {
                ammo = am;
            }
        }

        if (secondary != null) AddItem(secondary, 1);
        if (potion != null) AddItem(potion, 3);
        if (ammo != null) AddItem(ammo, 2);

        // Equipar pistola secundaria por defecto al iniciar
        if (secondary != null)
            EquipWeapon(secondary);
    }

    public WeaponData GetPrimaryWeapon()
    {
        foreach (var slot in items)
        {
            if (slot.item is WeaponData w && w.weaponCategory != WeaponType.Pistol)
                return w;
        }
        return null;
    }

    public WeaponData GetSecondaryWeapon()
    {
        foreach (var slot in items)
        {
            if (slot.item is WeaponData w && w.weaponCategory == WeaponType.Pistol)
                return w;
        }
        return null;
    }

    public PotionData GetFirstConsumable()
    {
        foreach (var slot in items)
        {
            if (slot.item is PotionData p)
                return p;
        }
        return null;
    }

    public void EquipPrimary()
    {
        WeaponData primary = GetPrimaryWeapon();
        if (primary != null)
        {
            EquipWeapon(primary);
        }
        else
        {
            OnInventoryNotification?.Invoke("¡Compra el Fusil AK-74 en la Tienda [T]!");
        }
    }

    public void EquipSecondary()
    {
        WeaponData secondary = GetSecondaryWeapon();
        if (secondary != null)
        {
            EquipWeapon(secondary);
        }
        else
        {
            OnInventoryNotification?.Invoke("No tienes un arma secundaria");
        }
    }

    public void UseQuickConsumable()
    {
        PotionData potion = GetFirstConsumable();
        if (potion != null)
        {
            UsePotion(potion);
        }
        else
        {
            OnInventoryNotification?.Invoke("¡No te quedan consumibles / pociones!");
        }
    }

    #region Inventory Management

    public void AddItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return;

        // Si es paquete de munición, rellenar arma directamente
        if (item is AmmoData ammo)
        {
            PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
            if (combat != null)
            {
                combat.RefillAmmo(ammo.ammoAmount * quantity);
                return;
            }
        }

        InventorySlot existingSlot = items.Find(slot => slot.item == item);
        if (existingSlot != null)
        {
            existingSlot.quantity += quantity;
        }
        else
        {
            items.Add(new InventorySlot(item, quantity));
        }

        Debug.Log($"[Inventario] +{quantity} {item.itemName}");
        OnInventoryNotification?.Invoke($"+{quantity} {item.itemName}");
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        InventorySlot existingSlot = items.Find(slot => slot.item == item);
        if (existingSlot != null)
        {
            if (existingSlot.quantity >= quantity)
            {
                existingSlot.quantity -= quantity;
                if (existingSlot.quantity <= 0)
                {
                    items.Remove(existingSlot);
                }
                Debug.Log($"[Inventario] -{quantity} {item.itemName}");
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning($"[Inventario] No hay suficiente {item.itemName} para remover {quantity}.");
                return false;
            }
        }

        Debug.LogWarning($"[Inventario] {item.itemName} no se encuentra en el inventario.");
        return false;
    }

    public int GetQuantity(ItemData item)
    {
        if (item == null) return 0;
        InventorySlot slot = items.Find(s => s.item == item);
        return slot != null ? slot.quantity : 0;
    }

    public bool HasItem(ItemData item, int minQuantity = 1)
    {
        return GetQuantity(item) >= minQuantity;
    }

    public void ClearInventory()
    {
        items.Clear();
        OnInventoryChanged?.Invoke();
    }

    #endregion

    #region Gold & Economy

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        Debug.Log($"[Oro] +{amount} Oro. Total: {gold}");
        OnInventoryNotification?.Invoke($"+{amount} Oro");
        OnGoldChanged?.Invoke(gold);
    }

    public bool RemoveGold(int amount)
    {
        if (amount <= 0) return true;
        if (gold >= amount)
        {
            gold -= amount;
            Debug.Log($"[Oro] -{amount} Oro. Total: {gold}");
            OnGoldChanged?.Invoke(gold);
            return true;
        }

        Debug.LogWarning($"[Oro] No tienes suficiente oro ({gold} de {amount} requerido).");
        return false;
    }

    public bool CanAfford(int cost)
    {
        return gold >= cost;
    }

    #endregion

    #region Buy & Sell

    public bool BuyItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        int totalCost = item.itemPrice * quantity;
        if (CanAfford(totalCost))
        {
            RemoveGold(totalCost);
            AddItem(item, quantity);
            Debug.Log($"[Tienda] Has comprado {quantity}x {item.itemName} por {totalCost} oro.");
            OnInventoryNotification?.Invoke($"Comprado: {quantity}x {item.itemName}");
            return true;
        }

        Debug.LogWarning($"[Tienda] Oro insuficiente para comprar {item.itemName}. Necesitas {totalCost} oro.");
        OnInventoryNotification?.Invoke($"¡Oro insuficiente para {item.itemName}!");
        return false;
    }

    public bool SellItem(ItemData item, int quantity = 1)
    {
        if (item == null || quantity <= 0) return false;

        if (HasItem(item, quantity))
        {
            int totalGain = item.GetSellPrice() * quantity;
            if (RemoveItem(item, quantity))
            {
                AddGold(totalGain);
                Debug.Log($"[Tienda] Has vendido {quantity}x {item.itemName} por {totalGain} oro.");
                OnInventoryNotification?.Invoke($"Vendido: {quantity}x {item.itemName} (+{totalGain}G)");
                return true;
            }
        }

        Debug.LogWarning($"[Tienda] No posees {quantity}x {item.itemName} para vender.");
        return false;
    }

    #endregion

    #region Equip & Use Items

    public void EquipWeapon(WeaponData weapon)
    {
        if (weapon == null) return;
        equippedWeapon = weapon;
        Debug.Log($"[Combate] Arma equipada: {weapon.itemName} (Daño: {weapon.damage}, Cadencia: {weapon.attackSpeed})");
        OnInventoryNotification?.Invoke($"Arma equipada: {weapon.itemName}");
        OnWeaponEquipped?.Invoke(weapon);
        OnInventoryChanged?.Invoke();
    }

    public void UsePotion(PotionData potion)
    {
        if (potion == null) return;
        if (!HasItem(potion, 1)) return;

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null)
        {
            if (potion.potionType == PotionType.Health)
            {
                if (playerHealth.CurrentHealth >= playerHealth.MaxHealth)
                {
                    OnInventoryNotification?.Invoke("¡Salud ya al máximo!");
                    return;
                }
                playerHealth.Heal(potion.healthRestoreAmount);
                RemoveItem(potion, 1);
                OnInventoryNotification?.Invoke($"Poción usada: +{potion.healthRestoreAmount} HP");
            }
        }
    }

    public void UseItem(ItemData item)
    {
        if (item == null) return;

        if (item is WeaponData weapon)
        {
            EquipWeapon(weapon);
        }
        else if (item is PotionData potion)
        {
            UsePotion(potion);
        }
        else if (item is AmmoData ammo)
        {
            PlayerCombat combat = FindAnyObjectByType<PlayerCombat>();
            if (combat != null)
            {
                combat.RefillAmmo(ammo.ammoAmount);
                RemoveItem(ammo, 1);
            }
        }
        else if (item is ArmorData armor)
        {
            equippedArmor = armor;
            OnInventoryNotification?.Invoke($"Armadura equipada: {armor.itemName}");
            OnInventoryChanged?.Invoke();
        }
    }

    #endregion
}
