using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// StageMapController — Điều phối màn hình Bản Đồ Ải (StageMap):
/// - Bố cục chuẩn Dark Fantasy: Thanh Header gọn gàng trên đỉnh, Bản Đồ 3 Ải với đường nối ở giữa,
///   và Thanh Điều Khiển đáy gồm Khung Thông Tin Ải bên trái & Cụm 3 Nút Hành Động bên phải.
/// - Cho phép di chuyển Hiệp Sĩ qua các ải, mở Xưởng Rèn Xúc Xắc và Tiến tới khiêu chiến ải kế tiếp.
/// </summary>
public class StageMapController : MonoBehaviour
{
    [Header("Map Visual Assets")]
    public Sprite mapBackgroundSprite;
    public Sprite heroTokenSprite;
    public Sprite goblinSprite;
    public Sprite orcSprite;
    public Sprite minotaurSprite;
    public Sprite forgeIconSprite;
    public Sprite attackIconSprite;
    public Sprite shieldIconSprite;

    // Tọa độ các nút Ải trên Canvas 1920x1080 (khớp chính xác với đường đi trên World_Map_3Stages_Overworld.png)
    private static readonly Vector2 Stage1Pos = new Vector2(-580f, 130f);
    private static readonly Vector2 Stage2Pos = new Vector2(-25f, -35f);
    private static readonly Vector2 Stage3Pos = new Vector2(590f, 25f);
    private static readonly Vector2 CombatCheckpointPos = new Vector2(-475f, 285f);

    // Các điểm mốc (Waypoints) nối giữa Ải 1 -> Ải 2 và Ải 2 -> Ải 3
    private static readonly Vector2[] Path1To2Waypoints = new Vector2[]
    {
        new Vector2(-580f, 130f),
        new Vector2(-525f, 185f),
        new Vector2(-495f, 235f),
        new Vector2(-460f, 265f),
        new Vector2(-415f, 250f),
        new Vector2(-365f, 220f),
        new Vector2(-315f, 185f),
        new Vector2(-260f, 145f),
        new Vector2(-205f, 100f),
        new Vector2(-155f, 55f),
        new Vector2(-105f, 12f),
        new Vector2(-60f, -18f),
        new Vector2(-25f, -35f)
    };

    private static readonly Vector2[] Path2To3Waypoints = new Vector2[]
    {
        new Vector2(-25f, -35f),
        new Vector2(65f, -20f),
        new Vector2(155f, 10f),
        new Vector2(245f, -25f),
        new Vector2(330f, -70f),
        new Vector2(405f, -115f),
        new Vector2(470f, -80f),
        new Vector2(530f, -30f),
        new Vector2(590f, 25f)
    };

    private System.Type tmpType;
    private Canvas rootCanvas;

    // Trạng thái tiến trình trên Map
    private int currentTargetStage = 2;
    private int heroMapStage = 1;
    private int highestUnlockedStage = 2;
    private int selectedInspectStage = 2;
    private bool isHeroMoving = false;
    private bool isTransitioningScene = false;

    // Tham chiếu UI động
    private RectTransform heroTokenRect;
    private Image heroHaloImage;
    private Component hpStatusLabel;
    private Component forgeHeaderButtonLabel;
    private Component stageDetailTitleLabel;
    private Component stageProgressLabel;

    private Button moveNextStageButton;
    private Button enterCombatButton;
    private Button openForgeButton;
    private Image openForgeGlowOutlineImg;

    private readonly List<GameObject> stageNodeObjects = new List<GameObject>();
    private readonly List<Outline> stageNodeOutlines = new List<Outline>();
    private readonly List<Component> stageNodeStatusLabels = new List<Component>();
    private readonly List<Image> pathSegmentImages1To2 = new List<Image>();
    private readonly List<Image> pathSegmentImages2To3 = new List<Image>();

    private static Sprite cachedCircleSprite;

    private void Awake()
    {
        tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        EnsureCircleSprite();
        EnsureCameraAndEventSystem();
    }

    private void Start()
    {
        GameManager gm = GameManager.ResolveInstance();
        SyncProgressionStateFromGameManager(gm);
        ResolveFallbackSpritesFromGameManager(gm);

        BuildOrRefreshMapSceneUI();
        RefreshAllMapVisuals();
    }

    private void Update()
    {
        AnimateHeroTokenAndTargetNodes();
        HandleKeyboardShortcuts();
    }

    // =========================================================================
    // 1. ĐỒNG BỘ DỮ LIỆU TIẾN TRÌNH TỪ GAMEMANAGER
    // =========================================================================

    private void SyncProgressionStateFromGameManager(GameManager gm)
    {
        if (gm != null)
        {
            gm.EnsureDiceBagInitialized();

            // Nếu người chơi mở trực tiếp scene StageMap trong Editor để test khi chưa đánh trận nào
            if (gm.lastClearedStageIndex == 0 && gm.currentStageIndex <= 1 && !gm.isRunVictory)
            {
                gm.lastClearedStageIndex = 1;
                gm.heroMapStageIndex = 1;
                gm.currentStageIndex = 2;
                gm.highestUnlockedStageIndex = 2;
                gm.pendingForgeUpgrades = Mathf.Max(1, gm.pendingForgeUpgrades);
            }

            int totalStages = gm.GetTotalStages();
            currentTargetStage = Mathf.Clamp(gm.currentStageIndex, 1, totalStages);
            highestUnlockedStage = Mathf.Clamp(Mathf.Max(gm.highestUnlockedStageIndex, currentTargetStage), 1, totalStages);
            heroMapStage = Mathf.Clamp(gm.heroMapStageIndex, 1, highestUnlockedStage);
            selectedInspectStage = currentTargetStage;
        }
        else
        {
            currentTargetStage = 2;
            highestUnlockedStage = 2;
            heroMapStage = 1;
            selectedInspectStage = 2;
        }
    }

    private void ResolveFallbackSpritesFromGameManager(GameManager gm)
    {
        if (gm == null || gm.stageEnemies == null) return;

        if (goblinSprite == null && gm.stageEnemies.Length > 0 && gm.stageEnemies[0] != null)
            goblinSprite = gm.stageEnemies[0].enemySprite;

        if (orcSprite == null && gm.stageEnemies.Length > 1 && gm.stageEnemies[1] != null)
            orcSprite = gm.stageEnemies[1].enemySprite;

        if (minotaurSprite == null && gm.stageEnemies.Length > 2 && gm.stageEnemies[2] != null)
            minotaurSprite = gm.stageEnemies[2].enemySprite;
    }

    // =========================================================================
    // 2. XÂY DỰNG GIAO DIỆN BẢN ĐỒ ẢI CHUẨN DARK FANTASY (HEADER, MAP, FOOTER)
    // =========================================================================

