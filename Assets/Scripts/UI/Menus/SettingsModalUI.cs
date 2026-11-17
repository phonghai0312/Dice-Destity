using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SettingsModalUI — Cửa sổ Cài Đặt Âm Thanh & Gameplay dùng chung cho MainMenu và PauseMenuUI.
/// </summary>
public class SettingsModalUI : MonoBehaviour
{
    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private TextMeshProUGUI masterValText;
    private TextMeshProUGUI bgmValText;
    private TextMeshProUGUI sfxValText;

    private Button muteBtn;
    private TextMeshProUGUI muteBtnText;
    private Image muteBtnImg;

    private Button shakeBtn;
    private TextMeshProUGUI shakeBtnText;
    private Image shakeBtnImg;

    public static SettingsModalUI ToggleOrCreate(Transform parentCanvas, bool showCodexButton = false)
    {
        if (parentCanvas == null)
        {
            Canvas c = FindAnyObjectByType<Canvas>();
            if (c != null) parentCanvas = c.transform;
        }
        if (parentCanvas == null) return null;

        SettingsModalUI existing = parentCanvas.GetComponentInChildren<SettingsModalUI>(true);
        if (existing != null)
        {
            bool next = !existing.gameObject.activeSelf;
            existing.gameObject.SetActive(next);
            if (next)
            {
                existing.transform.SetAsLastSibling();
                existing.SyncFromAudioManager();
            }
            return existing;
        }

        GameObject modalObj = new GameObject("SettingsModal", typeof(RectTransform), typeof(Image));
        modalObj.transform.SetParent(parentCanvas, false);
        modalObj.transform.SetAsLastSibling();

        RectTransform modalRect = modalObj.GetComponent<RectTransform>();
        modalRect.anchorMin = Vector2.zero;
        modalRect.anchorMax = Vector2.one;
        modalRect.offsetMin = Vector2.zero;
        modalRect.offsetMax = Vector2.zero;

        Image backdropImg = modalObj.GetComponent<Image>();
        backdropImg.color = new Color(0.02f, 0.01f, 0.04f, 0.84f);

        SettingsModalUI ui = modalObj.AddComponent<SettingsModalUI>();
        ui.BuildUI(showCodexButton);
        ui.SyncFromAudioManager();
        return ui;
    }

    private void OnEnable()
    {
        AudioManager.OnSettingsChanged += SyncFromAudioManager;
    }

    private void OnDisable()
    {
        AudioManager.OnSettingsChanged -= SyncFromAudioManager;
    }

