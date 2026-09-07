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
            storeGoldText.text = $"Oro Disponible: {currentGold} G";
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
        GameObject card = new GameObject("Card_StoreItem", typeof(RectTransform), typeof(Image), typeof(StoreItemUI));
        card.transform.SetParent(parent, false);
        card.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.17f, 0.95f);

        // 1. Icono del item (Izquierda)
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRt = iconObj.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0f, 0.5f);
        iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0f, 0.5f);
        iconRt.anchoredPosition = new Vector2(10f, 10f);
        iconRt.sizeDelta = new Vector2(56f, 56f);
        Image iconImg = iconObj.GetComponent<Image>();
        iconImg.preserveAspect = true;

        // 2. Nombre del item (Arriba a la derecha del icono)
        GameObject nameObj = new GameObject("NameText", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0f, 1f);
        nameRt.anchorMax = new Vector2(1f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(74f, -8f);
        nameRt.sizeDelta = new Vector2(-80f, 24f);
        TextMeshProUGUI nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        nameTmp.fontSize = 13;
        nameTmp.fontStyle = FontStyles.Bold;
        nameTmp.color = Color.white;
        nameTmp.alignment = TextAlignmentOptions.Left;

        // 3. Estadísticas del item
        GameObject statsObj = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statsObj.transform.SetParent(card.transform, false);
        RectTransform statsRt = statsObj.GetComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0f, 1f);
        statsRt.anchorMax = new Vector2(1f, 1f);
        statsRt.pivot = new Vector2(0f, 1f);
        statsRt.anchoredPosition = new Vector2(74f, -32f);
        statsRt.sizeDelta = new Vector2(-80f, 32f);
        TextMeshProUGUI statsTmp = statsObj.GetComponent<TextMeshProUGUI>();
        statsTmp.fontSize = 10.5f;
        statsTmp.color = new Color(0.4f, 0.85f, 1f);
        statsTmp.alignment = TextAlignmentOptions.TopLeft;

        // 4. Precio (Abajo a la izquierda)
        GameObject priceObj = new GameObject("PriceText", typeof(RectTransform), typeof(TextMeshProUGUI));
        priceObj.transform.SetParent(card.transform, false);
        RectTransform priceRt = priceObj.GetComponent<RectTransform>();
        priceRt.anchorMin = new Vector2(0f, 0f);
        priceRt.anchorMax = new Vector2(0f, 0f);
        priceRt.pivot = new Vector2(0f, 0f);
        priceRt.anchoredPosition = new Vector2(10f, 8f);
        priceRt.sizeDelta = new Vector2(110f, 26f);
        TextMeshProUGUI priceTmp = priceObj.GetComponent<TextMeshProUGUI>();
        priceTmp.fontSize = 13;
        priceTmp.fontStyle = FontStyles.Bold;
        priceTmp.color = new Color(1f, 0.85f, 0.2f);
        priceTmp.alignment = TextAlignmentOptions.Left;

        // 5. Botón de Compra (Abajo a la derecha)
        GameObject btnObj = new GameObject("Btn_Buy", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(card.transform, false);
        RectTransform btnRt = btnObj.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0f);
        btnRt.anchorMax = new Vector2(1f, 0f);
        btnRt.pivot = new Vector2(1f, 0f);
        btnRt.anchoredPosition = new Vector2(-8f, 8f);
        btnRt.sizeDelta = new Vector2(95f, 28f);
        btnObj.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.32f, 1f);

        GameObject btnTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnTxtObj.transform.SetParent(btnObj.transform, false);
        RectTransform btnTxtRt = btnTxtObj.GetComponent<RectTransform>();
        btnTxtRt.anchorMin = Vector2.zero;
        btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnTmp = btnTxtObj.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Comprar";
        btnTmp.fontSize = 12;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = Color.white;
        btnTmp.alignment = TextAlignmentOptions.Center;

        // Asignar campos al componente StoreItemUI
        StoreItemUI itemUI = card.GetComponent<StoreItemUI>();
        SetField(itemUI, "iconImage", iconImg);
        SetField(itemUI, "nameText", nameTmp);
        SetField(itemUI, "statsText", statsTmp);
        SetField(itemUI, "priceText", priceTmp);
        SetField(itemUI, "buyButton", btnObj.GetComponent<Button>());
        SetField(itemUI, "buyButtonText", btnTmp);

        return card;
    }

    private static void SetField(object target, string fieldName, object value)
    {
        if (target == null) return;
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
