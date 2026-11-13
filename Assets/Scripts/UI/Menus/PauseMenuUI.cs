using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PauseMenuUI — Menu Tạm Dừng (Pause Modal) & Thanh Nút Truy Cập Nhanh (Bách Khoa Xúc Xắc / Tạm Dừng)
/// ở góc phải trên màn hình cho CombatScene, StageMap và TheForge. Hỗ trợ phím tắt [ESC].
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private GameObject pauseModalObject;
    private Transform canvasTransform;

    public static PauseMenuUI AttachToCanvas(Transform parentCanvas)
    {
        if (parentCanvas == null)
        {
            Canvas c = FindAnyObjectByType<Canvas>();
            if (c != null) parentCanvas = c.transform;
        }
        if (parentCanvas == null) return null;

        PauseMenuUI existing = parentCanvas.GetComponentInChildren<PauseMenuUI>(true);
        if (existing != null) return existing;

        GameObject hostGo = new GameObject("PauseAndQuickBarUI", typeof(RectTransform));
        hostGo.transform.SetParent(parentCanvas, false);
        RectTransform rt = hostGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        PauseMenuUI ui = hostGo.AddComponent<PauseMenuUI>();
        ui.canvasTransform = parentCanvas;
        ui.BuildTopRightQuickButtons();
        ui.BuildPauseModal();
        return ui;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseModal();
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    public void TogglePauseModal()
    {
        if (pauseModalObject == null) return;
        bool nextState = !pauseModalObject.activeSelf;
        pauseModalObject.SetActive(nextState);
        if (nextState)
        {
            transform.SetAsLastSibling();
            pauseModalObject.transform.SetAsLastSibling();
        }
    }

    public void OpenCodex()
    {
        if (pauseModalObject != null) pauseModalObject.SetActive(false);
        DiceCodexModalUI.ToggleOrCreate(canvasTransform);
    }

    private void BuildTopRightQuickButtons()
    {
        // Nút mở Bách Khoa Xúc Xắc (Codex)
        Button codexBtn = CreateButton(
            transform,
            "QuickCodexButton",
            "BÁCH KHOA (13 MẶT)",
            new Vector2(610f, 498f),
            new Vector2(215f, 38f),
            new Color(0.12f, 0.22f, 0.38f, 0.94f),
            new Color(0.38f, 0.78f, 1f, 0.85f),
            13.5f);
        codexBtn.onClick.AddListener(OpenCodex);

        // Nút Menu / Pause [ESC]
        Button pauseBtn = CreateButton(
            transform,
            "QuickPauseButton",
            "MENU [ESC]",
            new Vector2(810f, 498f),
            new Vector2(150f, 38f),
            new Color(0.16f, 0.14f, 0.24f, 0.94f),
            new Color(0.85f, 0.68f, 0.25f, 0.85f),
            13.5f);
        pauseBtn.onClick.AddListener(TogglePauseModal);
    }

    private void BuildPauseModal()
    {
        pauseModalObject = new GameObject("PauseModalOverlay", typeof(RectTransform), typeof(Image));
        pauseModalObject.transform.SetParent(transform, false);
        RectTransform rt = pauseModalObject.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        pauseModalObject.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.82f);

        GameObject box = new GameObject("PauseDialog", typeof(RectTransform), typeof(Image), typeof(Outline));
        box.transform.SetParent(pauseModalObject.transform, false);
        RectTransform boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(500f, 440f);
        boxRt.anchoredPosition = Vector2.zero;

        box.GetComponent<Image>().color = new Color(0.08f, 0.07f, 0.13f, 0.98f);
        Outline ol = box.GetComponent<Outline>();
        ol.effectColor = new Color(0.92f, 0.75f, 0.25f, 0.95f);
        ol.effectDistance = new Vector2(2f, -2f);

        GameObject titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(box.transform, false);
        RectTransform tRt = titleGo.GetComponent<RectTransform>();
        tRt.anchoredPosition = new Vector2(0f, 155f);
        tRt.sizeDelta = new Vector2(440f, 44f);
        TextMeshProUGUI tTmp = titleGo.GetComponent<TextMeshProUGUI>();
        tTmp.text = "⚙ TẠM DỪNG TRÒ CHƠI";
        tTmp.fontSize = 26f;
        tTmp.fontStyle = FontStyles.Bold;
        tTmp.alignment = TextAlignmentOptions.Center;
        tTmp.color = new Color(1f, 0.86f, 0.32f, 1f);

        // 1. Tiếp tục
        Button resumeBtn = CreateButton(
            box.transform,
            "ResumeButton",
            "▶ TIẾP TỤC CHIẾN ĐẤU [ESC]",
            new Vector2(0f, 75f),
            new Vector2(360f, 50f),
            new Color(0.14f, 0.38f, 0.24f, 0.98f),
            new Color(0.35f, 0.92f, 0.58f, 0.9f),
            16f);
        resumeBtn.onClick.AddListener(TogglePauseModal);

        // 2. Xem Bách Khoa Xúc Xắc & Túi Đồ
        Button codexBtn = CreateButton(
            box.transform,
            "CodexInPauseButton",
            "BÁCH KHOA 13 MẶT XÚC XẮC & TÚI ĐỒ",
            new Vector2(0f, 10f),
            new Vector2(360f, 50f),
            new Color(0.14f, 0.26f, 0.46f, 0.98f),
            new Color(0.38f, 0.78f, 1f, 0.9f),
            15f);
        codexBtn.onClick.AddListener(OpenCodex);

        // 3. Chơi lại từ đầu (Restart Run)
        Button restartBtn = CreateButton(
            box.transform,
            "RestartRunButton",
            "CHƠI LẠI HÀNH TRÌNH MỚI (ẢI 1)",
            new Vector2(0f, -55f),
            new Vector2(360f, 50f),
            new Color(0.48f, 0.26f, 0.08f, 0.98f),
            new Color(1f, 0.78f, 0.25f, 0.9f),
            15f);
        restartBtn.onClick.AddListener(() =>
        {
            pauseModalObject.SetActive(false);
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.StartNewRun();
            else SceneManager.LoadScene("CombatScene");
        });

        // 4. Về Menu Chính
        Button mainMenuBtn = CreateButton(
            box.transform,
            "ReturnMainMenuButton",
            "VỀ MENU CHÍNH (MAIN MENU)",
            new Vector2(0f, -120f),
            new Vector2(360f, 50f),
            new Color(0.52f, 0.14f, 0.16f, 0.98f),
            new Color(1f, 0.45f, 0.45f, 0.9f),
            15f);
        mainMenuBtn.onClick.AddListener(() =>
        {
            pauseModalObject.SetActive(false);
            GameManager gm = GameManager.ResolveInstance();
            if (gm != null) gm.LoadMainMenu();
            else SceneManager.LoadScene("MainMenu");
        });

        pauseModalObject.SetActive(false);
    }

    private Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchoredPos,
        Vector2 size,
        Color bgColor,
        Color borderColor,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        Image img = go.GetComponent<Image>();
        img.color = bgColor;

        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = borderColor;
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        GameObject txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(6f, 2f);
        txtRt.offsetMax = new Vector2(-6f, -2f);

        TextMeshProUGUI tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        return btn;
    }
}

