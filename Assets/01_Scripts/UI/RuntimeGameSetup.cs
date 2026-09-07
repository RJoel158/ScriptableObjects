using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuntimeGameSetup : MonoBehaviour
{
    [Header("Configuración Opcional")]
    [Tooltip("Marca esta casilla para que genere automáticamente la UI/Escena al iniciar si no existe")]
    public bool executeOnStart = true;

    void Start()
    {
        if (executeOnStart)
        {
            EnsureSceneSetup();
        }
    }

    [ContextMenu("Generar UI y Escena Ahora")]
    public void BuildSceneNow()
    {
        EnsureSceneSetup();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureSceneSetup()
    {
        // 1. Asegurar EventSystem
        if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // 2. Asegurar WaveManager
        if (FindAnyObjectByType<WaveManager>() == null)
        {
            GameObject waveManagerObj = new GameObject("WaveManager", typeof(WaveManager));
            WaveManager wm = waveManagerObj.GetComponent<WaveManager>();

            EnemyData[] enemies = Resources.FindObjectsOfTypeAll<EnemyData>();
            if (enemies != null && enemies.Length > 0)
            {
                foreach (var e in enemies)
                {
                    if (e != null)
                    {
                        if (e.name.ToLower().Contains("mutant") || e.enemyType == EnemyType.MutantBoss)
                        {
                            wm.bossEnemyData = e;
                        }
                        else if (!wm.enemyDataList.Contains(e))
                        {
                            wm.enemyDataList.Add(e);
                        }
                    }
                }
            }
        }

        // 3. Asegurar Cámara con controlador de perspectiva (1ra y 3ra persona)
        Camera cam = Camera.main;
        if (cam != null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                PerspectiveCameraController camCtrl = cam.GetComponent<PerspectiveCameraController>();
                if (camCtrl == null)
                {
                    camCtrl = cam.gameObject.AddComponent<PerspectiveCameraController>();
                }
                camCtrl.targetPlayer = player.transform;
            }
        }

        // 4. Asegurar un único suelo sin planos duplicados ni Z-Fighting
        GameObject existingGround = GameObject.Find("Ground");
        if (existingGround == null) existingGround = GameObject.FindWithTag("Ground");
        if (existingGround == null) existingGround = GameObject.Find("Floor");
        if (existingGround == null) existingGround = GameObject.Find("Terrain");

        GameObject duplicatePlane = GameObject.Find("Ground_Plane");
        if (duplicatePlane != null && existingGround != null && duplicatePlane != existingGround)
        {
            Object.DestroyImmediate(duplicatePlane);
        }

        if (existingGround == null && duplicatePlane == null)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.tag = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(6f, 1f, 6f);
            Renderer r = ground.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.18f, 0.22f, 0.25f);
            }
        }

        // 5. Limpiar cualquier barra o Canvas legado
        GameObject oldTopBar = GameObject.Find("TopBar");
        if (oldTopBar != null) Object.DestroyImmediate(oldTopBar);

        // 6. Asegurar Mercader Físico (NPC de la Tienda)
        EnsureMerchantNPC();

        if (FindAnyObjectByType<UIManager>() == null)
        {
            CreateRuntimeUI();
        }
    }

    private static void EnsureMerchantNPC()
    {
        // Limpiar cualquier NPC placeholder o cilindro anterior
        MerchantNPC[] existingNpcs = Object.FindObjectsByType<MerchantNPC>(FindObjectsSortMode.None);
        foreach (var npc in existingNpcs)
        {
            if (npc != null)
            {
                MeshFilter mf = npc.GetComponent<MeshFilter>();
                // Si es un cilindro primitivo o no tiene mallas hijas del mercader 3D
                if ((mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cylinder")) || npc.transform.childCount <= 1)
                {
                    Object.DestroyImmediate(npc.gameObject);
                }
                else
                {
                    return; // Ya existe el modelo 3D real
                }
            }
        }

        GameObject oldObj = GameObject.Find("Merchant_NPC");
        if (oldObj != null)
        {
            MeshFilter mf = oldObj.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null && mf.sharedMesh.name.Contains("Cylinder"))
            {
                Object.DestroyImmediate(oldObj);
            }
        }

        // Buscar modelo 3D del mercader
        GameObject merchantModel = null;
#if UNITY_EDITOR
        merchantModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/05_Models/Merchant.fbx");
        if (merchantModel == null)
        {
            merchantModel = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Merchant.fbx");
        }
#endif
        if (merchantModel == null)
        {
            merchantModel = Resources.Load<GameObject>("Merchant");
        }
        if (merchantModel == null)
        {
            merchantModel = Resources.Load<GameObject>("Merchant_NPC");
        }

        Vector3 spawnPos = new Vector3(4.5f, 0f, 4.5f);
        GameObject merchantObj;

        if (merchantModel != null)
        {
            merchantObj = Object.Instantiate(merchantModel, spawnPos, Quaternion.Euler(0, -135, 0));
            merchantObj.name = "Merchant_NPC";
        }
        else
        {
            merchantObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            merchantObj.name = "Merchant_NPC";
            merchantObj.transform.position = spawnPos;
            merchantObj.transform.rotation = Quaternion.Euler(0, -135, 0);
            Renderer r = merchantObj.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.2f, 0.5f, 0.8f);
        }

        CapsuleCollider col = merchantObj.GetComponent<CapsuleCollider>();
        if (col == null && merchantObj.GetComponent<Collider>() == null)
        {
            col = merchantObj.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.9f, 0);
            col.height = 1.8f;
            col.radius = 0.45f;
        }

        MerchantNPC merchantScript = merchantObj.GetComponent<MerchantNPC>();
        if (merchantScript == null) merchantObj.AddComponent<MerchantNPC>();

        // Pequeña luz de ambiente cálida para el mercader
        Transform existingLight = merchantObj.transform.Find("Merchant_Light");
        if (existingLight == null)
        {
            GameObject lightObj = new GameObject("Merchant_Light", typeof(Light));
            lightObj.transform.SetParent(merchantObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 2.2f, 0.5f);
            Light l = lightObj.GetComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.82f, 0.45f);
            l.range = 8f;
            l.intensity = 2.5f;
        }
    }

    private static void CreateRuntimeUI()
    {
        GameObject canvasObj = new GameObject("Canvas_GameUI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) canvasObj.layer = uiLayer;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        UIManager uiManager = canvasObj.AddComponent<UIManager>();

        GameObject hudObj = new GameObject("Panel_HUD", typeof(RectTransform), typeof(HUDUI));
        hudObj.transform.SetParent(canvasObj.transform, false);
        if (uiLayer >= 0) hudObj.layer = uiLayer;
        RectTransform hudRect = hudObj.GetComponent<RectTransform>();
        hudRect.anchorMin = Vector2.zero;
        hudRect.anchorMax = Vector2.one;
        hudRect.sizeDelta = Vector2.zero;

        CreateCrosshair(hudObj);
        CreateHUDTopLeftStats(hudObj);
        CreateHUDTopRightWave(hudObj);
        CreateHUDAmmoPanel(hudObj);
        CreateNotificationToast(hudObj);
        GameObject invPanel = CreateInventoryPanel(canvasObj.transform);
        GameObject storePanel = CreateStorePanel(canvasObj.transform);

        SetField(uiManager, "inventoryUI", invPanel.GetComponent<InventoryUI>());
        SetField(uiManager, "storeUI", storePanel.GetComponent<StoreUI>());
        SetField(uiManager, "hudUI", hudObj.GetComponent<HUDUI>());
    }

    private static void CreateCrosshair(GameObject hudObj)
    {
        GameObject crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshair.transform.SetParent(hudObj.transform, false);
        RectTransform rt = crosshair.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(6f, 6f);
        Image img = crosshair.GetComponent<Image>();
        img.color = new Color(0.2f, 1f, 0.85f, 0.9f);
    }

    private static void CreateHUDTopLeftStats(GameObject hudObj)
    {
        // Panel contenedor Vida & Oro (Esquina superior izquierda)
        GameObject card = new GameObject("Card_PlayerStats", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(hudObj.transform, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(25f, -25f);
        rt.sizeDelta = new Vector2(300f, 82f);
        card.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.85f);

        // Barra de Vida
        GameObject sliderObj = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(card.transform, false);
        RectTransform sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.05f, 0.50f);
        sliderRt.anchorMax = new Vector2(0.95f, 0.88f);
        sliderRt.sizeDelta = Vector2.zero;
        Slider slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 100f;

        // Fondo de la barra
        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.18f, 0.2f, 0.25f, 0.9f);

        // Relleno de la barra
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = Vector2.zero;
        fillAreaRt.anchorMax = Vector2.one;
        fillAreaRt.sizeDelta = Vector2.zero;

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillObj.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;
        Image fillImg = fillObj.GetComponent<Image>();
        fillImg.color = new Color(0.18f, 0.80f, 0.44f, 1f); // Verde esmeralda vivo
        slider.fillRect = fillRt;

        // Texto HP centrado sobre la barra
        GameObject healthTextObj = CreateText(card.transform, "HP: 100 / 100", 17, TextAlignmentOptions.Center, new Vector2(0f, 18f), new Vector2(280f, 30f), Color.white);
        
        // Texto Oro
        GameObject goldTextObj = CreateText(card.transform, "ORO: 100 G", 19, TextAlignmentOptions.Left, new Vector2(15f, -22f), new Vector2(270f, 30f), new Color(1f, 0.85f, 0.2f));

        HUDUI hud = hudObj.GetComponent<HUDUI>();
        SetField(hud, "healthSlider", slider);
        SetField(hud, "healthText", healthTextObj.GetComponent<TextMeshProUGUI>());
        SetField(hud, "goldText", goldTextObj.GetComponent<TextMeshProUGUI>());
    }

    private static void CreateHUDTopRightWave(GameObject hudObj)
    {
        // Panel contenedor Ronda & Zombies (Esquina superior derecha)
        GameObject card = new GameObject("Card_WaveInfo", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(hudObj.transform, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-25f, -25f);
        rt.sizeDelta = new Vector2(300f, 80f);
        card.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.85f);

        GameObject waveTextObj = CreateText(card.transform, "RONDA: 1", 20, TextAlignmentOptions.Right, new Vector2(-15f, 16f), new Vector2(270f, 30f), new Color(1f, 0.85f, 0.25f));
        GameObject enemyTextObj = CreateText(card.transform, "ZOMBIES: 0", 18, TextAlignmentOptions.Right, new Vector2(-15f, -20f), new Vector2(270f, 30f), new Color(0.35f, 0.9f, 1f));

        HUDUI hud = hudObj.GetComponent<HUDUI>();
        SetField(hud, "waveText", waveTextObj.GetComponent<TextMeshProUGUI>());
        SetField(hud, "enemiesRemainingText", enemyTextObj.GetComponent<TextMeshProUGUI>());
    }

    private static void CreateHUDAmmoPanel(GameObject hudObj)
    {
        // Panel contenedor Arma & Munición (Esquina inferior derecha - Estilo Resident Evil)
        GameObject card = new GameObject("Card_AmmoPanel", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(hudObj.transform, false);
        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-25f, 25f);
        rt.sizeDelta = new Vector2(280f, 90f);
        card.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.13f, 0.88f);

        GameObject weaponNameObj = CreateText(card.transform, "Fusil AK-74", 17, TextAlignmentOptions.Right, new Vector2(-15f, 20f), new Vector2(250f, 28f), new Color(0.35f, 0.9f, 1f));
        GameObject ammoTextObj = CreateText(card.transform, "30 / 180 [R]", 24, TextAlignmentOptions.Right, new Vector2(-15f, -16f), new Vector2(250f, 36f), Color.white);

        HUDUI hud = hudObj.GetComponent<HUDUI>();
        SetField(hud, "weaponNameText", weaponNameObj.GetComponent<TextMeshProUGUI>());
        SetField(hud, "ammoText", ammoTextObj.GetComponent<TextMeshProUGUI>());
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

        CreateText(panel.transform, "INVENTARIO DEL JUGADOR", 26, TextAlignmentOptions.Center, new Vector2(0f, -35f), new Vector2(500f, 40f), Color.yellow);

        GameObject grid = new GameObject("SlotContainer", typeof(RectTransform), typeof(GridLayoutGroup));
        grid.transform.SetParent(panel.transform, false);
        RectTransform gridRt = grid.GetComponent<RectTransform>();
        gridRt.anchorMin = new Vector2(0.05f, 0.1f);
        gridRt.anchorMax = new Vector2(0.55f, 0.85f);
        gridRt.sizeDelta = Vector2.zero;

        GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(110f, 110f);
        glg.spacing = new Vector2(12f, 12f);

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

        CreateText(panel.transform, "TIENDA DE ARMAS & OBJETOS", 26, TextAlignmentOptions.Center, new Vector2(0f, -35f), new Vector2(500f, 40f), new Color(1f, 0.8f, 0.2f));

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
