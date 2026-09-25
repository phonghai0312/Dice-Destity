using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Rebuilds all 3 scenes (MainMenu, CombatScene, TheForge) to match the Figma/React design.
/// Run: Tools -> Rebuild All Scenes From Design
/// </summary>
public class RebuildAllScenes
{
    // ─── Design Tokens ────────────────────────────────────────────────────────
    static Color cBg, cCard, cPrimary, cPrimaryFg, cMutedFg, cAccent, cBorder;
    static System.Type tmpType;

    [MenuItem("Tools/Rebuild All Scenes From Design")]
    public static void RebuildAll()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode first!"); return; }

        ColorUtility.TryParseHtmlString("#0a090d", out cBg);
        ColorUtility.TryParseHtmlString("#13111a", out cCard);
        ColorUtility.TryParseHtmlString("#a81c1c", out cPrimary);
        ColorUtility.TryParseHtmlString("#fef2f2", out cPrimaryFg);
        ColorUtility.TryParseHtmlString("#7a7285", out cMutedFg);
        ColorUtility.TryParseHtmlString("#b8860b", out cAccent);
        ColorUtility.TryParseHtmlString("#2a2535", out cBorder);

        tmpType = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        RebuildMainMenu();
        RebuildCombat();
        RebuildForge();

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("=== All scenes rebuilt from design! ===");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static RectTransform FullScreen(GameObject go)
    {
        // After parenting to UI hierarchy, go.transform becomes RectTransform
        RectTransform rt = go.transform as RectTransform;
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static RectTransform Rect(GameObject go, Vector2 pos, Vector2 size, float ancX = 0.5f, float ancY = 0.5f)
    {
        RectTransform rt = go.transform as RectTransform;
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ancX, ancY);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(GameObject go, Color col, Sprite spr = null)
    {
        Image img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = col;
        if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; }
        return img;
    }

    static Component TMP(GameObject go, string text, float size, Color col, bool wrap = false, int align = 514)
    {
        if (tmpType == null) return null;
        Component c = go.GetComponent(tmpType) ?? go.AddComponent(tmpType);
        tmpType.GetProperty("text")?.SetValue(c, text);
        tmpType.GetProperty("fontSize")?.SetValue(c, size);
        tmpType.GetProperty("color")?.SetValue(c, col);
        tmpType.GetProperty("alignment")?.SetValue(c, align);
        tmpType.GetProperty("enableWordWrapping")?.SetValue(c, wrap);
        tmpType.GetProperty("overflowMode")?.SetValue(c, 0); // Overflow
        return c;
    }