    private void BuildOrRefreshMapSceneUI()
    {
        rootCanvas = FindAnyObjectByType<Canvas>();
        if (rootCanvas == null)
        {
            GameObject cvGo = new GameObject("Canvas");
            rootCanvas = cvGo.AddComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = cvGo.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight = 0.5f;
            cvGo.AddComponent<GraphicRaycaster>();
        }

        Transform existingRoot = rootCanvas.transform.Find("StageMapRoot");
        if (existingRoot != null)
        {
            Destroy(existingRoot.gameObject);
        }

        stageNodeObjects.Clear();
        stageNodeOutlines.Clear();
        stageNodeStatusLabels.Clear();
        pathSegmentImages1To2.Clear();
        pathSegmentImages2To3.Clear();

        GameObject mapRoot = new GameObject("StageMapRoot");
        mapRoot.transform.SetParent(rootCanvas.transform, false);
        RectTransform rootRt = mapRoot.AddComponent<RectTransform>();
        FullFill(rootRt);

        // 1. Hình nền Bản Đồ Thế Giới (World_Map_3Stages_Overworld)
        BuildMapBackground(mapRoot.transform);

        // 2. Tuyến đường nối các Ải & Điểm mốc chiến đấu trên bản đồ
        GameObject roadsLayer = new GameObject("RoadsLayer");
        roadsLayer.transform.SetParent(mapRoot.transform, false);
        FullFill(roadsLayer.AddComponent<RectTransform>());

        BuildRoadSegments(roadsLayer.transform, Path1To2Waypoints, pathSegmentImages1To2);
        BuildRoadSegments(roadsLayer.transform, Path2To3Waypoints, pathSegmentImages2To3);
        BuildCombatCheckpointMarker(roadsLayer.transform, CombatCheckpointPos);

        // 3. Các nút Ải (Ải 1, Ải 2, Ải 3) trên Map
        GameObject nodesLayer = new GameObject("NodesLayer");
        nodesLayer.transform.SetParent(mapRoot.transform, false);
        FullFill(nodesLayer.AddComponent<RectTransform>());

        BuildStageNode(nodesLayer.transform, 1, Stage1Pos, "ẢI 1: RỪNG NGUYỀN RỦA", goblinSprite, false);
        BuildStageNode(nodesLayer.transform, 2, Stage2Pos, "ẢI 2: THÁP HẮC ÁM", orcSprite, false);
        BuildStageNode(nodesLayer.transform, 3, Stage3Pos, "BOSS ẢI 3: PHÁO ĐÀI BOSS", minotaurSprite, true);

        // 4. Quân cờ Hiệp Sĩ (Hero Token) đứng trên Bản Đồ
        BuildHeroToken(mapRoot.transform);

        // 5. Thanh Header Đỉnh (Tiêu đề, Máu Hiệp Sĩ, Lượt Rèn, Bách Khoa, Menu)
        BuildTopHeaderBar(mapRoot.transform);

        // 6. Bảng Điều Khiển Đáy (Khung thông tin Ải bên trái + Cụm 3 nút hành động bên phải)
        BuildBottomControlPanel(mapRoot.transform);

        // 7. Khung viền kim loại Gothic bao quanh màn hình
        BuildScreenGothicFrame(mapRoot.transform);

        // 8. Nếu đã chiến thắng toàn bộ 3 ải thì hiển thị hộp thoại Vinh Danh Chiến Thắng
        if (GameManager.Instance != null && GameManager.Instance.isRunVictory)
        {
            BuildRunVictoryBanner(mapRoot.transform);
        }
    }

    private void BuildMapBackground(Transform parent)
    {
        EnsureMapBackgroundSprite();

        GameObject bgGo = new GameObject("MapBackground");
        bgGo.transform.SetParent(parent, false);
        FullFill(bgGo.AddComponent<RectTransform>());

        Image bgImg = bgGo.AddComponent<Image>();
        if (mapBackgroundSprite != null)
        {
            bgImg.sprite = mapBackgroundSprite;
            bgImg.color = Color.white;
        }
        else
        {
            bgImg.color = new Color(0.08f, 0.06f, 0.12f, 1f);
        }
        bgImg.raycastTarget = false;

        // Lớp phủ làm dịu nhẹ để các Nút Ải & Đường đi nổi bật rõ ràng
        GameObject vignetteGo = new GameObject("MapAtmosphereOverlay");
        vignetteGo.transform.SetParent(parent, false);
        FullFill(vignetteGo.AddComponent<RectTransform>());
        Image vigImg = vignetteGo.AddComponent<Image>();
        vigImg.color = new Color(0.03f, 0.02f, 0.06f, 0.18f);
        vigImg.raycastTarget = false;
    }

    private void EnsureMapBackgroundSprite()
    {
#if UNITY_EDITOR
        if (mapBackgroundSprite == null)
            mapBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Backgrounds 1/World_Map_3Stages_Overworld.png");
        if (mapBackgroundSprite == null)
            mapBackgroundSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/World_Map_Overworld.png");
#endif
        if (mapBackgroundSprite == null)
            mapBackgroundSprite = LoadSpriteFromFile("Assets/Sprites/Backgrounds 1/World_Map_3Stages_Overworld.png");
        if (mapBackgroundSprite == null)
            mapBackgroundSprite = LoadSpriteFromFile("Assets/GameAssets/World_Map_Overworld.png");
    }

    private static Sprite LoadSpriteFromFile(string path)
    {
        if (!System.IO.File.Exists(path)) return null;
        try
        {
            byte[] bytes = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
        }
        catch { }
        return null;
    }