    private void BuildUI(bool showCodexButton)
    {
        AudioManager am = AudioManager.ResolveInstance();

        GameObject boxObj = new GameObject("DialogBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        boxObj.transform.SetParent(transform, false);
        RectTransform boxRect = boxObj.GetComponent<RectTransform>();
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(560f, 470f);
        boxRect.anchoredPosition = Vector2.zero;

        boxObj.GetComponent<Image>().color = new Color(0.09f, 0.07f, 0.14f, 0.98f);
        Outline outline = boxObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.88f, 0.70f, 0.28f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Title
        CreateLabel(boxObj.transform, "TitleText", "⚙ CÀI ĐẶT (SETTINGS)", new Vector2(0f, 195f), new Vector2(480f, 44f), 28f, FontStyles.Bold, new Color(0.96f, 0.80f, 0.34f, 1f), TextAlignmentOptions.Center);

        // Sliders
        CreateSliderRow(boxObj.transform, "Âm lượng tổng (Master)", 125f, out masterSlider, out masterValText, val =>
        {
            AudioManager.ResolveInstance().SetMasterVolume(val);
        });

        CreateSliderRow(boxObj.transform, "Nhạc nền (Music)", 60f, out bgmSlider, out bgmValText, val =>
        {
            AudioManager.ResolveInstance().SetBgmVolume(val);
        });

        CreateSliderRow(boxObj.transform, "Hiệu ứng (SFX)", -5f, out sfxSlider, out sfxValText, val =>
        {
            AudioManager.ResolveInstance().SetSfxVolume(val);
        });

        // Toggles Row
        muteBtn = CreateActionButton(boxObj.transform, "MuteToggleButton", "TẮT TIẾNG: TẮT", new Vector2(-125f, -75f), new Vector2(230f, 44f), new Color(0.16f, 0.22f, 0.32f, 1f), new Color(0.45f, 0.65f, 0.90f, 0.9f), 15f, out muteBtnImg, out muteBtnText);
        muteBtn.onClick.AddListener(() =>
        {
            AudioManager mgr = AudioManager.ResolveInstance();
            mgr.SetMuted(!AudioManager.IsMuted);
            if (!AudioManager.IsMuted) mgr.PlayButtonClick();
        });

        shakeBtn = CreateActionButton(boxObj.transform, "ShakeToggleButton", "RUNG MÀN HÌNH: BẬT", new Vector2(125f, -75f), new Vector2(230f, 44f), new Color(0.16f, 0.34f, 0.24f, 1f), new Color(0.40f, 0.88f, 0.56f, 0.9f), 15f, out shakeBtnImg, out shakeBtnText);
        shakeBtn.onClick.AddListener(() =>
        {
            AudioManager mgr = AudioManager.ResolveInstance();
            mgr.SetScreenShake(!AudioManager.IsScreenShakeEnabled);
            mgr.PlayButtonClick();
        });

        // Utility Row: Test SFX + Reset
        Button testBtn = CreateActionButton(boxObj.transform, "TestSfxButton", "🔊 THỬ ÂM THANH", new Vector2(-125f, -135f), new Vector2(230f, 42f), new Color(0.24f, 0.20f, 0.38f, 1f), new Color(0.70f, 0.55f, 0.95f, 0.9f), 14.5f, out _, out _);
        testBtn.onClick.AddListener(() => AudioManager.ResolveInstance().PlayForge());

        Button resetBtn = CreateActionButton(boxObj.transform, "ResetSettingsButton", "↺ MẶC ĐỊNH", new Vector2(125f, -135f), new Vector2(230f, 42f), new Color(0.35f, 0.24f, 0.14f, 1f), new Color(0.90f, 0.68f, 0.30f, 0.9f), 14.5f, out _, out _);
        resetBtn.onClick.AddListener(() =>
        {
            AudioManager mgr = AudioManager.ResolveInstance();
            mgr.ResetToDefaults();
            mgr.PlayButtonClick();
        });

        // Bottom Row
        if (showCodexButton)
        {
            Button codexBtn = CreateActionButton(boxObj.transform, "OpenCodexButton", "📖 BÁCH KHOA (13 MẶT)", new Vector2(-125f, -195f), new Vector2(230f, 46f), new Color(0.14f, 0.28f, 0.48f, 1f), new Color(0.38f, 0.78f, 1f, 0.9f), 15f, out _, out _);
            codexBtn.onClick.AddListener(() =>
            {
                AudioManager.ResolveInstance().PlayButtonClick();
                gameObject.SetActive(false);
                DiceCodexModalUI.ToggleOrCreate(transform.parent);
            });

            Button closeBtn = CreateActionButton(boxObj.transform, "CloseSettingsButton", "ĐÓNG", new Vector2(125f, -195f), new Vector2(230f, 46f), new Color(0.55f, 0.16f, 0.16f, 1f), new Color(1f, 0.45f, 0.45f, 0.9f), 16f, out _, out _);
            closeBtn.onClick.AddListener(() =>
            {
                AudioManager.ResolveInstance().PlayButtonClick();
                gameObject.SetActive(false);
            });
        }
        else
        {
            Button closeBtn = CreateActionButton(boxObj.transform, "CloseSettingsButton", "ĐÓNG", new Vector2(0f, -195f), new Vector2(240f, 46f), new Color(0.55f, 0.16f, 0.16f, 1f), new Color(1f, 0.45f, 0.45f, 0.9f), 16f, out _, out _);
            closeBtn.onClick.AddListener(() =>
            {
                AudioManager.ResolveInstance().PlayButtonClick();
                gameObject.SetActive(false);
            });
        }
    }

    private void SyncFromAudioManager()
    {
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(AudioManager.MasterVolume);
        if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(AudioManager.BgmVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);

        if (masterValText != null) masterValText.text = Mathf.RoundToInt(AudioManager.MasterVolume * 100f) + "%";
        if (bgmValText != null) bgmValText.text = Mathf.RoundToInt(AudioManager.BgmVolume * 100f) + "%";
        if (sfxValText != null) sfxValText.text = Mathf.RoundToInt(AudioManager.SfxVolume * 100f) + "%";

        if (muteBtnText != null && muteBtnImg != null)
        {
            muteBtnText.text = AudioManager.IsMuted ? "TẮT TIẾNG: BẬT" : "TẮT TIẾNG: TẮT";
            muteBtnImg.color = AudioManager.IsMuted
                ? new Color(0.52f, 0.16f, 0.18f, 1f)
                : new Color(0.16f, 0.22f, 0.34f, 1f);
        }

        if (shakeBtnText != null && shakeBtnImg != null)
        {
            shakeBtnText.text = AudioManager.IsScreenShakeEnabled ? "RUNG MÀN HÌNH: BẬT" : "RUNG MÀN HÌNH: TẮT";
            shakeBtnImg.color = AudioManager.IsScreenShakeEnabled
                ? new Color(0.14f, 0.36f, 0.22f, 1f)
                : new Color(0.26f, 0.22f, 0.26f, 1f);
        }
    }

    private void CreateSliderRow(Transform parent, string label, float yPos, out Slider slider, out TextMeshProUGUI valueTmp, UnityEngine.Events.UnityAction<float> onChanged)
    {
        CreateLabel(parent, label + "_Label", label, new Vector2(-135f, yPos + 16f), new Vector2(230f, 26f), 16f, FontStyles.Bold, new Color(0.92f, 0.90f, 0.86f, 1f), TextAlignmentOptions.Left);
        valueTmp = CreateLabel(parent, label + "_Value", "100%", new Vector2(185f, yPos + 16f), new Vector2(80f, 26f), 16f, FontStyles.Bold, new Color(0.96f, 0.80f, 0.34f, 1f), TextAlignmentOptions.Right);

        GameObject sliderGo = new GameObject(label + "_Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(parent, false);
        RectTransform sRt = sliderGo.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(0f, yPos - 10f);
        sRt.sizeDelta = new Vector2(460f, 22f);

        // Background
        GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgGo.transform.SetParent(sliderGo.transform, false);
        RectTransform bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0f, 0.25f);
        bgRt.anchorMax = new Vector2(1f, 0.75f);
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgGo.GetComponent<Image>().color = new Color(0.18f, 0.16f, 0.24f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0f, 0.25f);
        faRt.anchorMax = new Vector2(1f, 0.75f);
        faRt.offsetMin = new Vector2(4f, 0f);
        faRt.offsetMax = new Vector2(-4f, 0f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(fillArea.transform, false);
        RectTransform fillRt = fillGo.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        fillGo.GetComponent<Image>().color = new Color(0.90f, 0.70f, 0.24f, 1f);

        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGo.transform, false);
        RectTransform haRt = handleArea.GetComponent<RectTransform>();
        haRt.anchorMin = Vector2.zero;
        haRt.anchorMax = Vector2.one;
        haRt.offsetMin = new Vector2(10f, 0f);
        haRt.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGo.transform.SetParent(handleArea.transform, false);
        RectTransform hRt = handleGo.GetComponent<RectTransform>();
        hRt.sizeDelta = new Vector2(22f, 22f);
        handleGo.GetComponent<Image>().color = new Color(1f, 0.88f, 0.45f, 1f);

        slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRt;
        slider.handleRect = hRt;
        slider.targetGraphic = handleGo.GetComponent<Image>();
        slider.onValueChanged.AddListener(onChanged);
    }

    private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, Color borderColor, float fontSize, out Image bgImg, out TextMeshProUGUI labelTmp)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        bgImg = go.GetComponent<Image>();
        bgImg.color = bgColor;

        Outline ol = go.GetComponent<Outline>();
        ol.effectColor = borderColor;
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = bgImg;

        GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        RectTransform txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(6f, 2f);
        txtRt.offsetMax = new Vector2(-6f, -2f);

        labelTmp = txtGo.GetComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = fontSize;
        labelTmp.fontStyle = FontStyles.Bold;
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.color = Color.white;
        labelTmp.raycastTarget = false;

        return btn;
    }
}
