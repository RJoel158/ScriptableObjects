using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoreUI : MonoBehaviour
{
    [Header("Main Panel")]
    [SerializeField] private GameObject storePanel;
    [SerializeField] private Transform catalogContainer;
    [SerializeField] private GameObject storeItemPrefab;
    [SerializeField] private TextMeshProUGUI storeGoldText;
    [SerializeField] private Button closeButton;

    private List<StoreItemUI> spawnedCards = new List<StoreItemUI>();

    void Start()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseStore);
        }

        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnGoldChanged += UpdateGoldDisplay;
            UpdateGoldDisplay(Inventory.Instance.CurrentGold);
        }

        PopulateCatalog();
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
        {
            Inventory.Instance.OnGoldChanged -= UpdateGoldDisplay;
        }
    }

    public void ToggleStore()
    {
        if (storePanel != null)
        {
            bool active = !storePanel.activeSelf;
            storePanel.SetActive(active);
            if (active)
            {
                if (Inventory.Instance != null)
                {
                    UpdateGoldDisplay(Inventory.Instance.CurrentGold);
                }
                PopulateCatalog();
            }
        }
    }

    public void OpenStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(true);
            if (Inventory.Instance != null)
            {
                UpdateGoldDisplay(Inventory.Instance.CurrentGold);
            }
            PopulateCatalog();
        }
    }

    public void CloseStore()
    {
        if (storePanel != null)
        {
            storePanel.SetActive(false);
        }
    }

    private void UpdateGoldDisplay(int currentGold)
    {
        if (storeGoldText != null)
        {
            storeGoldText.text = $"💰 Oro Disponible: {currentGold}";
        }

        foreach (var card in spawnedCards)
        {
            card.UpdateAffordability();
        }
    }

    public void PopulateCatalog()
    {
        if (catalogContainer == null || Store.Instance == null) return;

        foreach (Transform child in catalogContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedCards.Clear();

        foreach (ItemData item in Store.Instance.catalog)
        {
            if (item == null) continue;

            GameObject cardObj = storeItemPrefab != null
                ? Instantiate(storeItemPrefab, catalogContainer)
                : CreateDefaultCatalogCard(catalogContainer);

            StoreItemUI cardUI = cardObj.GetComponent<StoreItemUI>();
            if (cardUI == null) cardUI = cardObj.AddComponent<StoreItemUI>();

            cardUI.Setup(item, OnBuyItemClicked);
            spawnedCards.Add(cardUI);
        }
    }

    private void OnBuyItemClicked(ItemData item)
    {
        if (Store.Instance != null && item != null)
        {
            Store.Instance.BuyItemFromStore(item, 1);
        }
    }

    private GameObject CreateDefaultCatalogCard(Transform parent)
    {
        GameObject card = new GameObject("Card_StoreItem", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);
        return card;
    }
}