    private void BuildRoadSegments(Transform parent, Vector2[] waypoints, List<Image> segmentList)
    {
        if (waypoints == null || waypoints.Length < 2) return;

        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Vector2 a = waypoints[i];
            Vector2 b = waypoints[i + 1];
            Vector2 diff = b - a;
            float dist = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            // Vẽ các chấm đá cổ ngữ màu vàng hổ phách sáng dọc theo đường đi
            int steps = Mathf.Max(2, Mathf.RoundToInt(dist / 34f));
            for (int s = 1; s < steps; s++)
            {
                float t = s / (float)steps;
                Vector2 dotPos = Vector2.Lerp(a, b, t);

                GameObject dotGo = new GameObject($"RoadStep_{i}_{s}");
                dotGo.transform.SetParent(parent, false);
                RectTransform dotRt = dotGo.AddComponent<RectTransform>();
                dotRt.anchoredPosition = dotPos;
                dotRt.sizeDelta = new Vector2(16f, 16f);
                dotRt.localRotation = Quaternion.Euler(0f, 0f, angle);

                Image dotImg = dotGo.AddComponent<Image>();
                dotImg.sprite = cachedCircleSprite;
                dotImg.color = new Color(0.98f, 0.74f, 0.25f, 0.95f);
                dotImg.raycastTarget = false;

                Outline ol = dotGo.AddComponent<Outline>();
                ol.effectColor = new Color(0.08f, 0.05f, 0.02f, 0.95f);
                ol.effectDistance = new Vector2(1.5f, -1.5f);

                segmentList.Add(dotImg);
            }
        }
    }

    private void BuildCombatCheckpointMarker(Transform parent, Vector2 pos)
    {
        GameObject cpGo = new GameObject("CombatCheckpointMarker");
        cpGo.transform.SetParent(parent, false);
        RectTransform rt = cpGo.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(46f, 46f);

        Image bg = cpGo.AddComponent<Image>();
        bg.sprite = cachedCircleSprite;
        bg.color = new Color(0.18f, 0.12f, 0.06f, 0.98f);
        bg.raycastTarget = false;

        Outline ol = cpGo.AddComponent<Outline>();
        ol.effectColor = new Color(0.98f, 0.78f, 0.28f, 1f);
        ol.effectDistance = new Vector2(2f, -2f);

        CreateTMPChild(cpGo.transform, "CrossedSwords", "⚔", Vector2.zero, new Vector2(40f, 40f), 22f, new Color(1f, 0.88f, 0.45f, 1f));

        // Mũi tên nhỏ chỉ xuống
        CreateTMPChild(cpGo.transform, "DownArrow", "▼", new Vector2(0f, -28f), new Vector2(24f, 20f), 13f, new Color(1f, 0.82f, 0.25f, 0.95f));
    }

    private void BuildStageNode(
        Transform parent,
        int stageNumber,
        Vector2 pos,
        string stageTitle,
        Sprite enemyPortrait,
        bool isBoss)
    {
        GameObject nodeGo = new GameObject($"StageNode_{stageNumber}");
        nodeGo.transform.SetParent(parent, false);
        RectTransform rt = nodeGo.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        Vector2 nodeSize = isBoss ? new Vector2(146f, 146f) : new Vector2(124f, 124f);
        rt.sizeDelta = nodeSize;

        Image outerRing = nodeGo.AddComponent<Image>();
        outerRing.sprite = cachedCircleSprite;
        Color nodeRingColor = stageNumber == 1
            ? new Color(0.25f, 0.95f, 0.55f, 1f) // Ải 1: Viền Xanh Ngọc Emerald
            : (stageNumber == 2
                ? new Color(1f, 0.82f, 0.25f, 1f)  // Ải 2: Viền Vàng Hổ Phách
                : new Color(0.95f, 0.22f, 0.22f, 1f)); // Ải 3: Viền Đỏ Huyết Boss

        outerRing.color = new Color(0.08f, 0.07f, 0.12f, 0.96f);

        Outline nodeOl = nodeGo.AddComponent<Outline>();
        nodeOl.effectColor = nodeRingColor;
        nodeOl.effectDistance = new Vector2(3.5f, -3.5f);

        int capturedStage = stageNumber;
        Button btn = nodeGo.AddComponent<Button>();
        btn.targetGraphic = outerRing;
        btn.onClick.AddListener(() => OnStageNodeClicked(capturedStage));

        // Khung chân dung Quái Vật bên trong Node
        GameObject innerGo = new GameObject("PortraitFrame");
        innerGo.transform.SetParent(nodeGo.transform, false);
        RectTransform innerRt = innerGo.AddComponent<RectTransform>();
        innerRt.anchoredPosition = Vector2.zero;
        innerRt.sizeDelta = isBoss ? new Vector2(108f, 108f) : new Vector2(90f, 90f);

        Image portraitImg = innerGo.AddComponent<Image>();
        if (enemyPortrait != null)
        {
            portraitImg.sprite = enemyPortrait;
            portraitImg.preserveAspect = true;
            portraitImg.color = Color.white;
        }
        else
        {
            portraitImg.sprite = cachedCircleSprite;
            portraitImg.color = new Color(0.45f, 0.12f, 0.15f, 1f);
        }
        portraitImg.raycastTarget = false;

        // Nếu là Boss Ải 3: Huy hiệu đỏ ở trên đỉnh
        if (isBoss)
        {
            GameObject bossBadge = CreatePillPanel(
                nodeGo.transform,
                "BossTopBadge",
                new Vector2(0f, 86f),
                new Vector2(130f, 28f),
                new Color(0.68f, 0.12f, 0.14f, 0.96f),
                new Color(1f, 0.35f, 0.35f, 0.95f));

            CreateTMPChild(
                bossBadge.transform,
                "BossBadgeText",
                "BOSS ẢI 3",
                Vector2.zero,
                new Vector2(120f, 26f),
                13.5f,
                new Color(1f, 0.94f, 0.88f, 1f));
        }

        // Bảng tên Ải dưới chân Node
        float plateWidth = isBoss ? 260f : (stageNumber == 1 ? 230f : 210f);
        float plateY = isBoss ? -88f : -78f;
        GameObject titlePlate = CreatePillPanel(
            nodeGo.transform,
            "StageTitlePlate",
            new Vector2(0f, plateY),
            new Vector2(plateWidth, 34f),
            new Color(0.08f, 0.07f, 0.10f, 0.96f),
            new Color(0.65f, 0.50f, 0.25f, 0.85f));

        Component titleTmp = CreateTMPChild(
            titlePlate.transform,
            "StageNameText",
            stageTitle,
            Vector2.zero,
            new Vector2(plateWidth - 10f, 30f),
            13.5f,
            new Color(0.96f, 0.90f, 0.78f, 1f));

        stageNodeObjects.Add(nodeGo);
        stageNodeOutlines.Add(nodeOl);
        stageNodeStatusLabels.Add(titleTmp);
    }

    private void BuildHeroToken(Transform parent)
    {
        GameObject heroGo = new GameObject("HeroMapToken");
        heroGo.transform.SetParent(parent, false);
        heroTokenRect = heroGo.AddComponent<RectTransform>();
        heroTokenRect.anchoredPosition = GetHeroTokenPosition(heroMapStage);
        heroTokenRect.sizeDelta = new Vector2(96f, 96f);

        // Vòng hào quang vàng dưới chân Hiệp Sĩ
        GameObject haloGo = new GameObject("HeroHalo");
        haloGo.transform.SetParent(heroGo.transform, false);
        RectTransform haloRt = haloGo.AddComponent<RectTransform>();
        haloRt.anchoredPosition = Vector2.zero;
        haloRt.sizeDelta = new Vector2(108f, 108f);
        heroHaloImage = haloGo.AddComponent<Image>();
        heroHaloImage.sprite = cachedCircleSprite;
        heroHaloImage.color = new Color(1f, 0.85f, 0.25f, 0.35f);
        heroHaloImage.raycastTarget = false;

        // Khung tròn đại diện Hiệp Sĩ
        GameObject tokenBgGo = new GameObject("TokenCircle");
        tokenBgGo.transform.SetParent(heroGo.transform, false);
        RectTransform tokenBgRt = tokenBgGo.AddComponent<RectTransform>();
        tokenBgRt.anchoredPosition = Vector2.zero;
        tokenBgRt.sizeDelta = new Vector2(88f, 88f);

        Image tokenBgImg = tokenBgGo.AddComponent<Image>();
        tokenBgImg.sprite = cachedCircleSprite;
        tokenBgImg.color = new Color(0.10f, 0.18f, 0.32f, 0.98f);
        tokenBgImg.raycastTarget = false;

        Outline tokenOl = tokenBgGo.AddComponent<Outline>();
        tokenOl.effectColor = new Color(1f, 0.88f, 0.28f, 1f);
        tokenOl.effectDistance = new Vector2(2.5f, -2.5f);

        // Hình Hiệp Sĩ
        GameObject sprGo = new GameObject("HeroSprite");
        sprGo.transform.SetParent(tokenBgGo.transform, false);
        RectTransform sprRt = sprGo.AddComponent<RectTransform>();
        sprRt.anchoredPosition = new Vector2(0f, 2f);
        sprRt.sizeDelta = new Vector2(74f, 74f);

        Image sprImg = sprGo.AddComponent<Image>();
        if (heroTokenSprite != null)
        {
            sprImg.sprite = heroTokenSprite;
            sprImg.preserveAspect = true;
            sprImg.color = Color.white;
        }
        else
        {
            sprImg.sprite = cachedCircleSprite;
            sprImg.color = new Color(0.3f, 0.75f, 1f, 1f);
        }
        sprImg.raycastTarget = false;

        // Nhãn "HIỆP SĨ" trên đầu quân cờ
        GameObject tagGo = CreatePillPanel(
            heroGo.transform,
            "HeroTokenTag",
            new Vector2(0f, 56f),
            new Vector2(104f, 24f),
            new Color(0.08f, 0.07f, 0.10f, 0.96f),
            new Color(1f, 0.84f, 0.25f, 0.95f));

        CreateTMPChild(
            tagGo.transform,
            "HeroTagText",
            "HIỆP SĨ",
            Vector2.zero,
            new Vector2(96f, 22f),
            12f,
            new Color(1f, 0.92f, 0.55f, 1f));
    }

    private void BuildTopHeaderBar(Transform parent)
    {
        GameObject topBar = new GameObject("TopHeaderBar");
        topBar.transform.SetParent(parent, false);
        RectTransform rt = topBar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 54f);

        Image barImg = topBar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.05f, 0.08f, 0.96f);

        Outline barOl = topBar.AddComponent<Outline>();
        barOl.effectColor = new Color(0.45f, 0.38f, 0.28f, 0.75f);
        barOl.effectDistance = new Vector2(0f, -1.5f);

        // Điểm nhấn Gothic ở giữa đáy Header
        CreateTMPChild(
            topBar.transform,
            "HeaderGothicNotch",
            "◆",
            new Vector2(0f, -54f),
            new Vector2(24f, 20f),
            13f,
            new Color(0.65f, 0.52f, 0.30f, 0.9f));

        // Tiêu đề Bản Đồ bên trái
        CreateTMPChild(
            topBar.transform,
            "MapMainTitle",
            "BẢN ĐỒ HÀNH TRÌNH — VÙNG ĐẤT NGUYỀN RỦA",
            new Vector2(-607f, -27f),
            new Vector2(650f, 32f),
            18.5f,
            new Color(0.92f, 0.88f, 0.78f, 1f),
            align: 513); // MiddleLeft

        // --- CỤM ĐIỀU KHIỂN BÊN PHẢI ---
        // 1. Nút Menu ở góc ngoài cùng bên phải
        Button menuBtn = CreateStyledButton(
            topBar.transform,
            "MainMenuButton",
            "☰ MENU",
            new Vector2(874.5f, -27f),
            new Vector2(115f, 36f),
            new Color(0.10f, 0.09f, 0.14f, 0.96f),
            new Color(0.55f, 0.45f, 0.30f, 0.85f),
            new Color(0.92f, 0.88f, 0.80f, 1f),
            14f);
        menuBtn.onClick.AddListener(OnMainMenuClicked);

        // 2. Nút Bách Khoa
        Button codexBtn = CreateStyledButton(
            topBar.transform,
            "MapCodexButton",
            "📖 BÁCH KHOA",
            new Vector2(732.5f, -27f),
            new Vector2(145f, 36f),
            new Color(0.10f, 0.09f, 0.14f, 0.96f),
            new Color(0.55f, 0.45f, 0.30f, 0.85f),
            new Color(0.92f, 0.88f, 0.80f, 1f),
            14f);
        codexBtn.onClick.AddListener(() =>
        {
            if (rootCanvas != null) DiceCodexModalUI.ToggleOrCreate(rootCanvas.transform);
        });

        // 3. Nút Lượt Rèn
        Button forgeHeaderBtn = CreateStyledButton(
            topBar.transform,
            "HeaderForgeButton",
            "⚔ LƯỢT RÈN",
            new Vector2(580.5f, -27f),
            new Vector2(135f, 36f),
            new Color(0.24f, 0.16f, 0.06f, 0.98f),
            new Color(0.85f, 0.65f, 0.25f, 0.9f),
            new Color(1f, 0.90f, 0.55f, 1f),
            14f);
        forgeHeaderBtn.onClick.AddListener(OnOpenForgeClicked);
        forgeHeaderButtonLabel = forgeHeaderBtn.GetComponentInChildren(tmpType);

        // 4. Huy hiệu Máu Hiệp Sĩ (Xanh lá)
        GameObject hpBox = CreatePillPanel(
            topBar.transform,
            "HeroHPHeaderBox",
            new Vector2(392f, -27f),
            new Vector2(210f, 36f),
            new Color(0.08f, 0.24f, 0.14f, 0.96f),
            new Color(0.25f, 0.85f, 0.45f, 0.85f));

        hpStatusLabel = CreateTMPChild(
            hpBox.transform,
            "HPStatusText",
            "♥ HP HIỆP SĨ: 70/70",
            Vector2.zero,
            new Vector2(200f, 32f),
            14.5f,
            new Color(0.65f, 1f, 0.75f, 1f));
    }

    private void BuildBottomControlPanel(Transform parent)
    {
        GameObject bottomBar = new GameObject("BottomControlBar");
        bottomBar.transform.SetParent(parent, false);
        RectTransform rt = bottomBar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0f, 170f);

        Image barImg = bottomBar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.05f, 0.08f, 0.97f);

        Outline barOl = bottomBar.AddComponent<Outline>();
        barOl.effectColor = new Color(0.45f, 0.38f, 0.28f, 0.75f);
        barOl.effectDistance = new Vector2(0f, 1.5f);

        // Điểm nhấn Gothic ở giữa đỉnh Bottom Bar
        CreateTMPChild(
            bottomBar.transform,
            "BottomGothicNotch",
            "◆",
            new Vector2(0f, 170f),
            new Vector2(24f, 20f),
            13f,
            new Color(0.65f, 0.52f, 0.30f, 0.9f));

        // =====================================================================
        // CỘT TRÁI: HỘP THÔNG TIN ẢI (StageInfoCard)
        // =====================================================================
        GameObject infoCard = CreatePillPanel(
            bottomBar.transform,
            "StageInfoCard",
            new Vector2(-397f, 85f),
            new Vector2(1070f, 140f),
            new Color(0.08f, 0.07f, 0.11f, 0.96f),
            new Color(0.42f, 0.35f, 0.25f, 0.85f));

        // Line 1: Tên Ải & Tên Quái
        stageDetailTitleLabel = CreateTMPChild(
            infoCard.transform,
            "StageDetailTitle",
            "ẢI 1: CỬA RỪNG NGUYÊN RỦA - GOBLIN SCOUT",
            new Vector2(-10f, 38f),
            new Vector2(1000f, 30f),
            18.5f,
            new Color(0.96f, 0.92f, 0.84f, 1f),
            align: 513); // MiddleLeft

        // Line 2: Số ải: 3    Ải đã qua: 1
        stageProgressLabel = CreateTMPChild(
            infoCard.transform,
            "StageProgressText",
            "Số ải: 3    Ải đã qua: 1",
            new Vector2(-10f, 6f),
            new Vector2(1000f, 26f),
            15f,
            new Color(0.85f, 0.85f, 0.88f, 0.95f),
            align: 513);

        // Line 3: Đặc trưng: [⚔] [🛡] [🔨]
        CreateTMPChild(
            infoCard.transform,
            "TraitsLabel",
            "đặc trưng:",
            new Vector2(-460f, -32f),
            new Vector2(100f, 26f),
            14.5f,
            new Color(0.75f, 0.75f, 0.80f, 0.9f),
            align: 513);

        // 3 Icon Badges
        // Badge 1: Sword (Sát thương)
        GameObject b1 = CreatePillPanel(
            infoCard.transform,
            "TraitBadge_Attack",
            new Vector2(-385f, -32f),
            new Vector2(34f, 30f),
            new Color(0.32f, 0.08f, 0.10f, 0.98f),
            new Color(0.92f, 0.28f, 0.28f, 0.9f));
        CreateTMPChild(b1.transform, "Ico", "⚔", Vector2.zero, new Vector2(30f, 26f), 15f, new Color(1f, 0.75f, 0.75f, 1f));

        // Badge 2: Shield (Giáp ảo)
        GameObject b2 = CreatePillPanel(
            infoCard.transform,
            "TraitBadge_Shield",
            new Vector2(-342f, -32f),
            new Vector2(34f, 30f),
            new Color(0.08f, 0.18f, 0.35f, 0.98f),
            new Color(0.28f, 0.68f, 0.92f, 0.9f));
        CreateTMPChild(b2.transform, "Ico", "🛡", Vector2.zero, new Vector2(30f, 26f), 15f, new Color(0.75f, 0.90f, 1f, 1f));

        // Badge 3: Forge (Lò rèn)
        GameObject b3 = CreatePillPanel(
            infoCard.transform,
            "TraitBadge_Forge",
            new Vector2(-299f, -32f),
            new Vector2(34f, 30f),
            new Color(0.30f, 0.18f, 0.06f, 0.98f),
            new Color(0.95f, 0.72f, 0.22f, 0.9f));
        CreateTMPChild(b3.transform, "Ico", "🔨", Vector2.zero, new Vector2(30f, 26f), 15f, new Color(1f, 0.88f, 0.55f, 1f));

        // =====================================================================
        // CỘT PHẢI: CỤM NÚT HÀNH ĐỘNG
        // =====================================================================
        GameObject actionCluster = new GameObject("ActionButtonsCluster");
        actionCluster.transform.SetParent(bottomBar.transform, false);
        RectTransform acRt = actionCluster.AddComponent<RectTransform>();
        acRt.anchoredPosition = new Vector2(552f, 85f);
        acRt.sizeDelta = new Vector2(760f, 140f);

        // 1. Nút Rèn Xúc Xắc Ngay (Top Wide Button)
        openForgeButton = CreateStyledButton(
            actionCluster.transform,
            "OpenForgeButton",
            "RÈN XÚC XẮC NGAY (+1 Lượt rèn mới)",
            new Vector2(0f, 36f),
            new Vector2(760f, 54f),
            new Color(0.65f, 0.40f, 0.08f, 1f),
            new Color(1f, 0.85f, 0.30f, 1f),
            new Color(1f, 0.96f, 0.85f, 1f),
            16.5f);
        openForgeButton.onClick.AddListener(OnOpenForgeClicked);
        openForgeGlowOutlineImg = openForgeButton.GetComponent<Image>();

        // 2. Nút Di Chuyển Đến Ải (Bottom-Left Blue Button)
        moveNextStageButton = CreateStyledButton(
            actionCluster.transform,
            "MoveNextStageButton",
            $"DI CHUYỂN ĐẾN ẢI {currentTargetStage}",
            new Vector2(-195f, -32f),
            new Vector2(365f, 52f),
            new Color(0.12f, 0.32f, 0.55f, 1f),
            new Color(0.35f, 0.75f, 1f, 0.95f),
            Color.white,
            15.5f);
        moveNextStageButton.onClick.AddListener(OnMoveToNextStageClicked);

        // 3. Nút Tiến Tới & Đánh Ải (Bottom-Right Red Button)
        enterCombatButton = CreateStyledButton(
            actionCluster.transform,
            "EnterCombatButton",
            $"TIẾN TỚI & ĐÁNH ẢI {currentTargetStage}",
            new Vector2(195f, -32f),
            new Vector2(365f, 52f),
            new Color(0.62f, 0.14f, 0.12f, 1f),
            new Color(0.95f, 0.58f, 0.22f, 1f),
            Color.white,
            16f);
        enterCombatButton.onClick.AddListener(OnEnterCombatClicked);

        // Hiệu ứng ngọn lửa nhỏ trên nút đỏ
        CreateTMPChild(
            enterCombatButton.transform,
            "FlameSparkle",
            "🔥",
            new Vector2(0f, 22f),
            new Vector2(30f, 20f),
            12f,
            new Color(1f, 0.85f, 0.4f, 0.85f));
    }

    private void BuildScreenGothicFrame(Transform parent)
    {
        GameObject frameGo = new GameObject("ScreenGothicFrame");
        frameGo.transform.SetParent(parent, false);
        FullFill(frameGo.AddComponent<RectTransform>());

        Outline ol = frameGo.AddComponent<Outline>();
        ol.effectColor = new Color(0.35f, 0.28f, 0.20f, 0.75f);
        ol.effectDistance = new Vector2(2f, -2f);

        CreateTMPChild(frameGo.transform, "TopCrest", "⚜", new Vector2(0f, 532f), new Vector2(30f, 24f), 14f, new Color(0.65f, 0.52f, 0.32f, 0.9f));
        CreateTMPChild(frameGo.transform, "BottomCrest", "⚜", new Vector2(0f, -532f), new Vector2(30f, 24f), 14f, new Color(0.65f, 0.52f, 0.32f, 0.9f));
    }

    private void BuildRunVictoryBanner(Transform parent)
    {
        GameObject modalGo = new GameObject("VictoryMapModal");
        modalGo.transform.SetParent(parent, false);
        FullFill(modalGo.AddComponent<RectTransform>());

        Image backdrop = modalGo.AddComponent<Image>();
        backdrop.color = new Color(0.02f, 0.01f, 0.04f, 0.78f);

        GameObject box = CreatePillPanel(
            modalGo.transform,
            "VictoryDialogBox",
            Vector2.zero,
            new Vector2(720f, 360f),
            new Color(0.09f, 0.07f, 0.14f, 0.98f),
            new Color(1f, 0.82f, 0.25f, 1f));

        CreateTMPChild(
            box.transform,
            "WinTitle",
            "🏆 CHIẾN THẮNG HOÀN TOÀN!",
            new Vector2(0f, 110f),
            new Vector2(660f, 50f),
            34f,
            new Color(1f, 0.86f, 0.28f, 1f));

        CreateTMPChild(
            box.transform,
            "WinDesc",
            "Hiệp Sĩ đã đánh bại Minotaur Boss và chinh phục toàn bộ 3 Ải của Vùng Đất Nguyền Rủa!\r\nBạn có thể ghé Xưởng Rèn Huyền Thoại hoặc bắt đầu một Hành Trình Mới.",
            new Vector2(0f, 25f),
            new Vector2(640f, 80f),
            18f,
            new Color(0.92f, 0.94f, 0.98f, 1f));

        Button newRunBtn = CreateStyledButton(
            box.transform,
            "VictoryNewRunButton",
            "🔄 CHƠI LẠI TỪ ĐẦU (NEW RUN)",
            new Vector2(-170f, -95f),
            new Vector2(300f, 56f),
            new Color(0.62f, 0.14f, 0.14f, 1f),
            new Color(1f, 0.82f, 0.28f, 0.95f),
            Color.white,
            16f);
        newRunBtn.onClick.AddListener(() =>
        {
            if (GameManager.Instance != null) GameManager.Instance.StartNewRun();
            else SceneManager.LoadScene("CombatScene");
        });

        Button closeModalBtn = CreateStyledButton(
            box.transform,
            "VictoryInspectMapButton",
            "🗺 XEM BẢN ĐỒ & XƯỞNG RÈN",
            new Vector2(170f, -95f),
            new Vector2(300f, 56f),
            new Color(0.16f, 0.24f, 0.42f, 1f),
            new Color(0.45f, 0.82f, 1f, 0.9f),
            Color.white,
            16f);
        closeModalBtn.onClick.AddListener(() => modalGo.SetActive(false));
    }

    // =========================================================================
    // 3. CẬP NHẬT TRẠNG THÁI HIỂN THỊ CÁC NÚT ẢI, ĐƯỜNG ĐI & PANEL ĐIỀU KHIỂN
    // =========================================================================

    private void RefreshAllMapVisuals()
    {
        GameManager gm = GameManager.Instance;
        int curHP = gm != null ? gm.playerCurrentHP : 70;
        int maxHP = gm != null ? gm.playerMaxHP : 70;
        int forgeCount = gm != null ? gm.pendingForgeUpgrades : 1;
        int lastCleared = gm != null ? gm.lastClearedStageIndex : 1;
        bool isVictory = gm != null && gm.isRunVictory;

        // 1. Cập nhật Máu & Nút Lượt Rèn trên Top Header
        if (hpStatusLabel != null)
        {
            SetTMPText(hpStatusLabel, $"♥ HP HIỆP SĨ: {curHP}/{maxHP}");
        }

        if (forgeHeaderButtonLabel != null)
        {
            SetTMPText(forgeHeaderButtonLabel, forgeCount > 0 ? $"⚔ LƯỢT RÈN ({forgeCount})" : "⚔ LƯỢT RÈN");
        }

        // 2. Cập nhật màu sắc Tuyến Đường (Road Segments)
        bool stage1Cleared = lastCleared >= 1;
        bool stage2Cleared = lastCleared >= 2;

        foreach (Image seg in pathSegmentImages1To2)
        {
            if (seg == null) continue;
            if (heroMapStage >= 2 || stage2Cleared)
                seg.color = new Color(0.25f, 0.95f, 0.58f, 0.92f); // Đã đi qua (Xanh ngọc)
            else if (stage1Cleared)
                seg.color = new Color(0.98f, 0.78f, 0.22f, 0.92f); // Đường mở khóa tới Ải 2 (Vàng rực)
            else
                seg.color = new Color(0.45f, 0.38f, 0.58f, 0.45f);
        }

        foreach (Image seg in pathSegmentImages2To3)
        {
            if (seg == null) continue;
            if (heroMapStage >= 3 || isVictory)
                seg.color = new Color(0.25f, 0.95f, 0.58f, 0.92f);
            else if (stage2Cleared || highestUnlockedStage >= 3)
                seg.color = new Color(0.98f, 0.42f, 0.35f, 0.95f); // Đường mở khóa tới Boss (Đỏ cam)
            else
                seg.color = new Color(0.35f, 0.30f, 0.45f, 0.38f);
        }

        // 3. Cập nhật viền các Nút Ải
        for (int i = 0; i < stageNodeObjects.Count; i++)
        {
            int stageNum = i + 1;
            Outline ol = stageNodeOutlines[i];
            if (ol != null)
            {
                if (stageNum == 1)
                    ol.effectColor = new Color(0.25f, 0.95f, 0.55f, 1f); // Xanh lục ngọc
                else if (stageNum == 2)
                    ol.effectColor = new Color(1f, 0.82f, 0.25f, 1f);   // Vàng hổ phách
                else
                    ol.effectColor = new Color(0.95f, 0.22f, 0.22f, 1f); // Đỏ huyết
            }
        }

        // 4. Cập nhật Thẻ Thông Tin Ải ở Bottom Panel
        UpdateStageDetailCard(selectedInspectStage, forgeCount);

        // 5. Cập nhật trạng thái 3 Nút Hành Động chính
        if (openForgeButton != null)
        {
            openForgeButton.interactable = !isHeroMoving && !isTransitioningScene;
            string forgeBtnText = forgeCount > 0
                ? $"RÈN XÚC XẮC NGAY (+{forgeCount} Lượt rèn mới)"
                : "RÈN XÚC XẮC (VÀO LÒ RÈN)";
            UpdateButtonText(openForgeButton, forgeBtnText);
        }

        bool needsMove = heroMapStage < currentTargetStage;
        if (moveNextStageButton != null)
        {
            moveNextStageButton.interactable = !isHeroMoving && !isTransitioningScene && needsMove;
            UpdateButtonText(moveNextStageButton, needsMove
                ? $"DI CHUYỂN ĐẾN ẢI {currentTargetStage}"
                : $"ĐÃ ĐẾN ẢI {currentTargetStage}");
        }

        if (enterCombatButton != null)
        {
            enterCombatButton.interactable = !isHeroMoving && !isTransitioningScene;
            UpdateButtonText(enterCombatButton, $"TIẾN TỚI & ĐÁNH ẢI {currentTargetStage}");
        }
    }

    private void UpdateStageDetailCard(int stageNum, int forgeCount)
    {
        string title;
        switch (stageNum)
        {
            case 1:
                title = "ẢI 1: CỬA RỪNG NGUYÊN RỦA - GOBLIN SCOUT";
                break;
            case 2:
                title = "ẢI 2: THÁP HẮC ÁM - ARMORED ORC";
                break;
            default:
                title = "BOSS ẢI 3: PHÁO ĐÀI BOSS - MINOTAUR BOSS";
                break;
        }

        SetTMPText(stageDetailTitleLabel, title);

        GameManager gm = GameManager.Instance;
        int cleared = gm != null ? gm.lastClearedStageIndex : 1;
        SetTMPText(stageProgressLabel, $"Số ải: 3    Ải đã qua: {cleared}");
    }

    // =========================================================================
    // 4. XỬ LÝ DI CHUYỂN HIỆP SĨ TRÊN MAP & CHUYỂN SCENE
    // =========================================================================

    public void OnStageNodeClicked(int clickedStage)
    {
        if (isHeroMoving || isTransitioningScene) return;

        selectedInspectStage = clickedStage;

        // Nếu click vào ải chưa mở khóa
        if (clickedStage > highestUnlockedStage)
        {
            RefreshAllMapVisuals();
            return;
        }

        // Nếu Hiệp Sĩ đang ở ải khác với ải vừa click -> Di chuyển Hiệp Sĩ tới ải đó!
        if (heroMapStage != clickedStage)
        {
            StartCoroutine(MoveHeroAlongPathCoroutine(heroMapStage, clickedStage, autoEnterCombatAfterMove: false));
            return;
        }

        // Nếu Hiệp Sĩ ĐÃ đứng ngay tại ải mục tiêu và người chơi click lần nữa -> Vào chiến đấu!
        if (clickedStage == currentTargetStage)
        {
            OnEnterCombatClicked();
        }
        else
        {
            RefreshAllMapVisuals();
        }
    }

    public void OnMoveToNextStageClicked()
    {
        if (isHeroMoving || isTransitioningScene) return;

        selectedInspectStage = currentTargetStage;
        if (heroMapStage != currentTargetStage)
        {
            StartCoroutine(MoveHeroAlongPathCoroutine(heroMapStage, currentTargetStage, autoEnterCombatAfterMove: false));
        }
        else
        {
            RefreshAllMapVisuals();
        }
    }

    public void OnEnterCombatClicked()
    {
        if (isHeroMoving || isTransitioningScene) return;

        selectedInspectStage = currentTargetStage;

        // Nếu Hiệp Sĩ chưa di chuyển đến ải tiếp theo thì tự động di chuyển trên Map rồi vào trận
        if (heroMapStage != currentTargetStage)
        {
            StartCoroutine(MoveHeroAlongPathCoroutine(heroMapStage, currentTargetStage, autoEnterCombatAfterMove: true));
        }
        else
        {
            StartCoroutine(LaunchCombatAfterBriefEffect());
        }
    }

    public void OnOpenForgeClicked()
    {
        if (isHeroMoving || isTransitioningScene) return;
        isTransitioningScene = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.heroMapStageIndex = heroMapStage;
            GameManager.Instance.LoadForgeScene();
        }
        else
        {
            SceneManager.LoadScene("TheForge");
        }
    }

    public void OnMainMenuClicked()
    {
        if (isHeroMoving || isTransitioningScene) return;
        isTransitioningScene = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private IEnumerator MoveHeroAlongPathCoroutine(int fromStage, int toStage, bool autoEnterCombatAfterMove)
    {
        isHeroMoving = true;
        RefreshAllMapVisuals();

        List<Vector2> waypoints = GetWaypointsBetweenStages(fromStage, toStage);
        Vector2 startOffset = GetHeroTokenPosition(fromStage) - GetStageNodePosition(fromStage);
        Vector2 endOffset = GetHeroTokenPosition(toStage) - GetStageNodePosition(toStage);

        for (int i = 1; i < waypoints.Count; i++)
        {
            Vector2 startPos = heroTokenRect != null ? heroTokenRect.anchoredPosition : (waypoints[i - 1] + startOffset);
            Vector2 currentOffset = Vector2.Lerp(startOffset, endOffset, i / (float)waypoints.Count);
            Vector2 endPos = waypoints[i] + currentOffset;

            float duration = 0.22f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);

                float hopY = Mathf.Sin(t * Mathf.PI) * 16f;
                if (heroTokenRect != null)
                {
                    heroTokenRect.anchoredPosition = Vector2.Lerp(startPos, endPos, smoothT) + new Vector2(0f, hopY);
                }
                yield return null;
            }

            if (heroTokenRect != null)
            {
                heroTokenRect.anchoredPosition = endPos;
            }
        }

        heroMapStage = toStage;
        if (heroTokenRect != null)
        {
            heroTokenRect.anchoredPosition = GetHeroTokenPosition(toStage);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.heroMapStageIndex = heroMapStage;
        }

        isHeroMoving = false;
        RefreshAllMapVisuals();

        if (autoEnterCombatAfterMove)
        {
            yield return new WaitForSeconds(0.25f);
            yield return StartCoroutine(LaunchCombatAfterBriefEffect());
        }
    }

    private IEnumerator LaunchCombatAfterBriefEffect()
    {
        isTransitioningScene = true;
        RefreshAllMapVisuals();

        if (heroTokenRect != null)
        {
            float d = 0.28f;
            float e = 0f;
            while (e < d)
            {
                e += Time.deltaTime;
                float s = 1f + Mathf.Sin((e / d) * Mathf.PI) * 0.25f;
                heroTokenRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            heroTokenRect.localScale = Vector3.one;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentStageIndex = currentTargetStage;
            GameManager.Instance.heroMapStageIndex = heroMapStage;
            GameManager.Instance.LoadCombatScene();
        }
        else
        {
            SceneManager.LoadScene("CombatScene");
        }
    }

    private List<Vector2> GetWaypointsBetweenStages(int fromStage, int toStage)
    {
        List<Vector2> result = new List<Vector2>();
        if (fromStage == toStage)
        {
            result.Add(GetStageNodePosition(toStage));
            return result;
        }

        if (fromStage == 1 && toStage == 2)
        {
            result.AddRange(Path1To2Waypoints);
        }
        else if (fromStage == 2 && toStage == 3)
        {
            result.AddRange(Path2To3Waypoints);
        }
        else if (fromStage == 1 && toStage == 3)
        {
            result.AddRange(Path1To2Waypoints);
            for (int i = 1; i < Path2To3Waypoints.Length; i++)
                result.Add(Path2To3Waypoints[i]);
        }
        else if (fromStage == 2 && toStage == 1)
        {
            for (int i = Path1To2Waypoints.Length - 1; i >= 0; i--)
                result.Add(Path1To2Waypoints[i]);
        }
        else if (fromStage == 3 && toStage == 2)
        {
            for (int i = Path2To3Waypoints.Length - 1; i >= 0; i--)
                result.Add(Path2To3Waypoints[i]);
        }
        else
        {
            result.Add(GetStageNodePosition(fromStage));
            result.Add(GetStageNodePosition(toStage));
        }

        return result;
    }

    private Vector2 GetStageNodePosition(int stage)
    {
        switch (stage)
        {
            case 1: return Stage1Pos;
            case 2: return Stage2Pos;
            case 3: return Stage3Pos;
            default: return Stage1Pos;
        }
    }

    private Vector2 GetHeroTokenPosition(int stage)
    {
        switch (stage)
        {
            case 1: return Stage1Pos + new Vector2(-70f, 65f);
            case 2: return Stage2Pos + new Vector2(-65f, 65f);
            case 3: return Stage3Pos + new Vector2(-70f, 65f);
            default: return Stage1Pos + new Vector2(-70f, 65f);
        }
    }

    // =========================================================================
    // 5. HIỆU ỨNG ĐỘNG TRÊN BẢN ĐỒ & PHÍM TẮT ĐIỀU KHIỂN
    // =========================================================================

    private void AnimateHeroTokenAndTargetNodes()
    {
        float time = Time.time;

        if (heroHaloImage != null)
        {
            float pulse = 0.28f + Mathf.Sin(time * 3.5f) * 0.14f;
            heroHaloImage.color = new Color(1f, 0.84f, 0.25f, pulse);
            float scale = 1f + Mathf.Sin(time * 3.5f) * 0.08f;
            heroHaloImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        int targetIdx = currentTargetStage - 1;
        if (targetIdx >= 0 && targetIdx < stageNodeObjects.Count && stageNodeObjects[targetIdx] != null)
        {
            float nodeScale = 1f + Mathf.Sin(time * 3.2f) * 0.045f;
            stageNodeObjects[targetIdx].transform.localScale = new Vector3(nodeScale, nodeScale, 1f);
        }

        if (openForgeGlowOutlineImg != null && GameManager.Instance != null && GameManager.Instance.pendingForgeUpgrades > 0)
        {
            float glow = 0.55f + Mathf.Sin(time * 4f) * 0.15f;
            openForgeGlowOutlineImg.color = new Color(glow, 0.32f, 0.06f, 1f);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (isHeroMoving || isTransitioningScene) return;

        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (heroMapStage < currentTargetStage)
            {
                OnMoveToNextStageClicked();
            }
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            OnOpenForgeClicked();
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (heroMapStage < currentTargetStage)
            {
                OnMoveToNextStageClicked();
            }
            else
            {
                OnEnterCombatClicked();
            }
        }
    }

    // =========================================================================
    // 6. UI HELPERS & KHỞI TẠO HỆ THỐNG CƠ BẢN
    // =========================================================================

    private GameObject CreatePillPanel(
        Transform parent,
        string name,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        Color outlineColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = false;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }

    private Button CreateStyledButton(
        Transform parent,
        string name,
        string caption,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        Color outlineColor,
        Color textColor,
        float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.AddComponent<Image>();
        img.color = bgColor;

        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(2f, -2f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        CreateTMPChild(
            go.transform,
            "Label",
            caption,
            Vector2.zero,
            size - new Vector2(12f, 6f),
            fontSize,
            textColor);

        return btn;
    }

    private Component CreateTMPChild(
        Transform parent,
        string name,
        string text,
        Vector2 anchoredPos,
        Vector2 size,
        float fontSize,
        Color color,
        int align = 514)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        if (tmpType == null) return null;
        Component tmp = go.AddComponent(tmpType);
        SetTMPText(tmp, text);
        tmpType.GetProperty("fontSize")?.SetValue(tmp, fontSize);
        tmpType.GetProperty("color")?.SetValue(tmp, color);
        tmpType.GetProperty("alignment")?.SetValue(tmp, align);
        tmpType.GetProperty("raycastTarget")?.SetValue(tmp, false);
        return tmp;
    }

    private void UpdateButtonText(Button btn, string text)
    {
        if (btn == null || tmpType == null) return;
        Component tmp = btn.GetComponentInChildren(tmpType);
        SetTMPText(tmp, text);
    }

    private void SetTMPText(Component tmpComp, string value)
    {
        if (tmpComp == null || tmpType == null) return;
        tmpType.GetProperty("text")?.SetValue(tmpComp, value);
    }

    private static void FullFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    private static void EnsureCircleSprite()
    {
        if (cachedCircleSprite != null) return;
        int sz = 64;
        Texture2D tex = new Texture2D(sz, sz, TextureFormat.RGBA32, false);
        for (int y = 0; y < sz; y++)
        {
            for (int x = 0; x < sz; x++)
            {
                float nx = (x / (float)(sz - 1)) * 2f - 1f;
                float ny = (y / (float)(sz - 1)) * 2f - 1f;
                float dist = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01((1f - dist) * 12f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f));
    }

    private static void EnsureCameraAndEventSystem()
    {
        if (FindAnyObjectByType<Camera>() == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f, 1f);
            cam.orthographic = true;
            cam.depth = -1;
            camGo.AddComponent<AudioListener>();
        }

        EventSystem es = FindAnyObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esGo = new GameObject("EventSystem");
            es = esGo.AddComponent<EventSystem>();
            System.Type inputSysModule = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputSysModule != null)
            {
                Component mod = esGo.AddComponent(inputSysModule);
                inputSysModule.GetMethod("AssignDefaultActions")?.Invoke(mod, null);
            }
            else
            {
                esGo.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
