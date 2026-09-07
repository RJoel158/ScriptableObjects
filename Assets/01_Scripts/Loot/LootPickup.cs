using UnityEngine;

public enum LootType
{
    Gold,
    Item
}

public class LootPickup : MonoBehaviour
{
    [Header("Loot Properties")]
    public LootType lootType = LootType.Gold;
    public int goldAmount = 10;
    public ItemData itemData;
    public int itemQuantity = 1;

    [Header("Animation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobbingSpeed = 2f;
    [SerializeField] private float bobbingHeight = 0.25f;

    private Vector3 startPosition;
    private bool isCollected = false;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Animación visual de giro y levitación
        transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime), Space.World);
        float newY = startPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    public void SetupGold(int amount)
    {
        lootType = LootType.Gold;
        goldAmount = amount;
    }

    public void SetupItem(ItemData item, int quantity)
    {
        lootType = LootType.Item;
        itemData = item;
        itemQuantity = quantity;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null || other.GetComponent<PlayerHealth>() != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        if (isCollected) return;
        isCollected = true;

        if (Inventory.Instance != null)
        {
            if (lootType == LootType.Gold)
            {
                Inventory.Instance.AddGold(goldAmount);
            }
            else if (lootType == LootType.Item && itemData != null)
            {
                Inventory.Instance.AddItem(itemData, itemQuantity);
            }
        }

        // Efecto visual rápido al recoger
        GameObject pickupBurst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pickupBurst.transform.position = transform.position;
        pickupBurst.transform.localScale = Vector3.one * 0.3f;
        Collider c = pickupBurst.GetComponent<Collider>();
        if (c != null) Destroy(c);
        Renderer r = pickupBurst.GetComponent<Renderer>();
        if (r != null) r.material.color = lootType == LootType.Gold ? Color.yellow : Color.cyan;
        Destroy(pickupBurst, 0.2f);

        Destroy(gameObject);
    }

    public static GameObject CreatePickup(Vector3 position, LootType type, int gold, ItemData item, int qty)
    {
        GameObject lootObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lootObj.name = type == LootType.Gold ? "Loot_Gold" : $"Loot_{(item != null ? item.itemName : "Item")}";
        lootObj.transform.position = new Vector3(position.x, 0.4f, position.z);
        lootObj.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

        Collider col = lootObj.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Renderer rend = lootObj.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material.color = type == LootType.Gold ? new Color(1f, 0.85f, 0.1f) : new Color(0.2f, 0.9f, 1f);
        }

        LootPickup pickup = lootObj.AddComponent<LootPickup>();
        if (type == LootType.Gold)
            pickup.SetupGold(gold);
        else
            pickup.SetupItem(item, qty);

        return lootObj;
    }
}
