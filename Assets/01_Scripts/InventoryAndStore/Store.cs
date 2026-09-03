using System.Collections.Generic;
using UnityEngine;

public class Store : MonoBehaviour
{
    public static Store Instance { get; private set; }

    [Header("Store Catalog")]
    public string storeName = "Tienda del Aventurero";
    [SerializeField] public List<ItemData> catalog = new List<ItemData>();
    [SerializeField] public List<ItemData> storage = new List<ItemData>(); // Compatibilidad

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

        // Sincronizar catálogo con storage si uno de los dos está lleno
        if (catalog.Count == 0 && storage.Count > 0)
        {
            catalog.AddRange(storage);
        }
        else if (storage.Count == 0 && catalog.Count > 0)
        {
            storage.AddRange(catalog);
        }

        // Si la tienda está vacía, cargar automáticamente items de Resources o ScriptableObjects
        if (catalog.Count == 0)
        {
            ItemData[] allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            foreach (var it in allItems)
            {
                if (!catalog.Contains(it)) catalog.Add(it);
            }
        }
    }

    public bool BuyItemFromStore(ItemData item, int quantity = 1)
    {
        if (Inventory.Instance != null && item != null)
        {
            return Inventory.Instance.BuyItem(item, quantity);
        }
        return false;
    }

    public bool SellItemToStore(ItemData item, int quantity = 1)
    {
        if (Inventory.Instance != null && item != null)
        {
            return Inventory.Instance.SellItem(item, quantity);
        }
        return false;
    }
}