    static Sprite LoadSprite(params string[] paths)
    {
        foreach (var p in paths)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) return s;
        }
        return null;
    }

    static void FixES()
    {
        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj == null) { esObj = new GameObject("EventSystem"); esObj.AddComponent<UnityEngine.EventSystems.EventSystem>(); }
        foreach (var m in esObj.GetComponents<UnityEngine.EventSystems.BaseInputModule>()) Object.DestroyImmediate(m);
        System.Type t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) { var m = esObj.AddComponent(t); t.GetMethod("AssignDefaultActions")?.Invoke(m, null); }
        else esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject Canvas(string sceneName, string canvasName = "Canvas")
    {
        GameObject c = GameObject.Find(canvasName);
        if (c == null) { c = new GameObject(canvasName); }
        // Clear children
        for (int i = c.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(c.transform.GetChild(i).gameObject);

        Canvas cv = c.GetComponent<Canvas>() ?? c.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = c.GetComponent<CanvasScaler>() ?? c.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        if (c.GetComponent<GraphicRaycaster>() == null) c.AddComponent<GraphicRaycaster>();
        return c;
    }

    static void EnsureCamera()
    {
        Camera cam = Object.FindObjectOfType<Camera>();
        if (cam == null)
        {
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.depth = -1;
            camGo.AddComponent<AudioListener>();
        }
    }

    /// <summary>Creates a styled portrait frame with gold corner ornaments</summary>
    static GameObject Portrait(Transform parent, string label, Sprite sprite, Vector2 pos, Vector2 size, bool isBoss = false)
    {
        // Container (EnemyArea / PlayerArea equivalent)
        GameObject area = new GameObject(label + "Area");
        area.transform.SetParent(parent, false);
        Rect(area, pos, size);

        // Frame background
        Image areaImg = Img(area, new Color(cCard.r, cCard.g, cCard.b, 0f)); // transparent

        // Character sprite (fills frame)
        GameObject sprGo = new GameObject(label + "Sprite");
        sprGo.transform.SetParent(area.transform, false);
        FullScreen(sprGo);
        Image sprImg = Img(sprGo, Color.white, sprite);
        sprImg.preserveAspect = false;

        // Bottom vignette gradient
        GameObject vig = new GameObject("Vignette");
        vig.transform.SetParent(area.transform, false);
        FullScreen(vig);
        Img(vig, new Color(0, 0, 0, 0.35f));

        // Gold corners
        void Corner(string n, float ancX, float ancY)
        {
            Vector2 a = new Vector2(ancX, ancY);
            float ox = ancX == 0 ? 6 : -6; float oy = ancY == 0 ? 6 : -6;
            // Horizontal
            GameObject h = new GameObject(n + "_H"); h.transform.SetParent(area.transform, false);
            RectTransform hrt = h.AddComponent<RectTransform>();
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = a;
            hrt.anchoredPosition = new Vector2(ox, oy); hrt.sizeDelta = new Vector2(22, 2);
            Img(h, new Color(cAccent.r, cAccent.g, cAccent.b, 0.8f));
            // Vertical
            GameObject v = new GameObject(n + "_V"); v.transform.SetParent(area.transform, false);
            RectTransform vrt = v.AddComponent<RectTransform>();
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = a;
            vrt.anchoredPosition = new Vector2(ox, oy); vrt.sizeDelta = new Vector2(2, 22);
            Img(v, new Color(cAccent.r, cAccent.g, cAccent.b, 0.8f));
        }
        Corner("TL", 0, 1); Corner("TR", 1, 1); Corner("BL", 0, 0); Corner("BR", 1, 0);

        // Boss glow outline
        if (isBoss)
        {
            Outline o = area.AddComponent<Outline>();
            Color bossRed; ColorUtility.TryParseHtmlString("#8a1818", out bossRed);
            o.effectColor = bossRed; o.effectDistance = new Vector2(4, -4);
        }

        return area;
    }

    /// <summary>Creates a styled button</summary>
    static Button Btn(Transform parent, string name, string label, Color bg, Color textCol, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name); go.transform.SetParent(parent, false);
        Rect(go, pos, size);
        Img(go, bg);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.highlightedColor = new Color(1, 1, 1, 0.2f); cb.pressedColor = new Color(1, 1, 1, 0.35f);
        btn.colors = cb;

        if (tmpType != null)
        {
            GameObject t = new GameObject("Text"); t.transform.SetParent(go.transform, false);
            FullScreen(t); TMP(t, label, 18, textCol);
        }
        return btn;
    }

    /// <summary>Creates a horizontal HP bar</summary>
    static void HPBar(Transform parent, string label, Vector2 pos, Vector2 size, int current, int max)
    {
        GameObject bg = new GameObject(label + "_HPBg"); bg.transform.SetParent(parent, false);
        Rect(bg, pos, size);
        Img(bg, cCard);
        // Border
        Outline o = bg.AddComponent<Outline>(); o.effectColor = new Color(cBorder.r, cBorder.g, cBorder.b, 1f); o.effectDistance = new Vector2(1, -1);

        float pct = Mathf.Clamp01((float)current / max);
        Color fgCol; ColorUtility.TryParseHtmlString(pct > 0.5f ? "#b91c1c" : pct > 0.25f ? "#c2410c" : "#7f1d1d", out fgCol);

        GameObject fill = new GameObject(label + "_HPFill"); fill.transform.SetParent(bg.transform, false);
        RectTransform frt = fill.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = new Vector2(pct, 1);
        frt.sizeDelta = Vector2.zero;
        Img(fill, fgCol);

        if (tmpType != null)
        {
            // HP label above bar
            GameObject lbl = new GameObject(label + "_HPLabel"); lbl.transform.SetParent(parent, false);
            Rect(lbl, new Vector2(pos.x, pos.y + size.y / 2 + 14), new Vector2(size.x, 20));
            Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
            TMP(lbl, $"{current} / {max}", 13, cMutedFg);
        }
    }

    /// <summary>Creates a small colored dice tile</summary>
    static GameObject DiceTile(Transform parent, string diceName, int val, Color bg, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("Dice_" + diceName); go.transform.SetParent(parent, false);
        Rect(go, pos, size);
        Img(go, bg);
        Outline o = go.AddComponent<Outline>(); o.effectColor = new Color(cBorder.r, cBorder.g, cBorder.b, 1f); o.effectDistance = new Vector2(1, -1);
        go.AddComponent<Button>();
        go.AddComponent<CanvasGroup>();
        if (go.GetComponent<DraggableDice>() == null) go.AddComponent<DraggableDice>();

        if (tmpType != null)
        {
            // Name at top
            GameObject n = new GameObject("Name"); n.transform.SetParent(go.transform, false);
            RectTransform nrt = n.AddComponent<RectTransform>();
            nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1);
            nrt.pivot = new Vector2(0.5f, 1); nrt.anchoredPosition = new Vector2(0, -5); nrt.sizeDelta = new Vector2(0, 20);
            TMP(n, diceName.ToUpper(), 9, cMutedFg);
            // Value centered
            Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
            GameObject v = new GameObject("Val"); v.transform.SetParent(go.transform, false);
            Rect(v, new Vector2(0, -8), new Vector2(size.x - 10, 50));
            TMP(v, val.ToString(), 36, e4);
        }
        return go;
    }

    // ─── MAIN MENU ────────────────────────────────────────────────────────────

    static void RebuildMainMenu()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        GameObject cv = Canvas(s.name, "Canvas");
        EnsureCamera();
        FixES();

        // Background
        GameObject bg = new GameObject("Background"); bg.transform.SetParent(cv.transform, false); FullScreen(bg);
        Img(bg, Color.white, LoadSprite("Assets/Sprites/Backgrounds 1/bg_dugeon_state_1.jpg"));

        // Overlay
        GameObject ov = new GameObject("Overlay"); ov.transform.SetParent(cv.transform, false); FullScreen(ov);
        Img(ov, new Color(cBg.r, cBg.g, cBg.b, 0.68f));

        // Center group
        GameObject center = new GameObject("Center"); center.transform.SetParent(cv.transform, false);
        Rect(center, new Vector2(0, 40), new Vector2(560, 720));

        // Gold ornament lines
        GameObject ornTop = new GameObject("OrnTop"); ornTop.transform.SetParent(center.transform, false);
        Rect(ornTop, new Vector2(0, 310), new Vector2(220, 1));
        Img(ornTop, new Color(cAccent.r, cAccent.g, cAccent.b, 0.5f));

        // Title DICE
        Color titleColor; ColorUtility.TryParseHtmlString("#e4ddd0", out titleColor);
        GameObject t1 = new GameObject("Title1"); t1.transform.SetParent(center.transform, false);
        Rect(t1, new Vector2(0, 230), new Vector2(560, 110));
        TMP(t1, "DICE", 96, titleColor);

        // Title & DESTINY
        GameObject t2 = new GameObject("Title2"); t2.transform.SetParent(center.transform, false);
        Rect(t2, new Vector2(0, 130), new Vector2(560, 100));
        TMP(t2, "& DESTINY", 84, titleColor);

        // Gold ornament line bottom
        GameObject ornBot = new GameObject("OrnBot"); ornBot.transform.SetParent(center.transform, false);
        Rect(ornBot, new Vector2(0, 75), new Vector2(220, 1));
        Img(ornBot, new Color(cAccent.r, cAccent.g, cAccent.b, 0.5f));

        // Subtitle
        GameObject sub = new GameObject("Subtitle"); sub.transform.SetParent(center.transform, false);
        Rect(sub, new Vector2(0, 40), new Vector2(560, 30));
        TMP(sub, "Roll your fate — Forge your legend", 18, cMutedFg);

        // ─ Buttons ─
        // Start Run (RED)
        Button startBtn = Btn(center.transform, "StartRunButton", "START NEW RUN",
            cPrimary, cPrimaryFg, new Vector2(0, -60), new Vector2(300, 64));

        // Continue (dark outline)
        Btn(center.transform, "ContinueButton", "CONTINUE",
            new Color(0, 0, 0, 0), cMutedFg, new Vector2(0, -145), new Vector2(300, 54));

        // Codex (dark outline)
        Btn(center.transform, "CodexButton", "CODEX",
            new Color(0, 0, 0, 0), cMutedFg, new Vector2(0, -215), new Vector2(300, 54));

        // Border on buttons via outline
        foreach (var btnName in new[] { "StartRunButton", "ContinueButton", "CodexButton" })
        {
            GameObject btnGo = center.transform.Find(btnName)?.gameObject;
            if (btnGo)
            {
                Outline o = btnGo.AddComponent<Outline>();
                o.effectColor = btnName == "StartRunButton"
                    ? new Color(0.76f, 0.19f, 0.19f, 1f)
                    : new Color(cBorder.r, cBorder.g, cBorder.b, 0.7f);
                o.effectDistance = new Vector2(1, -1);
            }
        }

        // Version
        if (tmpType != null)
        {
            GameObject ver = new GameObject("Version"); ver.transform.SetParent(center.transform, false);
            Rect(ver, new Vector2(0, -295), new Vector2(300, 20));
            TMP(ver, "v0.4.2 — Early Access", 11, new Color(cBorder.r, cBorder.g, cBorder.b, 1f));
        }

        // GameManager + MainMenuController
        if (Object.FindObjectOfType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();
        if (Object.FindObjectOfType<MainMenuController>() == null)
        {
            GameObject mmcGo = new GameObject("MainMenuController");
            var mmc = mmcGo.AddComponent<MainMenuController>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, mmc.OnStartRunClicked);
        }

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("MainMenu rebuilt!");
    }

    // ─── COMBAT SCENE ─────────────────────────────────────────────────────────

    static void RebuildCombat()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");
        GameObject cv = Canvas(s.name, "MainCanvas");
        EnsureCamera();
        FixES();

        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
        Color redTxt; ColorUtility.TryParseHtmlString("#f87171", out redTxt);

        // 1. Background
        GameObject bgGo = new GameObject("Background"); bgGo.transform.SetParent(cv.transform, false); FullScreen(bgGo);
        Img(bgGo, Color.white, LoadSprite("Assets/Sprites/Backgrounds 1/bg_dugeon_state_1.jpg"));

        // 2. Dark overlay (65%)
        GameObject dkOv = new GameObject("DarkOverlay"); dkOv.transform.SetParent(cv.transform, false); FullScreen(dkOv);
        Img(dkOv, new Color(cCard.r, cCard.g, cCard.b, 0.65f));

        // 3. Bottom fade
        GameObject btmFade = new GameObject("BottomFade"); btmFade.transform.SetParent(cv.transform, false);
        RectTransform bfRt = btmFade.AddComponent<RectTransform>();
        bfRt.anchorMin = Vector2.zero; bfRt.anchorMax = new Vector2(1, 0);
        bfRt.pivot = new Vector2(0.5f, 0); bfRt.anchoredPosition = Vector2.zero; bfRt.sizeDelta = new Vector2(0, 210);
        Img(btmFade, new Color(0, 0, 0, 0.85f));

        // 4. Turn indicator
        GameObject turnBadge = new GameObject("TurnBadge"); turnBadge.transform.SetParent(cv.transform, false);
        Rect(turnBadge, new Vector2(0, -30), new Vector2(320, 38), 0.5f, 1f);
        Img(turnBadge, new Color(cAccent.r, cAccent.g, cAccent.b, 0.07f));
        if (tmpType != null) { GameObject tt = new GameObject("Text"); tt.transform.SetParent(turnBadge.transform, false); FullScreen(tt); TMP(tt, "Your Turn — Round 1", 16, cAccent); }

        // ─── HERO SIDE (left x= -460) ───

        // HP Bar
        HPBar(cv.transform, "Hero", new Vector2(-460, 200), new Vector2(230, 10), 64, 70);

        // Portrait
        Sprite heroSpr = LoadSprite("Assets/Sprites/Characters 1/player.png", "Assets/Sprites/Characters 1/hero.jpg", "Assets/Sprites/Characters 1/hero.png");
        if (heroSpr == null)
        {
            var gs = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Sprites/Characters 1" });
            if (gs.Length > 0) heroSpr = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(gs[0]));
        }
        GameObject playerArea = Portrait(cv.transform, "Player", heroSpr, new Vector2(-460, 10), new Vector2(200, 270));

        // Player label
        if (tmpType != null) { GameObject pl = new GameObject("HeroLabel"); pl.transform.SetParent(cv.transform, false); Rect(pl, new Vector2(-460, -130), new Vector2(220, 28)); TMP(pl, "HERO", 18, cAccent); }

        // ─── VS Divider ───
        if (tmpType != null) { GameObject vsGo = new GameObject("VS"); vsGo.transform.SetParent(cv.transform, false); Rect(vsGo, new Vector2(0, 10), new Vector2(60, 40)); TMP(vsGo, "VS", 24, new Color(cAccent.r, cAccent.g, cAccent.b, 0.4f)); }
        GameObject vsLine = new GameObject("VSLine"); vsLine.transform.SetParent(cv.transform, false);
        Rect(vsLine, new Vector2(0, 10), new Vector2(1, 180));
        Img(vsLine, new Color(cAccent.r, cAccent.g, cAccent.b, 0.2f));

        // ─── ENEMY SIDE (right x= +460) ───

        // Intent badge
        GameObject intentBadge = new GameObject("IntentBadge"); intentBadge.transform.SetParent(cv.transform, false);
        Rect(intentBadge, new Vector2(460, 225), new Vector2(180, 30));
        Color ib; ColorUtility.TryParseHtmlString("#1a0a0a", out ib); Img(intentBadge, ib);
        Outline ibo = intentBadge.AddComponent<Outline>(); Color ibc; ColorUtility.TryParseHtmlString("#4a1e1e", out ibc); ibo.effectColor = ibc; ibo.effectDistance = new Vector2(1, -1);
        if (tmpType != null) { GameObject it = new GameObject("Text"); it.transform.SetParent(intentBadge.transform, false); FullScreen(it); TMP(it, "ATTACK: 6", 14, redTxt); }

        // HP Bar
        HPBar(cv.transform, "Enemy", new Vector2(460, 195), new Vector2(230, 10), 20, 20);

        // Portrait
        Sprite goblinSpr = LoadSprite("Assets/Sprites/Characters 1/goblin.jpg");
        GameObject enemyArea = Portrait(cv.transform, "Enemy", goblinSpr, new Vector2(460, 10), new Vector2(200, 270));
        BattleEntity be = enemyArea.GetComponent<BattleEntity>() ?? enemyArea.AddComponent<BattleEntity>();

        // Enemy label
        if (tmpType != null) { GameObject el = new GameObject("EnemyLabel"); el.transform.SetParent(cv.transform, false); Rect(el, new Vector2(460, -130), new Vector2(240, 28)); TMP(el, "GOBLIN SCOUT", 18, cAccent); }

        // ─── DICE TRAY (bottom) ───
        GameObject diceTray = new GameObject("DiceTray"); diceTray.transform.SetParent(cv.transform, false);
        RectTransform dtRt = diceTray.AddComponent<RectTransform>();
        dtRt.anchorMin = Vector2.zero; dtRt.anchorMax = new Vector2(1, 0);
        dtRt.pivot = new Vector2(0.5f, 0); dtRt.anchoredPosition = Vector2.zero; dtRt.sizeDelta = new Vector2(0, 205);
        Img(diceTray, new Color(cBg.r, cBg.g, cBg.b, 0.92f));

        // Top gold divider
        GameObject trayTop = new GameObject("TrayTop"); trayTop.transform.SetParent(diceTray.transform, false);
        RectTransform ttRt = trayTop.AddComponent<RectTransform>();
        ttRt.anchorMin = new Vector2(0, 1); ttRt.anchorMax = new Vector2(1, 1);
        ttRt.pivot = new Vector2(0.5f, 1); ttRt.anchoredPosition = Vector2.zero; ttRt.sizeDelta = new Vector2(0, 1);
        Img(trayTop, new Color(cAccent.r, cAccent.g, cAccent.b, 0.35f));

        // "Dice Tray" label
        if (tmpType != null) { GameObject dtLabel = new GameObject("TrayLabel"); dtLabel.transform.SetParent(diceTray.transform, false); Rect(dtLabel, new Vector2(-600, 175), new Vector2(200, 24), 0.5f, 0f); TMP(dtLabel, "DICE TRAY", 13, cMutedFg, false, 513); }

        // 3 Dice
        string[] diceNames = { "Strike", "Guard", "Ignite" };
        Color[] diceColors = {
            new Color(0.32f, 0.05f, 0.05f, 0.85f),
            new Color(0.05f, 0.08f, 0.32f, 0.85f),
            new Color(0.36f, 0.15f, 0.02f, 0.85f)
        };
        int[] diceVals = { 4, 2, 6 };

        for (int i = 0; i < 3; i++)
        {
            float xPos = -300 + i * 150;
            DiceTile(diceTray.transform, diceNames[i], diceVals[i], diceColors[i], new Vector2(xPos, 90), new Vector2(120, 120));
        }

        // Re-roll button
        Button rerollBtn = Btn(diceTray.transform, "RerollButton", "RE-ROLL  (2)",
            new Color(cAccent.r, cAccent.g, cAccent.b, 0.08f), cAccent,
            new Vector2(270, 140), new Vector2(180, 48));
        Outline ro = rerollBtn.gameObject.AddComponent<Outline>(); ro.effectColor = new Color(cAccent.r, cAccent.g, cAccent.b, 0.4f); ro.effectDistance = new Vector2(1, -1);

        // End Turn button
        Button endTurnBtn = Btn(diceTray.transform, "EndTurnButton", "END TURN",
            new Color(cPrimary.r, cPrimary.g, cPrimary.b, 0.15f), redTxt,
            new Vector2(270, 75), new Vector2(180, 48));
        Outline eto = endTurnBtn.gameObject.AddComponent<Outline>(); eto.effectColor = new Color(cPrimary.r, cPrimary.g, cPrimary.b, 0.5f); eto.effectDistance = new Vector2(1, -1);

        // Debug Kill button (small green, top-right corner for testing)
        Button killBtn = Btn(diceTray.transform, "DebugKillBtn", "KILL",
            new Color(0.05f, 0.4f, 0.05f, 0.9f), Color.white,
            new Vector2(480, 75), new Vector2(80, 30));

        // CombatManager
        GameObject cmGo = GameObject.Find("CombatManager") ?? new GameObject("CombatManager");
        CombatController cc = cmGo.GetComponent<CombatController>() ?? cmGo.AddComponent<CombatController>();
        cc.background = bgGo.GetComponent<Image>();
        cc.enemyEntity = be;
        cc.enemySpriteRenderer = enemyArea.transform.Find("EnemySprite")?.GetComponent<Image>();
        if (cc.enemySpriteRenderer == null) cc.enemySpriteRenderer = enemyArea.GetComponentInChildren<Image>();

        // GameManager (placeholder - real one comes from MainMenu via DontDestroyOnLoad)
        if (Object.FindObjectOfType<GameManager>() == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        // Wire kill button
        GameManager gm = Object.FindObjectOfType<GameManager>();
        if (gm != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(killBtn.onClick, gm.OnEnemyDefeated);

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("CombatScene rebuilt!");
    }

    // ─── THE FORGE ────────────────────────────────────────────────────────────

    static void RebuildForge()
    {
        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/TheForge.unity");
        GameObject cv = Canvas(s.name, "Canvas");
        EnsureCamera();
        FixES();

        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);

        // Background
        GameObject bgGo = new GameObject("Background"); bgGo.transform.SetParent(cv.transform, false); FullScreen(bgGo);
        Img(bgGo, cBg);

        // Crimson tint at top
        GameObject tintGo = new GameObject("CrimsonTint"); tintGo.transform.SetParent(cv.transform, false); FullScreen(tintGo);
        Img(tintGo, new Color(cPrimary.r, cPrimary.g, cPrimary.b, 0.04f));

        // ─ Header ─
        if (tmpType != null)
        {
            GameObject hdr = new GameObject("ForgeTitle"); hdr.transform.SetParent(cv.transform, false);
            Rect(hdr, new Vector2(0, -50), new Vector2(400, 48), 0.5f, 1f);
            TMP(hdr, "THE FORGE", 32, cAccent);

            GameObject sub = new GameObject("ForgeSub"); sub.transform.SetParent(cv.transform, false);
            Rect(sub, new Vector2(0, -95), new Vector2(560, 28), 0.5f, 1f);
            TMP(sub, "Select a reward to add to your bag", 16, cMutedFg);
        }

        // ─ Current Dice Section ─
        if (tmpType != null)
        {
            GameObject cdLbl = new GameObject("CurrentDiceLabel"); cdLbl.transform.SetParent(cv.transform, false);
            Rect(cdLbl, new Vector2(-400, 190), new Vector2(300, 26), 0, 0.5f);
            TMP(cdLbl, "YOUR CURRENT DICE", 15, cMutedFg, false, 513);
        }

        GameObject cdContainer = new GameObject("CurrentDiceContainer"); cdContainer.transform.SetParent(cv.transform, false);
        Rect(cdContainer, new Vector2(0, 80), new Vector2(860, 126));
        Img(cdContainer, new Color(cCard.r, cCard.g, cCard.b, 0.85f));
        Outline cdO = cdContainer.AddComponent<Outline>(); cdO.effectColor = new Color(cBorder.r, cBorder.g, cBorder.b, 1f); cdO.effectDistance = new Vector2(1, -1);
        HorizontalLayoutGroup cdHlg = cdContainer.AddComponent<HorizontalLayoutGroup>();
        cdHlg.childAlignment = TextAnchor.MiddleCenter; cdHlg.spacing = 14;
        cdHlg.padding = new RectOffset(20, 20, 12, 12);
        cdHlg.childForceExpandWidth = cdHlg.childForceExpandHeight = false;

        string[] curNames = { "Strike", "Guard", "Ignite", "Pierce", "Curse", "Bless" };
        Color[] curColors = {
            new Color(0.3f,0.05f,0.05f,0.85f), new Color(0.05f,0.09f,0.3f,0.85f),
            new Color(0.36f,0.15f,0.02f,0.85f), new Color(0.02f,0.26f,0.1f,0.85f),
            new Color(0.2f,0.02f,0.26f,0.85f), new Color(0.3f,0.25f,0.02f,0.85f)
        };
        int[] curVals = { 4, 2, 6, 3, 5, 1 };

        for (int i = 0; i < 6; i++)
        {
            GameObject d = new GameObject("CurDice_" + i); d.transform.SetParent(cdContainer.transform, false);
            LayoutElement le = d.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 96;
            Img(d, curColors[i]);
            d.AddComponent<Button>();
            Outline dO = d.AddComponent<Outline>(); dO.effectColor = new Color(cBorder.r, cBorder.g, cBorder.b, 1f); dO.effectDistance = new Vector2(1, -1);

            if (tmpType != null)
            {
                GameObject v = new GameObject("Val"); v.transform.SetParent(d.transform, false);
                Rect(v, new Vector2(0, -8), new Vector2(80, 50)); TMP(v, curVals[i].ToString(), 32, e4);
                GameObject n = new GameObject("Name"); n.transform.SetParent(d.transform, false);
                RectTransform nrt = n.AddComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1); nrt.pivot = new Vector2(0.5f, 1);
                nrt.anchoredPosition = new Vector2(0, -5); nrt.sizeDelta = new Vector2(0, 18);
                TMP(n, curNames[i].ToUpper(), 9, cMutedFg);
            }
        }

        // ─ Gold Divider ─
        GameObject div = new GameObject("GoldDivider"); div.transform.SetParent(cv.transform, false);
        Rect(div, new Vector2(0, -30), new Vector2(700, 1));
        Img(div, new Color(cAccent.r, cAccent.g, cAccent.b, 0.35f));

        // ─ Reward Section ─
        if (tmpType != null)
        {
            GameObject rwLbl = new GameObject("RewardLabel"); rwLbl.transform.SetParent(cv.transform, false);
            Rect(rwLbl, new Vector2(0, -65), new Vector2(360, 30));
            TMP(rwLbl, "CHOOSE A REWARD", 20, cAccent);
        }

        GameObject rwContainer = new GameObject("RewardsContainer"); rwContainer.transform.SetParent(cv.transform, false);
        Rect(rwContainer, new Vector2(0, -195), new Vector2(700, 175));
        HorizontalLayoutGroup rwHlg = rwContainer.AddComponent<HorizontalLayoutGroup>();
        rwHlg.childAlignment = TextAnchor.MiddleCenter; rwHlg.spacing = 48;
        rwHlg.childForceExpandWidth = rwHlg.childForceExpandHeight = false;

        string[] rwNames = { "Cleave", "Inferno", "Doom" };
        Color[] rwColors = {
            new Color(0.32f, 0.05f, 0.05f, 0.9f),
            new Color(0.37f, 0.16f, 0.02f, 0.9f),
            new Color(0.22f, 0.02f, 0.27f, 0.9f)
        };
        int[] rwVals = { 7, 9, 8 };

        for (int i = 0; i < 3; i++)
        {
            GameObject rd = new GameObject("Reward_" + rwNames[i]); rd.transform.SetParent(rwContainer.transform, false);
            LayoutElement le = rd.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 150;
            Img(rd, rwColors[i]);
            Outline ro = rd.AddComponent<Outline>(); ro.effectColor = new Color(cAccent.r, cAccent.g, cAccent.b, 0.55f); ro.effectDistance = new Vector2(2, -2);
            rd.AddComponent<Button>();

            if (tmpType != null)
            {
                GameObject v = new GameObject("Val"); v.transform.SetParent(rd.transform, false);
                Rect(v, new Vector2(0, -10), new Vector2(130, 80)); TMP(v, rwVals[i].ToString(), 60, e4);
                GameObject n = new GameObject("Name"); n.transform.SetParent(rd.transform, false);
                RectTransform nrt = n.AddComponent<RectTransform>();
                nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1); nrt.pivot = new Vector2(0.5f, 1);
                nrt.anchoredPosition = new Vector2(0, -6); nrt.sizeDelta = new Vector2(0, 22);
                TMP(n, rwNames[i].ToUpper(), 11, cMutedFg);
            }
        }

        // ─ Confirm Button ─
        GameObject confirmGo = new GameObject("ConfirmButton"); confirmGo.transform.SetParent(cv.transform, false);
        Rect(confirmGo, new Vector2(0, -350), new Vector2(260, 62));
        Img(confirmGo, new Color(cAccent.r, cAccent.g, cAccent.b, 0.1f));
        Outline co = confirmGo.AddComponent<Outline>(); co.effectColor = new Color(cAccent.r, cAccent.g, cAccent.b, 0.55f); co.effectDistance = new Vector2(1, -1);
        Button confirmBtn = confirmGo.AddComponent<Button>();
        if (tmpType != null) { GameObject ct = new GameObject("Text"); ct.transform.SetParent(confirmGo.transform, false); FullScreen(ct); TMP(ct, "CONFIRM & NEXT", 18, cAccent); }

        // ForgeController
        GameObject fcGo = new GameObject("ForgeController");
        ForgeController fc = fcGo.AddComponent<ForgeController>();
        fc.rewardsContainer = rwContainer.transform;
        fc.currentDiceContainer = cdContainer.transform;
        fc.nextStageButton = confirmBtn;

        List<DiceFaceData> faces = new List<DiceFaceData>();
        foreach (string guid in AssetDatabase.FindAssets("t:DiceFaceData"))
        {
            DiceFaceData face = AssetDatabase.LoadAssetAtPath<DiceFaceData>(AssetDatabase.GUIDToAssetPath(guid));
            if (face != null) faces.Add(face);
        }
        fc.allPossibleFaces = faces.ToArray();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmBtn.onClick, fc.OnNextStageClicked);

        if (Object.FindObjectOfType<GameManager>() == null) new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("TheForge rebuilt!");
    }
}
