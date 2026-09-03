using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuntimeGameSetup : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureSceneSetup()
    {
        // 1. Asegurar EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 2. Asegurar WaveManager
        if (FindFirstObjectByType<WaveManager>() == null)
        {
            GameObject waveManagerObj = new GameObject("WaveManager", typeof(WaveManager));
            WaveManager wm = waveManagerObj.GetComponent<WaveManager>();

            // Cargar enemigos disponibles
            EnemyData[] enemies = Resources.FindObjectsOfTypeAll<EnemyData>();
            if (enemies != null && enemies.Length > 0)
            {
                wm.enemyDataList.AddRange(enemies);
            }
        }

        // 3. Asegurar Cámara siguiendo al jugador o fija cenital
        Camera cam = Camera.main;
        if (cam != null)
        {
            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 12f, player.transform.position.z - 10f);
                cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
                
                // Seguidor suave de cámara si no tiene
                if (cam.GetComponent<CameraFollow>() == null)
                {
                    CameraFollow follow = cam.gameObject.AddComponent<CameraFollow>();
                    follow.target = player.transform;
                    follow.offset = new Vector3(0f, 12f, -10f);
                }
            }
        }

        // 4. Crear suelo si no existe
        if (GameObject.Find("Ground_Plane") == null && GameObject.Find("Floor") == null && GameObject.Find("Terrain") == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground_Plane";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            ground.tag = "Obstacle";
            Renderer r = ground.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.18f, 0.22f, 0.25f);
            }
        }

        // 5. Asegurar Canvas y UI completa
        if (FindFirstObjectByType<UIManager>() == null)
        {
            CreateRuntimeUI();
        }
    }

    private static void CreateRuntimeUI()
    {
        GameObject canvasObj = new GameObject("Canvas_GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        UIManager uiManager = canvasObj.AddComponent<UIManager>();

        // Crear HUD
        GameObject hudObj = new GameObject("Panel_HUD", typeof(RectTransform), typeof(HUDUI));
        hudObj.transform.SetParent(canvasObj.transform, false);
        RectTransform hudRect = hudObj.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;

        // Crear Contenedor Superior (Vida, Oro, Oleada)
        CreateHUDTopBar(hudObj);

        // Crear Notificaciones Toast
        CreateNotificationToast(hudObj);

        // Crear Panel de Inventario
        GameObject invPanel = CreateInventoryPanel(canvasObj.transform);

        // Crear Panel de Tienda
        GameObject storePanel = CreateStorePanel(canvasObj.transform);

        // Inyectar referencias a UIManager
        SetField(uiManager, "inventoryUI", invPanel.GetComponent<InventoryUI>());
        SetField(uiManager, "storeUI", storePanel.GetComponent<StoreUI>());
        SetField(uiManager, "hudUI", hudObj.GetComponent<HUDUI>());
    }

    private static void CreateHUDTopBar(GameObject hudObj)
    {
        // Barra de Salud y HUD superior estilizado
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(hudObj.transform, false);
        RectTransform rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 70f);
        topBar.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.14f, 0.85f);

        // Vida
        GameObject healthTextObj = CreateText(topBar.transform, "HP: 100/100", 22, TextAlignmentOptions.Left, new Vector2(25f, -35f), new Vector2(250f, 40f), Color.green);
        
        // Oro
        GameObject goldTextObj = CreateText(topBar.transform, "💰 100 G", 22, TextAlignmentOptions.Center, new Vector2(0f, -35f), new Vector2(250f, 40f), new Color(1f, 0.85f, 0.2f));

        // Oleada
        GameObject waveTextObj = CreateText(topBar.transform, "⚔ Oleada: 1", 22, TextAlignmentOptions.Right, new Vector2(-25f, -35f), new Vector2(300f, 40f), Color.white);

        // Botones de acceso rápido a Inventario (I) y Tienda (T)
        CreateQuickButton(topBar.transform, "Inventario [I]", new Vector2(300f, -35f), () => UIManager.Instance?.ToggleInventory());
        CreateQuickButton(topBar.transform, "Tienda [T]", new Vector2(460f, -35f), () => UIManager.Instance?.ToggleStore());

        HUDUI hud = hudObj.GetComponent<HUDUI>();
        SetField(hud, "healthText", healthTextObj.GetComponent<TextMeshProUGUI>());
        SetField(hud, "goldText", goldTextObj.GetComponent<TextMeshProUGUI>());
        SetField(hud, "waveText", waveTextObj.GetComponent<TextMeshProUGUI>());
    }

    private static void CreateNotificationToast(GameObject hudObj)
    {
        GameObject toast = new GameObject("ToastNotification", typeof(RectTransform), typeof(Image));
        toast.transform.SetParent(hudObj.transform, false);
        RectTransform rt = toast.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.85f);
        rt.anchorMax = new Vector2(0.5f, 0.85f);
        rt.sizeDelta = new Vector2(500f, 50f);
        toast.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.08f, 0.9f);

        GameObject txt = CreateText(toast.transform, "Notificación", 20, TextAlignmentOptions.Center, Vector2.zero, new Vector2(480f, 40f), Color.white);

        HUDUI hud = hudObj.GetComponent<HUDUI>();
        SetField(hud, "notificationPanel", toast);
        SetField(hud, "notificationText", txt.GetComponent<TextMeshProUGUI>());

        toast.SetActive(false);
    }

    private static GameObject CreateInventoryPanel(Transform canvas)
    {
        GameObject panel = new GameObject("Panel_Inventory", typeof(RectTransform), typeof(Image), typeof(InventoryUI));
        panel.transform.SetParent(canvas, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.15f, 0.15f);
        rt.anchorMax = new Vector2(0.85f, 0.85f);
        rt.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

        // Título
        CreateText(panel.transform, "🎒 INVENTARIO DEL JUGADOR", 26, TextAlignmentOptions.Center, new Vector2(0f, -35f), new Vector2(500f, 40f), Color.yellow);

        // Contenedor de slots
        GameObject grid = new GameObject("SlotContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        RectTransform gridRt = grid.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.05f, 0.1f);
        gridRt.anchorMax = new Vector2(0.55f, 0.85f);
        gridRt.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(110f, 110f);
        glg.spacing = new Vector2(12f, 12f);

        // Panel de Detalle lateral
        GameObject detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
        detailPanel.transform.SetParent(panel.transform, false);
        RectTransform detailRt = detailPanel.GetComponent<RectTransform>();
        detailRt.anchorMin = new Vector2(0.6f, 0.1f);
        detailRt.anchorMax = new Vector2(0.95f, 0.85f);
        detailRt.sizeDelta = Vector2.zero;
        detailPanel.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 0.9f);

        GameObject detailName = CreateText(detailPanel.transform, "Nombre Item", 22, TextAlignmentOptions.Center, new Vector2(0f, -30f), new Vector2(300f, 35f), Color.white);
        GameObject detailType = CreateText(detailPanel.transform, "Tipo: Arma", 16, TextAlignmentOptions.Center, new Vector2(0f, -65f), new Vector2(300f, 25f), Color.cyan);
        GameObject detailStats = CreateText(detailPanel.transform, "Estadísticas", 18, TextAlignmentOptions.Left, new Vector2(20f, -120f), new Vector2(280f, 100f), Color.white);
        GameObject detailDesc = CreateText(detailPanel.transform, "Descripción", 15, TextAlignmentOptions.Left, new Vector2(20f, -220f), new Vector2(280f, 80f), Color.gray);
        GameObject detailPrice = CreateText(detailPanel.transform, "Precio Venta: 10G", 18, TextAlignmentOptions.Left, new Vector2(20f, -300f), new Vector2(280f, 30f), Color.yellow);

        // Botones de acción
        GameObject btnEquip = CreateActionButton(detailPanel.transform, "Equipar", new Vector2(-75f, 40f));
        GameObject btnSell = CreateActionButton(detailPanel.transform, "Vender", new Vector2(75f, 40f));

        InventoryUI invUI = panel.GetComponent<InventoryUI>();
        SetField(invUI, "inventoryPanel", panel);
        SetField(invUI, "slotContainer", grid.transform);
        SetField(invUI, "detailPanel", detailPanel);
        SetField(invUI, "detailName", detailName.GetComponent<TextMeshProUGUI>());
        SetField(invUI, "detailType", detailType.GetComponent<TextMeshProUGUI>());
        SetField(invUI, "detailStats", detailStats.GetComponent<TextMeshProUGUI>());
        SetField(invUI, "detailDescription", detailDesc.GetComponent<TextMeshProUGUI>());
        SetField(invUI, "detailSellPrice", detailPrice.GetComponent<TextMeshProUGUI>());
        SetField(invUI, "equipButton", btnEquip.GetComponent<Button>());
        SetField(invUI, "sellButton", btnSell.GetComponent<Button>());

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateStorePanel(Transform canvas)
    {
        GameObject panel = new GameObject("Panel_Store", typeof(RectTransform), typeof(Image), typeof(StoreUI));
        panel.transform.SetParent(canvas, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.2f, 0.15f);
        rt.anchorMax = new Vector2(0.8f, 0.85f);
        rt.sizeDelta = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.18f, 0.96f);

        CreateText(panel.transform, "🛒 TIENDA DE ARMAS & OBJETOS", 26, TextAlignmentOptions.Center, new Vector2(0f, -35f), new Vector2(500f, 40f), new Color(1f, 0.8f, 0.2f));

        GameObject catalog = new GameObject("CatalogContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        catalog.transform.SetParent(panel.transform, false);
        RectTransform catRt = catalog.GetComponent<RectTransform>();
        catRt.anchorMin = new Vector2(0.08f, 0.12f);
        catRt.anchorMax = new Vector2(0.92f, 0.82f);
        catRt.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = catalog.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(240f, 130f);
        glg.spacing = new Vector2(20f, 20f);

        StoreUI storeUI = panel.GetComponent<StoreUI>();
        SetField(storeUI, "storePanel", panel);
        SetField(storeUI, "catalogContainer", catalog.transform);

        panel.SetActive(false);
        return panel;
    }

    private static GameObject CreateText(Transform parent, string text, float size, TextAlignmentOptions align, Vector2 pos, Vector2 delta, Color color)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = delta;

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        return obj;
    }

    private static GameObject CreateQuickButton(Transform parent, string text, Vector2 pos, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(130f, 36f);
        btnObj.GetComponent<Image>().color = new Color(0.2f, 0.28f, 0.38f, 1f);

        GameObject txt = CreateText(btnObj.transform, text, 15, TextAlignmentOptions.Center, Vector2.zero, new Vector2(125f, 30f), Color.white);
        btnObj.GetComponent<Button>().onClick.AddListener(onClick);
        return btnObj;
    }

    private static GameObject CreateActionButton(Transform parent, string text, Vector2 pos)
    {
        GameObject btnObj = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(120f, 40f);
        btnObj.GetComponent<Image>().color = new Color(0.18f, 0.55f, 0.35f, 1f);

        CreateText(btnObj.transform, text, 16, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 30f), Color.white);
        return btnObj;
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
