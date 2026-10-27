using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// StageMapController — Điều phối màn hình Bản Đồ Ải (StageMap):
/// - Hiển thị hành trình qua 3 Ải (Ải 1: Goblin, Ải 2: Orc, Ải 3: Minotaur Boss) trên nền World Map Dark Fantasy.
/// - Cho phép người chơi di chuyển Hiệp Sĩ từ ải vừa thắng sang ải tiếp theo trên bản đồ (bằng cách click vào nút ải
///   trên bản đồ, bấm nút "DI CHUYỂN ĐẾN ẢI TIẾP THEO", hoặc phím mũi tên).
/// - Cho phép truy cập trực tiếp vào màn hình Rèn Xúc Xắc (TheForge) để nâng cấp mặt xúc xắc chuẩn bị cho trận kế tiếp.
/// - Khi đã sẵn sàng ở ải tiếp theo, người chơi có thể bấm "VÀO CHIẾN ĐẤU" để bắt đầu trận đánh.
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

    // Tọa độ các nút Ải trên Canvas 1920x1080 (khớp với đường đi trên World_Map_Overworld.png)
    private static readonly Vector2 Stage1Pos = new Vector2(-640f, 90f);
    private static readonly Vector2 Stage2Pos = new Vector2(-10f, -55f);
    private static readonly Vector2 Stage3Pos = new Vector2(630f, -20f);
    private static readonly Vector2 ForgeSanctuaryPos = new Vector2(-470f, 235f);

    // Các điểm mốc (Waypoints) nối giữa Ải 1 -> Ải 2 và Ải 2 -> Ải 3
    private static readonly Vector2[] Path1To2Waypoints = new Vector2[]
    {
        new Vector2(-640f, 90f),
        new Vector2(-490f, 165f),
        new Vector2(-345f, 105f),
        new Vector2(-175f, 12f),
        new Vector2(-10f, -55f)
    };

    private static readonly Vector2[] Path2To3Waypoints = new Vector2[]
    {
        new Vector2(-10f, -55f),
        new Vector2(160f, 10f),
        new Vector2(330f, -85f),
        new Vector2(485f, -170f),
        new Vector2(630f, -20f)
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
    private Component headerSubLabel;
    private Component hpStatusLabel;
    private Image hpFillImage;
    private Component forgeBadgeLabel;
    private Component stageDetailTitleLabel;
    private Component stageDetailDescLabel;
    private Component heroLocationStatusLabel;

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
    // 2. XÂY DỰNG GIAO DIỆN BẢN ĐỒ ẢI (4 PHÂN KHU: HEADER, MAP NODES, HERO, BOTTOM BAR)
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

        // 1. Hình nền Bản Đồ Thế Giới (World_Map_Overworld)
        BuildMapBackground(mapRoot.transform);

        // 2. Tuyến đường nối các ải & Trạm Rèn Xúc Xắc trên bản đồ
        GameObject roadsLayer = new GameObject("RoadsLayer");
        roadsLayer.transform.SetParent(mapRoot.transform, false);
        FullFill(roadsLayer.AddComponent<RectTransform>());

        BuildRoadSegments(roadsLayer.transform, Path1To2Waypoints, pathSegmentImages1To2);
        BuildRoadSegments(roadsLayer.transform, Path2To3Waypoints, pathSegmentImages2To3);
        BuildForgeBranchRoad(roadsLayer.transform, Stage1Pos, ForgeSanctuaryPos);

        // 3. Các nút Ải (Ải 1, Ải 2, Ải 3) + Nút Lò Rèn Xúc Xắc trên Map
        GameObject nodesLayer = new GameObject("NodesLayer");
        nodesLayer.transform.SetParent(mapRoot.transform, false);
        FullFill(nodesLayer.AddComponent<RectTransform>());

        BuildForgeSanctuaryMapNode(nodesLayer.transform);
        BuildStageNode(nodesLayer.transform, 1, Stage1Pos, "ẢI 1: RỪNG NGUYỀN RỦA", "Goblin Scout", goblinSprite, false);
        BuildStageNode(nodesLayer.transform, 2, Stage2Pos, "ẢI 2: THÁP HẮC ÁM", "Armored Orc", orcSprite, false);
        BuildStageNode(nodesLayer.transform, 3, Stage3Pos, "ẢI 3: PHÁO ĐÀI BOSS", "Minotaur Boss", minotaurSprite, true);

        // 4. Quân cờ Hiệp Sĩ (Hero Avatar Token) đứng trên Bản Đồ
        BuildHeroToken(mapRoot.transform);

        // 5. Thanh Header Đỉnh (Thông tin Hành Trình, Máu Hiệp Sĩ, Nút về Menu)
        BuildTopHeaderBar(mapRoot.transform);

        // 6. Bảng Điều Khiển Đáy (Thông tin Ải + Túi Xúc Xắc + Nút Rèn Xúc Xắc + Nút Di Chuyển & Chiến Đấu)
        BuildBottomControlPanel(mapRoot.transform);

        // 7. Nếu đã chiến thắng toàn bộ 3 ải thì hiển thị hộp thoại Vinh Danh Chiến Thắng
        if (GameManager.Instance != null && GameManager.Instance.isRunVictory)
        {
            BuildRunVictoryBanner(mapRoot.transform);
        }
    }

    private void BuildMapBackground(Transform parent)
    {
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
        vigImg.color = new Color(0.03f, 0.02f, 0.06f, 0.28f);
        vigImg.raycastTarget = false;
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

            // Vẽ các chấm đá cổ ngữ (Runic Stepping Stones) dọc theo đoạn đường
            int steps = Mathf.Max(3, Mathf.RoundToInt(dist / 38f));
            for (int s = 1; s < steps; s++)
            {
                float t = s / (float)steps;
                Vector2 dotPos = Vector2.Lerp(a, b, t);

                GameObject dotGo = new GameObject($"RoadStep_{i}_{s}");
                dotGo.transform.SetParent(parent, false);
                RectTransform dotRt = dotGo.AddComponent<RectTransform>();
                dotRt.anchoredPosition = dotPos;
                dotRt.sizeDelta = new Vector2(18f, 18f);
                dotRt.localRotation = Quaternion.Euler(0f, 0f, angle);

                Image dotImg = dotGo.AddComponent<Image>();
                dotImg.sprite = cachedCircleSprite;
                dotImg.color = new Color(0.68f, 0.35f, 0.98f, 0.85f);
                dotImg.raycastTarget = false;

                Outline ol = dotGo.AddComponent<Outline>();
                ol.effectColor = new Color(0.05f, 0.02f, 0.1f, 0.9f);
                ol.effectDistance = new Vector2(1.5f, -1.5f);

                segmentList.Add(dotImg);
            }
        }
    }

    private void BuildForgeBranchRoad(Transform parent, Vector2 fromPos, Vector2 toPos)
    {
        int steps = 5;
        for (int s = 1; s < steps; s++)
        {
            float t = s / (float)steps;
            Vector2 pos = Vector2.Lerp(fromPos, toPos, t);

            GameObject dotGo = new GameObject($"ForgeRoadDot_{s}");
            dotGo.transform.SetParent(parent, false);
            RectTransform rt = dotGo.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(14f, 14f);

            Image img = dotGo.AddComponent<Image>();
            img.sprite = cachedCircleSprite;
            img.color = new Color(0.98f, 0.68f, 0.2f, 0.8f);
            img.raycastTarget = false;
        }
    }

    private void BuildForgeSanctuaryMapNode(Transform parent)
    {
        GameObject nodeGo = new GameObject("MapNode_TheForge");
        nodeGo.transform.SetParent(parent, false);
        RectTransform rt = nodeGo.AddComponent<RectTransform>();
        rt.anchoredPosition = ForgeSanctuaryPos;
        rt.sizeDelta = new Vector2(118f, 118f);

        Image bgImg = nodeGo.AddComponent<Image>();
        bgImg.sprite = cachedCircleSprite;
        bgImg.color = new Color(0.32f, 0.16f, 0.04f, 0.96f);

        Outline ol = nodeGo.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 0.75f, 0.22f, 0.95f);
        ol.effectDistance = new Vector2(3f, -3f);

        Button btn = nodeGo.AddComponent<Button>();
        btn.targetGraphic = bgImg;
        btn.onClick.AddListener(OnOpenForgeClicked);

        if (forgeIconSprite != null)
        {
            GameObject iconGo = new GameObject("ForgeIcon");
            iconGo.transform.SetParent(nodeGo.transform, false);
            RectTransform iconRt = iconGo.AddComponent<RectTransform>();
            iconRt.anchoredPosition = new Vector2(0f, 8f);
            iconRt.sizeDelta = new Vector2(56f, 56f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = forgeIconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
        }
        else
        {
            CreateTMPChild(nodeGo.transform, "ForgeEmoji", "🔥", new Vector2(0f, 6f), new Vector2(80f, 54f), 34f, new Color(1f, 0.82f, 0.3f, 1f));
        }

        // Bảng tên dưới chân Lò Rèn
        GameObject plateGo = CreatePillPanel(
            nodeGo.transform,
            "ForgePlate",
            new Vector2(0f, -68f),
            new Vector2(210f, 34f),
            new Color(0.12f, 0.07f, 0.02f, 0.95f),
            new Color(1f, 0.75f, 0.22f, 0.9f));

        CreateTMPChild(
            plateGo.transform,
            "ForgePlateText",
            "🔨 XƯỞNG RÈN XÚC XẮC",
            Vector2.zero,
            new Vector2(200f, 30f),
            13.5f,
            new Color(1f, 0.88f, 0.45f, 1f));
    }

    private void BuildStageNode(
        Transform parent,
        int stageNumber,
        Vector2 pos,
        string stageTitle,
        string enemyName,
        Sprite enemyPortrait,
        bool isBoss)
    {
        GameObject nodeGo = new GameObject($"StageNode_{stageNumber}");
        nodeGo.transform.SetParent(parent, false);
        RectTransform rt = nodeGo.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        Vector2 nodeSize = isBoss ? new Vector2(158f, 158f) : new Vector2(138f, 138f);
        rt.sizeDelta = nodeSize;

        Image outerRing = nodeGo.AddComponent<Image>();
        outerRing.sprite = cachedCircleSprite;
        outerRing.color = new Color(0.12f, 0.10f, 0.18f, 0.96f);

        Outline nodeOl = nodeGo.AddComponent<Outline>();
        nodeOl.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.9f);
        nodeOl.effectDistance = new Vector2(3.5f, -3.5f);

        int capturedStage = stageNumber;
        Button btn = nodeGo.AddComponent<Button>();
        btn.targetGraphic = outerRing;
        btn.onClick.AddListener(() => OnStageNodeClicked(capturedStage));

        // Khung chân dung Quái Vật bên trong Node
        GameObject innerGo = new GameObject("PortraitFrame");
        innerGo.transform.SetParent(nodeGo.transform, false);
        RectTransform innerRt = innerGo.AddComponent<RectTransform>();
        innerRt.anchoredPosition = new Vector2(0f, 6f);
        innerRt.sizeDelta = isBoss ? new Vector2(112f, 112f) : new Vector2(96f, 96f);

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

        // Huy hiệu số Ải ở góc trên Node
        GameObject numBadge = CreatePillPanel(
            nodeGo.transform,
            "StageNumberBadge",
            new Vector2(0f, isBoss ? 76f : 66f),
            new Vector2(isBoss ? 140f : 110f, 28f),
            isBoss ? new Color(0.55f, 0.08f, 0.14f, 0.98f) : new Color(0.14f, 0.11f, 0.22f, 0.98f),
            isBoss ? new Color(1f, 0.35f, 0.35f, 0.95f) : new Color(0.92f, 0.75f, 0.28f, 0.9f));

        CreateTMPChild(
            numBadge.transform,
            "BadgeText",
            isBoss ? $"👑 BOSS ẢI {stageNumber}" : $"⚔ ẢI {stageNumber}",
            Vector2.zero,
            new Vector2(130f, 26f),
            13.5f,
            new Color(1f, 0.94f, 0.78f, 1f));

        // Bảng tên Ải & Quái vật phía dưới Node
        GameObject titlePlate = CreatePillPanel(
            nodeGo.transform,
            "StageTitlePlate",
            new Vector2(0f, isBoss ? -92f : -82f),
            new Vector2(246f, 54f),
            new Color(0.06f, 0.06f, 0.11f, 0.95f),
            new Color(0.75f, 0.58f, 0.22f, 0.85f));

        CreateTMPChild(
            titlePlate.transform,
            "StageNameText",
            $"<b>{stageTitle}</b>\n<size=12><color=#cbd5e1>{enemyName}</color></size>",
            Vector2.zero,
            new Vector2(236f, 50f),
            13.5f,
            new Color(0.98f, 0.88f, 0.58f, 1f));

        // Nhãn trạng thái Ải (ĐÃ VƯỢT QUA / CLICK ĐỂ DI CHUYỂN / KHÓA)
        GameObject statusPill = CreatePillPanel(
            nodeGo.transform,
            "StageStatusPill",
            new Vector2(0f, isBoss ? -134f : -124f),
            new Vector2(226f, 28f),
            new Color(0.08f, 0.12f, 0.18f, 0.95f),
            new Color(0.35f, 0.75f, 0.95f, 0.8f));

        Component statusTmp = CreateTMPChild(
            statusPill.transform,
            "StatusText",
            "SẴN SÀNG",
            Vector2.zero,
            new Vector2(218f, 26f),
            12.5f,
            Color.white);

        stageNodeObjects.Add(nodeGo);
        stageNodeOutlines.Add(nodeOl);
        stageNodeStatusLabels.Add(statusTmp);
    }

    private void BuildHeroToken(Transform parent)
    {
        GameObject heroGo = new GameObject("HeroMapToken");
        heroGo.transform.SetParent(parent, false);
        heroTokenRect = heroGo.AddComponent<RectTransform>();
        heroTokenRect.anchoredPosition = GetStageNodePosition(heroMapStage) + new Vector2(-58f, 46f);
        heroTokenRect.sizeDelta = new Vector2(104f, 104f);

        // Vòng hào quang vàng dưới chân Hiệp Sĩ
        GameObject haloGo = new GameObject("HeroHalo");
        haloGo.transform.SetParent(heroGo.transform, false);
        RectTransform haloRt = haloGo.AddComponent<RectTransform>();
        haloRt.anchoredPosition = Vector2.zero;
        haloRt.sizeDelta = new Vector2(118f, 118f);
        heroHaloImage = haloGo.AddComponent<Image>();
        heroHaloImage.sprite = cachedCircleSprite;
        heroHaloImage.color = new Color(1f, 0.82f, 0.22f, 0.35f);
        heroHaloImage.raycastTarget = false;

        // Khung tròn đại diện Hiệp Sĩ
        GameObject tokenBgGo = new GameObject("TokenCircle");
        tokenBgGo.transform.SetParent(heroGo.transform, false);
        RectTransform tokenBgRt = tokenBgGo.AddComponent<RectTransform>();
        tokenBgRt.anchoredPosition = Vector2.zero;
        tokenBgRt.sizeDelta = new Vector2(92f, 92f);

        Image tokenBgImg = tokenBgGo.AddComponent<Image>();
        tokenBgImg.sprite = cachedCircleSprite;
        tokenBgImg.color = new Color(0.12f, 0.24f, 0.42f, 0.98f);
        tokenBgImg.raycastTarget = false;

        Outline tokenOl = tokenBgGo.AddComponent<Outline>();
        tokenOl.effectColor = new Color(1f, 0.86f, 0.28f, 1f);
        tokenOl.effectDistance = new Vector2(3f, -3f);

        // Hình Hiệp Sĩ
        GameObject sprGo = new GameObject("HeroSprite");
        sprGo.transform.SetParent(tokenBgGo.transform, false);
        RectTransform sprRt = sprGo.AddComponent<RectTransform>();
        sprRt.anchoredPosition = new Vector2(0f, 4f);
        sprRt.sizeDelta = new Vector2(78f, 78f);

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
            new Vector2(0f, 60f),
            new Vector2(118f, 26f),
            new Color(0.08f, 0.16f, 0.28f, 0.96f),
            new Color(1f, 0.84f, 0.25f, 0.95f));

        CreateTMPChild(
            tagGo.transform,
            "HeroTagText",
            "⚔ HIỆP SĨ",
            Vector2.zero,
            new Vector2(110f, 24f),
            12.5f,
            new Color(1f, 0.92f, 0.45f, 1f));
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
        rt.sizeDelta = new Vector2(0f, 84f);

        Image barImg = topBar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.05f, 0.09f, 0.94f);

        Outline barOl = topBar.AddComponent<Outline>();
        barOl.effectColor = new Color(0.82f, 0.64f, 0.22f, 0.65f);
        barOl.effectDistance = new Vector2(0f, -1.5f);

        // Tiêu đề Bản Đồ bên trái
        CreateTMPChild(
            topBar.transform,
            "MapMainTitle",
            "🗺 BẢN ĐỒ HÀNH TRÌNH — VÙNG ĐẤT NGUYỀN RỦA",
            new Vector2(-520f, -24f),
            new Vector2(780f, 34f),
            22f,
            new Color(0.98f, 0.84f, 0.38f, 1f),
            align: 513); // MiddleLeft

        headerSubLabel = CreateTMPChild(
            topBar.transform,
            "MapSubTitle",
            "Chọn ải tiếp theo trên bản đồ để di chuyển hoặc ghé Xưởng Rèn Xúc Xắc để chuẩn bị chiến đấu",
            new Vector2(-520f, -56f),
            new Vector2(820f, 26f),
            14f,
            new Color(0.78f, 0.84f, 0.92f, 0.95f),
            align: 513);

        // Cụm Thanh Máu Hiệp Sĩ ở giữa-phải
        GameObject hpBox = CreatePillPanel(
            topBar.transform,
            "HeroHPHeaderBox",
            new Vector2(220f, -42f),
            new Vector2(310f, 46f),
            new Color(0.09f, 0.10f, 0.16f, 0.96f),
            new Color(0.25f, 0.82f, 0.45f, 0.8f));

        GameObject hpFillGo = new GameObject("HPFill");
        hpFillGo.transform.SetParent(hpBox.transform, false);
        RectTransform hpFillRt = hpFillGo.AddComponent<RectTransform>();
        hpFillRt.anchorMin = new Vector2(0f, 0f);
        hpFillRt.anchorMax = new Vector2(1f, 1f);
        hpFillRt.offsetMin = new Vector2(4f, 4f);
        hpFillRt.offsetMax = new Vector2(-4f, -4f);
        hpFillImage = hpFillGo.AddComponent<Image>();
        hpFillImage.color = new Color(0.18f, 0.68f, 0.34f, 0.85f);
        hpFillImage.raycastTarget = false;

        hpStatusLabel = CreateTMPChild(
            hpBox.transform,
            "HPStatusText",
            "♥ HP HIỆP SĨ: 70 / 70",
            Vector2.zero,
            new Vector2(296f, 40f),
            16f,
            Color.white);

        // Huy hiệu Lượt Rèn khả dụng
        GameObject forgePill = CreatePillPanel(
            topBar.transform,
            "ForgeCountPill",
            new Vector2(545f, -42f),
            new Vector2(280f, 46f),
            new Color(0.22f, 0.14f, 0.04f, 0.96f),
            new Color(0.98f, 0.74f, 0.22f, 0.9f));

        forgeBadgeLabel = CreateTMPChild(
            forgePill.transform,
            "ForgeCountText",
            "🔨 LƯỢT RÈN: 1 KHẢ DỤNG",
            Vector2.zero,
            new Vector2(268f, 40f),
            15f,
            new Color(1f, 0.88f, 0.42f, 1f));

        // Nút về Menu Chính ở góc phải trên
        Button menuBtn = CreateStyledButton(
            topBar.transform,
            "MainMenuButton",
            "🏠 MENU CHÍNH",
            new Vector2(815f, -42f),
            new Vector2(200f, 46f),
            new Color(0.16f, 0.14f, 0.22f, 0.96f),
            new Color(0.75f, 0.65f, 0.45f, 0.8f),
            new Color(0.92f, 0.88f, 0.82f, 1f),
            15f);
        menuBtn.onClick.AddListener(OnMainMenuClicked);
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
        rt.sizeDelta = new Vector2(0f, 214f);

        Image barImg = bottomBar.AddComponent<Image>();
        barImg.color = new Color(0.05f, 0.05f, 0.09f, 0.96f);

        Outline barOl = bottomBar.AddComponent<Outline>();
        barOl.effectColor = new Color(0.85f, 0.66f, 0.22f, 0.75f);
        barOl.effectDistance = new Vector2(0f, 1.5f);

        // --- CỘT TRÁI: THÔNG TIN CHI TIẾT ẢI TIẾP THEO ---
        GameObject infoCard = CreatePillPanel(
            bottomBar.transform,
            "StageInfoCard",
            new Vector2(-575f, 107f),
            new Vector2(690f, 178f),
            new Color(0.09f, 0.08f, 0.14f, 0.96f),
            new Color(0.72f, 0.56f, 0.22f, 0.75f));

        stageDetailTitleLabel = CreateTMPChild(
            infoCard.transform,
            "StageDetailTitle",
            "⚔ ẢI TIẾP THEO: ẢI 2 — THÁP CANH HẮC ÁM",
            new Vector2(0f, 56f),
            new Vector2(650f, 34f),
            19f,
            new Color(1f, 0.84f, 0.36f, 1f),
            align: 513);

        stageDetailDescLabel = CreateTMPChild(
            infoCard.transform,
            "StageDetailDesc",
            "Kẻ địch: Armored Orc (35 HP • +5 Giáp khởi đầu)\nChiến thuật: Rèn thêm xúc xắc Sát thương lớn hoặc Đâm độc để xuyên lớp giáp dày của Orc.",
            new Vector2(0f, 4f),
            new Vector2(650f, 68f),
            15f,
            new Color(0.88f, 0.92f, 0.96f, 1f),
            align: 513);

        heroLocationStatusLabel = CreateTMPChild(
            infoCard.transform,
            "HeroLocationStatus",
            "📍 Vị trí Hiệp Sĩ: Đang ở Ải 1  ➔  Hãy bấm [DI CHUYỂN ĐẾN ẢI 2] hoặc click trực tiếp vào Ải 2 trên bản đồ!",
            new Vector2(0f, -56f),
            new Vector2(650f, 32f),
            14.5f,
            new Color(0.55f, 0.88f, 1f, 1f),
            align: 513);

        // --- CỘT GIỮA: TÓM TẮT 3 VIÊN XÚC XẮC TRONG TÚI ---
        GameObject diceBagCard = CreatePillPanel(
            bottomBar.transform,
            "DiceBagPreviewCard",
            new Vector2(60f, 107f),
            new Vector2(510f, 178f),
            new Color(0.09f, 0.08f, 0.14f, 0.96f),
            new Color(0.55f, 0.45f, 0.78f, 0.7f));

        CreateTMPChild(
            diceBagCard.transform,
            "DiceBagHeader",
            "🎲 TÚI XÚC XẮC HIỆN TẠI (3 VIÊN × 6 MẶT)",
            new Vector2(0f, 62f),
            new Vector2(480f, 28f),
            15.5f,
            new Color(0.95f, 0.82f, 0.42f, 1f));

        List<DiceInstance> bag = GameManager.Instance != null && GameManager.Instance.playerDiceBag != null && GameManager.Instance.playerDiceBag.Count > 0
            ? GameManager.Instance.playerDiceBag
            : DiceCatalog.CreateStarterDiceBag();

        for (int i = 0; i < Mathf.Min(3, bag.Count); i++)
        {
            DiceInstance die = bag[i];
            float yPos = 22f - i * 38f;

            GameObject rowGo = CreatePillPanel(
                diceBagCard.transform,
                $"DieRow_{i + 1}",
                new Vector2(0f, yPos),
                new Vector2(476f, 32f),
                new Color(0.14f, 0.12f, 0.20f, 0.92f),
                new Color(0.42f, 0.36f, 0.55f, 0.55f));

            string summary = BuildSingleDieFacesSummary(die);
            CreateTMPChild(
                rowGo.transform,
                "DieSummaryText",
                $"<b>#{i + 1} {die.diceName}:</b>  {summary}",
                Vector2.zero,
                new Vector2(460f, 28f),
                12.5f,
                new Color(0.92f, 0.94f, 0.98f, 1f),
                align: 513);
        }

        // --- CỘT PHẢI: 3 NÚT HÀNH ĐỘNG CHÍNH (RÈN XÚC XẮC / DI CHUYỂN / VÀO CHIẾN ĐẤU) ---
        // 1. Nút Rèn Xúc Xắc (Mở scene TheForge)
        openForgeButton = CreateStyledButton(
            bottomBar.transform,
            "OpenForgeButton",
            "🔨 RÈN XÚC XẮC (XƯỞNG RÈN)",
            new Vector2(635f, 162f),
            new Vector2(580f, 52f),
            new Color(0.48f, 0.28f, 0.05f, 0.98f),
            new Color(1f, 0.82f, 0.24f, 0.95f),
            new Color(1f, 0.95f, 0.75f, 1f),
            17f);
        openForgeButton.onClick.AddListener(OnOpenForgeClicked);
        openForgeGlowOutlineImg = openForgeButton.GetComponent<Image>();

        // 2. Nút Di Chuyển Đến Ải Tiếp Theo trên Map
        moveNextStageButton = CreateStyledButton(
            bottomBar.transform,
            "MoveNextStageButton",
            $"🚶 DI CHUYỂN ĐẾN ẢI {currentTargetStage}",
            new Vector2(485f, 98f),
            new Vector2(280f, 52f),
            new Color(0.14f, 0.26f, 0.48f, 0.98f),
            new Color(0.38f, 0.78f, 1f, 0.95f),
            Color.white,
            15.5f);
        moveNextStageButton.onClick.AddListener(OnMoveToNextStageClicked);

        // 3. Nút Vào Chiến Đấu (Bắt đầu trận đánh ở Ải tiếp theo)
        enterCombatButton = CreateStyledButton(
            bottomBar.transform,
            "EnterCombatButton",
            $"⚔ KHIÊU CHIẾN ẢI {currentTargetStage}",
            new Vector2(785f, 98f),
            new Vector2(280f, 52f),
            new Color(0.64f, 0.12f, 0.14f, 0.98f),
            new Color(1f, 0.78f, 0.28f, 0.95f),
            Color.white,
            16.5f);
        enterCombatButton.onClick.AddListener(OnEnterCombatClicked);

        // Dòng hướng dẫn thao tác nhanh dưới đáy cột phải
        CreateTMPChild(
            bottomBar.transform,
            "ControlHintText",
            "💡 <i>Mẹo: Bạn có thể vào <b>Xưởng Rèn</b> trước rồi quay lại Bản Đồ để <b>Di chuyển & Khiêu chiến</b>!</i>",
            new Vector2(635f, 38f),
            new Vector2(580f, 30f),
            13.5f,
            new Color(0.82f, 0.86f, 0.94f, 0.9f));
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
            "Hiệp Sĩ đã đánh bại Minotaur Boss và chinh phục toàn bộ 3 Ải của Vùng Đất Nguyền Rủa!\nBạn có thể ghé Xưởng Rèn Huyền Thoại hoặc bắt đầu một Hành Trình Mới.",
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

        // 1. Cập nhật Thanh Máu & Lượt Rèn trên Top Header
        if (hpStatusLabel != null)
        {
            SetTMPText(hpStatusLabel, $"♥ HP HIỆP SĨ: {curHP} / {maxHP}");
        }
        if (hpFillImage != null && maxHP > 0)
        {
            RectTransform fillRt = hpFillImage.rectTransform;
            fillRt.anchorMax = new Vector2(Mathf.Clamp01((float)curHP / maxHP), 1f);
        }

        if (forgeBadgeLabel != null)
        {
            if (forgeCount > 0)
            {
                SetTMPText(forgeBadgeLabel, $"🔨 LƯỢT RÈN: +{forgeCount} SẴN SÀNG!");
            }
            else
            {
                SetTMPText(forgeBadgeLabel, "🔨 XƯỞNG RÈN: ĐÃ RÈN XONG");
            }
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
                seg.color = new Color(0.35f, 0.30f, 0.45f, 0.38f); // Chưa mở khóa
        }

        // 3. Cập nhật trạng thái 3 Nút Ải (Stage Nodes)
        for (int i = 0; i < stageNodeObjects.Count; i++)
        {
            int stageNum = i + 1;
            GameObject nodeGo = stageNodeObjects[i];
            Outline ol = stageNodeOutlines[i];
            Component statusLbl = stageNodeStatusLabels[i];
            if (nodeGo == null) continue;

            Image bgImg = nodeGo.GetComponent<Image>();
            bool isCleared = stageNum <= lastCleared;
            bool isNextTarget = !isVictory && (stageNum == currentTargetStage);
            bool isHeroHere = (stageNum == heroMapStage);

            if (isCleared && !isNextTarget)
            {
                if (bgImg != null) bgImg.color = new Color(0.06f, 0.28f, 0.18f, 0.96f);
                if (ol != null) ol.effectColor = new Color(0.22f, 0.92f, 0.55f, 0.95f);
                SetTMPText(statusLbl, isHeroHere
                    ? "<color=#6ee7b7>✓ ĐÃ QUA (ĐANG ĐỨNG)</color>"
                    : "<color=#6ee7b7>✓ ĐÃ VƯỢT QUA</color>");
            }
            else if (isNextTarget)
            {
                if (bgImg != null) bgImg.color = new Color(0.38f, 0.14f, 0.18f, 0.98f);
                if (ol != null) ol.effectColor = new Color(1f, 0.84f, 0.22f, 1f);
                SetTMPText(statusLbl, isHeroHere
                    ? "<color=#fde047>⚔ CLICK ĐỂ VÀO TRẬN!</color>"
                    : "<color=#fde047>🚶 CLICK ĐỂ DI CHUYỂN TỚI</color>");
            }
            else
            {
                if (bgImg != null) bgImg.color = new Color(0.12f, 0.11f, 0.16f, 0.82f);
                if (ol != null) ol.effectColor = new Color(0.35f, 0.32f, 0.42f, 0.65f);
                SetTMPText(statusLbl, "<color=#94a3b8>🔒 CHƯA MỞ KHÓA</color>");
            }
        }

        // 4. Cập nhật Thông tin Ải ở Bottom Panel
        UpdateStageDetailCard(selectedInspectStage, forgeCount);

        // 5. Cập nhật trạng thái 3 Nút Hành Động chính
        if (openForgeButton != null)
        {
            openForgeButton.interactable = !isHeroMoving && !isTransitioningScene;
            string forgeBtnText = forgeCount > 0
                ? $"🔨 RÈN XÚC XẮC NGAY (+{forgeCount} LƯỢT RÈN MỚI!)"
                : "🔨 VÀO XƯỞNG RÈN XÚC XẮC (XEM TÚI XÚC XẮC)";
            UpdateButtonText(openForgeButton, forgeBtnText);
        }

        bool needsMove = heroMapStage < currentTargetStage;
        if (moveNextStageButton != null)
        {
            moveNextStageButton.interactable = !isHeroMoving && !isTransitioningScene && needsMove;
            UpdateButtonText(moveNextStageButton, needsMove
                ? $"🚶 DI CHUYỂN ĐẾN ẢI {currentTargetStage}"
                : $"📍 ĐÃ ĐẾN ẢI {currentTargetStage}");
        }

        if (enterCombatButton != null)
        {
            enterCombatButton.interactable = !isHeroMoving && !isTransitioningScene;
            UpdateButtonText(enterCombatButton, needsMove
                ? $"⚔ TIẾN TỚI & ĐÁNH ẢI {currentTargetStage}"
                : $"⚔ VÀO CHIẾN ĐẤU (ẢI {currentTargetStage})");
        }
    }

    private void UpdateStageDetailCard(int stageNum, int forgeCount)
    {
        string title;
        string desc;

        switch (stageNum)
        {
            case 1:
                title = "⚔ ẢI 1: CỬA RỪNG NGUYỀN RỦA — GOBLIN SCOUT";
                desc = "• Chỉ số Địch: <b>20 HP</b>  |  Giáp khởi đầu: <b>0</b>\n" +
                       "• Đặc trưng: Tốc kích chém nhanh (4-6 ST) và Đâm mũi kiếm tẩm độc.";
                break;
            case 2:
                title = "⚔ ẢI 2: THÁP CANH HẮC ÁM — ARMORED ORC";
                desc = "• Chỉ số Địch: <b>35 HP</b>  |  Giáp khởi đầu: <b>5 🛡</b>\n" +
                       "• Đặc trưng: Dựng khiên dày, Gồng cuồng nộ tăng sát thương và bổ rìu hạng nặng.";
                break;
            default:
                title = "👑 ẢI 3 (TRÙM CUỐI): PHÁO ĐÀI HUYẾT NGUYỆT — MINOTAUR BOSS";
                desc = "• Chỉ số Địch: <b>60 HP</b>  |  Trùm Cuối Hầm Ngục\n" +
                       "• Đặc trưng: Trọng kích hủy diệt, Hấp huyết hồi máu và Gồng cuồng nộ liên hoàn.";
                break;
        }

        SetTMPText(stageDetailTitleLabel, title);
        SetTMPText(stageDetailDescLabel, desc);

        if (heroLocationStatusLabel != null)
        {
            if (heroMapStage < currentTargetStage)
            {
                string forgeRemind = forgeCount > 0
                    ? "Bạn đang có <b>+1 Lượt Rèn Xúc Xắc</b>! Hãy ghé <b>Xưởng Rèn</b> hoặc bấm <b>Di Chuyển Đến Ải Tiếp Theo</b>."
                    : $"Hãy bấm <b>[DI CHUYỂN ĐẾN ẢI {currentTargetStage}]</b> (hoặc click vào Ải {currentTargetStage} trên bản đồ) để tiến quân!";
                SetTMPText(heroLocationStatusLabel, $"📍 Hiệp Sĩ đang ở Ải {heroMapStage}  ➔  {forgeRemind}");
            }
            else
            {
                string readyRemind = forgeCount > 0
                    ? $"📍 Đã di chuyển tới <b>Ải {currentTargetStage}</b>! Bạn vẫn còn <b>+{forgeCount} Lượt Rèn</b> trong Xưởng Rèn trước khi vào trận."
                    : $"📍 Hiệp Sĩ đã đứng trước cổng <b>Ải {currentTargetStage}</b> & hoàn tất chuẩn bị — Sẵn sàng xuất chiến!";
                SetTMPText(heroLocationStatusLabel, readyRemind);
            }
        }
    }

    // =========================================================================
    // 4. XỬ LÝ DI CHUYỂN HIỆP SĨ TRÊN MAP & CHUYỂN SCENE (FORGE / COMBAT / MENU)
    // =========================================================================

    public void OnStageNodeClicked(int clickedStage)
    {
        if (isHeroMoving || isTransitioningScene) return;

        selectedInspectStage = clickedStage;

        // Nếu click vào ải chưa mở khóa
        if (clickedStage > highestUnlockedStage)
        {
            RefreshAllMapVisuals();
            if (heroLocationStatusLabel != null)
            {
                SetTMPText(
                    heroLocationStatusLabel,
                    $"🔒 <b>Ải {clickedStage}</b> chưa mở khóa! Hãy vượt qua <b>Ải {currentTargetStage}</b> trước.");
            }
            return;
        }

        // Nếu Hiệp Sĩ đang ở ải khác với ải vừa click (đã mở khóa) -> Di chuyển Hiệp Sĩ tới ải đó!
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
        Vector2 heroOffset = new Vector2(-58f, 46f);

        for (int i = 1; i < waypoints.Count; i++)
        {
            Vector2 startPos = heroTokenRect != null ? heroTokenRect.anchoredPosition : (waypoints[i - 1] + heroOffset);
            Vector2 endPos = waypoints[i] + heroOffset;

            float duration = 0.24f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);

                // Hiệu ứng bước nhảy nhẹ khi Hiệp Sĩ di chuyển trên đường mòn
                float hopY = Mathf.Sin(t * Mathf.PI) * 18f;
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

        if (heroLocationStatusLabel != null)
        {
            SetTMPText(heroLocationStatusLabel, $"⚔ Đang tiến vào <b>Ải {currentTargetStage}</b> — Chuẩn bị đổ xúc xắc!");
        }

        // Hiệu ứng phóng nhẹ quân cờ Hiệp Sĩ trước khi vào trận
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

    // =========================================================================
    // 5. HIỆU ỨNG ĐỘNG TRÊN BẢN ĐỒ & PHÍM TẮT ĐIỀU KHIỂN
    // =========================================================================

    private void AnimateHeroTokenAndTargetNodes()
    {
        float time = Time.time;

        // 1. Nhịp thở hào quang của Quân cờ Hiệp Sĩ
        if (heroHaloImage != null)
        {
            float pulse = 0.28f + Mathf.Sin(time * 3.5f) * 0.14f;
            heroHaloImage.color = new Color(1f, 0.84f, 0.25f, pulse);
            float scale = 1f + Mathf.Sin(time * 3.5f) * 0.08f;
            heroHaloImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // 2. Nhấp nháy nhẹ Nút Ải Tiếp Theo để thu hút sự chú ý của người chơi
        int targetIdx = currentTargetStage - 1;
        if (targetIdx >= 0 && targetIdx < stageNodeObjects.Count && stageNodeObjects[targetIdx] != null)
        {
            float nodeScale = 1f + Mathf.Sin(time * 3.2f) * 0.045f;
            stageNodeObjects[targetIdx].transform.localScale = new Vector3(nodeScale, nodeScale, 1f);
        }

        // 3. Nếu còn lượt Rèn Xúc Xắc khả dụng, làm sáng nhẹ nút Xưởng Rèn
        if (openForgeGlowOutlineImg != null && GameManager.Instance != null && GameManager.Instance.pendingForgeUpgrades > 0)
        {
            float glow = 0.46f + Mathf.Sin(time * 4f) * 0.12f;
            openForgeGlowOutlineImg.color = new Color(glow, 0.28f, 0.05f, 0.98f);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (isHeroMoving || isTransitioningScene) return;

        // Phím D / Mũi tên Phải: Di chuyển đến ải tiếp theo
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (heroMapStage < currentTargetStage)
            {
                OnMoveToNextStageClicked();
            }
        }
        // Phím F: Mở nhanh Xưởng Rèn Xúc Xắc
        else if (Input.GetKeyDown(KeyCode.F))
        {
            OnOpenForgeClicked();
        }
        // Phím Space / Enter: Vào chiến đấu nếu đã đứng ở ải tiếp theo, hoặc di chuyển tới đó
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

    private string BuildSingleDieFacesSummary(DiceInstance die)
    {
        if (die == null || die.faces == null) return "";
        List<string> parts = new List<string>();
        for (int i = 0; i < die.faces.Length; i++)
        {
            DiceFaceData f = die.faces[i];
            if (f == null) continue;
            string sym = GetSkillShortIcon(f.GetResolvedSkillKind());
            parts.Add($"{sym}{f.valueV}");
        }
        return string.Join(" • ", parts);
    }

    private string GetSkillShortIcon(DiceSkillKind kind)
    {
        switch (kind)
        {
            case DiceSkillKind.Strike: return "⚔";
            case DiceSkillKind.HeavySlash: return "💥";
            case DiceSkillKind.Vampiric: return "🩸";
            case DiceSkillKind.Block: return "🛡";
            case DiceSkillKind.IronWall: return "🏰";
            case DiceSkillKind.Regenerate: return "💚";
            case DiceSkillKind.Purify: return "✨";
            case DiceSkillKind.PoisonSting: return "☠";
            case DiceSkillKind.ShockStun: return "⚡";
            default: return "💨";
        }
    }

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
