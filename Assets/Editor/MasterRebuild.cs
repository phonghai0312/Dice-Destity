using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// MasterRebuild — Rebuilds all 3 scenes to match the Figma mockup 100%.
/// Uses proper assets from Assets/Sprites.
/// Run: Tools → Master Rebuild All Scenes
/// </summary>
public class MasterRebuild
{
    // ─── Design tokens ────────────────────────────────────────────────────────
    static Color cBg, cCard, cAccent, cBorder, cMuted;
    static Color cPrimary; // red #a81c1c
    static System.Type TMP;

    // ─── Sprite cache ─────────────────────────────────────────────────────────
    static Sprite sBgStage1, sBgStage2, sBgStage3;
    static Sprite sHero, sGoblin, sOrc, sMino;
    static Sprite sRedBtn, sNoneBtn, sHpBorder;
    static Sprite sFireIcon, sStrikeIcon, sBlockIcon, sGhostIcon, sStarIcon, sSkipIcon;

    [MenuItem("Tools/Master Rebuild All Scenes")]
    public static void RebuildAll()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode first!"); return; }

        // Colors
        ColorUtility.TryParseHtmlString("#0a090d", out cBg);
        ColorUtility.TryParseHtmlString("#13111a", out cCard);
        ColorUtility.TryParseHtmlString("#a81c1c", out cPrimary);
        ColorUtility.TryParseHtmlString("#b8860b", out cAccent);
        ColorUtility.TryParseHtmlString("#2a2535", out cBorder);
        ColorUtility.TryParseHtmlString("#7a7285", out cMuted);

        TMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");

        // Load sprites
        sBgStage1  = Load<Sprite>("Assets/Sprites/Backgrounds 1/bg_dugeon_state_1.jpg");
        sBgStage2  = Load<Sprite>("Assets/Sprites/Backgrounds 1/bg_dungeon_state_2 (1).jpg");
        sBgStage3  = Load<Sprite>("Assets/Sprites/Backgrounds 1/bg_dungeon_state_3.jpg");
        sHero      = Load<Sprite>("Assets/Sprites/Characters 1/hero.jpg");
        sGoblin    = Load<Sprite>("Assets/Sprites/Characters 1/goblin.jpg");
        sOrc       = Load<Sprite>("Assets/Sprites/Characters 1/orc.jpg");
        sMino      = Load<Sprite>("Assets/Sprites/Characters 1/mino.jpg");
        sRedBtn    = Load<Sprite>("Assets/Sprites/UI 1/red_button.png");
        sNoneBtn   = Load<Sprite>("Assets/Sprites/UI 1/none_button.png");
        sHpBorder  = Load<Sprite>("Assets/Sprites/UI 1/hp_border.png");
        sFireIcon  = Load<Sprite>("Assets/Sprites/UI 1/fire.png");
        sStrikeIcon= Load<Sprite>("Assets/Sprites/UI 1/strike.png");
        sBlockIcon = Load<Sprite>("Assets/Sprites/UI 1/block.png");
        sGhostIcon = Load<Sprite>("Assets/Sprites/UI 1/ghost.png");
        sStarIcon  = Load<Sprite>("Assets/Sprites/UI 1/star.png");
        sSkipIcon  = Load<Sprite>("Assets/Sprites/UI 1/skip.png");

        BuildMainMenu();
        BuildCombat();
        BuildForge();

        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("=== MASTER REBUILD COMPLETE ===");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    // Create child go, parent to parent, return RectTransform
    static RectTransform Child(Transform parent, string name, out GameObject go)
    {
        go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform as RectTransform ?? go.AddComponent<RectTransform>();
    }

    static RectTransform FullFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static RectTransform Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
    }

    static RectTransform Anchored(RectTransform rt, Vector2 anc, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anc;
        rt.anchoredPosition = pos; rt.sizeDelta = size; return rt;
    }

    static Image Img(GameObject go, Color col, Sprite spr = null, Image.Type imgType = Image.Type.Simple)
    {
        Image i = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        i.color = col; if (spr != null) { i.sprite = spr; i.type = imgType; } return i;
    }

    static Component Text(GameObject go, string text, float size, Color col, bool wrap = false, int align = 514)
    {
        if (TMP == null) return null;
        Component c = go.GetComponent(TMP) ?? go.AddComponent(TMP);
        TMP.GetProperty("text")?.SetValue(c, text);
        TMP.GetProperty("fontSize")?.SetValue(c, size);
        TMP.GetProperty("color")?.SetValue(c, col);
        TMP.GetProperty("alignment")?.SetValue(c, align);
        TMP.GetProperty("enableWordWrapping")?.SetValue(c, wrap);
        return c;
    }

    static Button MakeButton(Transform parent, string name, string label, Sprite bgSprite, Color textCol,
        Vector2 pos, Vector2 size, float fontSize = 18)
    {
        GameObject go; RectTransform rt = Child(parent, name, out go);
        Center(rt, pos, size);
        Img(go, Color.white, bgSprite, Image.Type.Sliced);
        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors; cb.highlightedColor = new Color(1,1,1,0.85f); btn.colors = cb;

        if (TMP != null)
        {
            GameObject tgo; RectTransform trt = Child(go.transform, "Text", out tgo);
            FullFill(trt); Text(tgo, label, fontSize, textCol);
        }
        return btn;
    }

    static void EnsureCamera()
    {
        if (Object.FindObjectOfType<Camera>() != null) return;
        GameObject camGo = new GameObject("Main Camera"); camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black; cam.depth = -1;
        camGo.AddComponent<AudioListener>();
    }

    static void FixEventSystem()
    {
        GameObject esObj = GameObject.Find("EventSystem");
        if (esObj == null) { esObj = new GameObject("EventSystem"); esObj.AddComponent<UnityEngine.EventSystems.EventSystem>(); }
        foreach (var m in esObj.GetComponents<UnityEngine.EventSystems.BaseInputModule>()) Object.DestroyImmediate(m);
        System.Type t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) { var mod = esObj.AddComponent(t); t.GetMethod("AssignDefaultActions")?.Invoke(mod, null); }
        else esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    static GameObject GetOrCreateCanvas(string canvasName)
    {
        GameObject c = GameObject.Find(canvasName);
        if (c == null) c = new GameObject(canvasName);
        for (int i = c.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(c.transform.GetChild(i).gameObject);
        Canvas cv = c.GetComponent<Canvas>() ?? c.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = c.GetComponent<CanvasScaler>() ?? c.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        if (!c.GetComponent<GraphicRaycaster>()) c.AddComponent<GraphicRaycaster>();
        return c;
    }

    /// <summary>Portrait frame: character sprite + 4 gold corners</summary>
    static GameObject Portrait(Transform parent, string id, Sprite spr, Vector2 pos, Vector2 size, bool isBoss = false)
    {
        GameObject area; RectTransform areaRt = Child(parent, id + "Area", out area);
        Center(areaRt, pos, size);
        Img(area, new Color(0,0,0,0)); // transparent container

        // Character sprite - fills frame
        GameObject sprGo; RectTransform sprRt = Child(area.transform, id + "Sprite", out sprGo);
        FullFill(sprRt);
        Image sprImg = Img(sprGo, Color.white, spr);
        sprImg.preserveAspect = false;

        // Bottom vignette
        GameObject vig; FullFill(Child(area.transform, "Vignette", out vig));
        Img(vig, new Color(0,0,0, isBoss ? 0.2f : 0.3f));

        // Gold border/outline
        if (isBoss)
        {
            Outline ob = area.AddComponent<Outline>();
            Color bc; ColorUtility.TryParseHtmlString("#8a1818", out bc);
            ob.effectColor = bc; ob.effectDistance = new Vector2(4,-4);
        }

        // Corner ornaments (L-shaped)
        void Corner(string n, float ax, float ay)
        {
            Vector2 a = new Vector2(ax, ay);
            float ox = ax == 0 ? 5 : -5; float oy = ay == 0 ? 5 : -5;
            GameObject h; RectTransform hrt = Child(area.transform, n+"_H", out h);
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = a;
            hrt.anchoredPosition = new Vector2(ox, oy); hrt.sizeDelta = new Vector2(20, 2);
            Img(h, new Color(cAccent.r, cAccent.g, cAccent.b, 0.9f));
            GameObject v; RectTransform vrt = Child(area.transform, n+"_V", out v);
            vrt.anchorMin = vrt.anchorMax = vrt.pivot = a;
            vrt.anchoredPosition = new Vector2(ox, oy); vrt.sizeDelta = new Vector2(2, 20);
            Img(v, new Color(cAccent.r, cAccent.g, cAccent.b, 0.9f));
        }
        Corner("TL",0,1); Corner("TR",1,1); Corner("BL",0,0); Corner("BR",1,0);

        return area;
    }

    /// <summary>Dice tile with icon + name + value</summary>
    static GameObject DiceTile(Transform parent, string id, string label, int val,
        Color bg, Sprite icon, Vector2 pos, Vector2 size)
    {
        GameObject go; RectTransform rt = Child(parent, "Dice_" + id, out go);
        Center(rt, pos, size);
        Img(go, bg);
        Outline ol = go.AddComponent<Outline>();
        ol.effectColor = new Color(cBorder.r, cBorder.g, cBorder.b, 0.8f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        go.AddComponent<Button>();
        go.AddComponent<CanvasGroup>();

        Color labelCol; ColorUtility.TryParseHtmlString("#9a8a75", out labelCol);
        Color valCol; ColorUtility.TryParseHtmlString("#e4ddd0", out valCol);

        // Name at top
        if (TMP != null)
        {
            GameObject n; RectTransform nrt = Child(go.transform, "DiceName", out n);
            nrt.anchorMin = new Vector2(0,1); nrt.anchorMax = new Vector2(1,1);
            nrt.pivot = new Vector2(0.5f,1); nrt.anchoredPosition = new Vector2(0,-5);
            nrt.sizeDelta = new Vector2(0, size.y * 0.22f);
            Text(n, label, size.y * 0.13f, labelCol);
        }

        // Icon in middle
        if (icon != null)
        {
            GameObject ico; RectTransform irt = Child(go.transform, "DiceIcon", out ico);
            float iconSz = size.y * 0.35f;
            Center(irt, new Vector2(0, size.y * 0.06f), new Vector2(iconSz, iconSz));
            Img(ico, Color.white, icon);
        }

        // Value at bottom
        if (TMP != null)
        {
            GameObject v; RectTransform vrt = Child(go.transform, "DiceVal", out v);
            vrt.anchorMin = new Vector2(0,0); vrt.anchorMax = new Vector2(1,0);
            vrt.pivot = new Vector2(0.5f,0); vrt.anchoredPosition = new Vector2(0, size.y * 0.05f);
            vrt.sizeDelta = new Vector2(0, size.y * 0.45f);
            Text(v, val.ToString(), size.y * 0.38f, valCol);
        }

        return go;
    }

    /// <summary>HP bar: background + fill + label</summary>
    static (Image fill, Component label) HPBar(Transform parent, string id, Vector2 pos, Vector2 barSize, int hp, int maxHp)
    {
        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
        Color hpFillCol; ColorUtility.TryParseHtmlString("#c0392b", out hpFillCol);

        // Label above bar
        Component lbl = null;
        if (TMP != null)
        {
            GameObject lblGo; RectTransform lblRt = Child(parent, id + "_HPLabel", out lblGo);
            Anchored(lblRt, new Vector2(0,1), new Vector2(pos.x + barSize.x/2, pos.y + 16), new Vector2(barSize.x, 18));
            lbl = Text(lblGo, $"{hp} / {maxHp}", 13, new Color(e4.r,e4.g,e4.b,0.8f));

            // "HP" tiny label
            GameObject hpLbl; RectTransform hpLblRt = Child(parent, id + "_HP_Tag", out hpLbl);
            Anchored(hpLblRt, new Vector2(0,1), new Vector2(pos.x, pos.y + 16), new Vector2(30, 18));
            Text(hpLbl, "HP", 11, cMuted, false, 513); // left align
        }

        // Bar background
        GameObject bg; RectTransform bgRt = Child(parent, id + "_HPBg", out bg);
        Anchored(bgRt, new Vector2(0,1), pos, barSize);
        Image bgImg = Img(bg, cCard, sHpBorder, Image.Type.Sliced);
        bgImg.color = new Color(0.08f, 0.05f, 0.05f, 1f);

        // Fill
        GameObject fill; RectTransform fillRt = Child(bg.transform, id + "_HPFill", out fill);
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = new Vector2(Mathf.Clamp01((float)hp/maxHp), 1f);
        fillRt.sizeDelta = new Vector2(-4, -4);
        fillRt.anchoredPosition = new Vector2(2, 0);
        Image fillImg = Img(fill, hpFillCol);

        return (fillImg, lbl);
    }

    // ─── MAIN MENU ────────────────────────────────────────────────────────────

    static void BuildMainMenu()
    {
        UnityEngine.SceneManagement.Scene s = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        GameObject cv = GetOrCreateCanvas("Canvas");
        EnsureCamera(); FixEventSystem();

        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
        Color mutedText; ColorUtility.TryParseHtmlString("#7a7285", out mutedText);

        // 1. Background
        GameObject bg; FullFill(Child(cv.transform, "Background", out bg));
        Img(bg, Color.white, sBgStage1);

        // 2. Dark overlay (65%)
        GameObject ov; FullFill(Child(cv.transform, "Overlay", out ov));
        Img(ov, new Color(0.04f, 0.035f, 0.05f, 0.72f));

        // 3. Center group
        GameObject center; RectTransform cRt = Child(cv.transform, "Center", out center);
        Center(cRt, new Vector2(0, 30), new Vector2(640, 760));

        // 4. Logo image (DICE & DESTINY.png)
        Sprite logoSpr = Load<Sprite>("Assets/Sprites/UI 1/DICE & DESTINY.png");
        if (logoSpr != null)
        {
            GameObject logo; RectTransform lrt = Child(center.transform, "Logo", out logo);
            Center(lrt, new Vector2(0, 250), new Vector2(620, 200));
            Img(logo, Color.white, logoSpr);
        }
        else if (TMP != null) // fallback text
        {
            GameObject t1; Center(Child(center.transform, "Title1", out t1), new Vector2(0, 230), new Vector2(620, 110));
            Text(t1, "DICE", 92, e4);
            GameObject t2; Center(Child(center.transform, "Title2", out t2), new Vector2(0, 130), new Vector2(620, 100));
            Text(t2, "& DESTINY", 82, e4);
        }

        // 5. Subtitle image
        Sprite subSpr = Load<Sprite>("Assets/Sprites/UI 1/Roll your fate — Forge your legend.png");
        if (subSpr != null)
        {
            GameObject sub; RectTransform srt = Child(center.transform, "Subtitle", out sub);
            Center(srt, new Vector2(0, 60), new Vector2(500, 40));
            Img(sub, Color.white, subSpr);
        }
        else if (TMP != null)
        {
            GameObject sub; Center(Child(center.transform, "Subtitle", out sub), new Vector2(0, 55), new Vector2(560, 32));
            Text(sub, "ROLL YOUR FATE — FORGE YOUR LEGEND", 16, mutedText);
        }

        // 6. Dice preview (3 small dice)
        Color[] diceColors = {
            new Color(0.38f, 0.05f, 0.05f, 0.9f),
            new Color(0.05f, 0.08f, 0.38f, 0.9f),
            new Color(0.38f, 0.17f, 0.02f, 0.9f)
        };
        string[] diceLabels = { "STRIKE", "GUARD", "IGNITE" };
        int[] diceVals = { 4, 2, 6 };
        Sprite[] diceIcons = { sStrikeIcon, sBlockIcon, sFireIcon };

        for (int i = 0; i < 3; i++)
        {
            float x = -130 + i * 130;
            DiceTile(center.transform, diceLabels[i] + "_Preview", diceLabels[i], diceVals[i],
                diceColors[i], diceIcons[i], new Vector2(x, -20), new Vector2(100, 100));
        }

        // 7. START NEW RUN button (red)
        Button startBtn = MakeButton(center.transform, "StartRunButton", "START NEW RUN",
            sRedBtn, Color.white, new Vector2(0, -130), new Vector2(320, 64), 20);

        // 8. CONTINUE button
        MakeButton(center.transform, "ContinueButton", "CONTINUE",
            sNoneBtn, new Color(e4.r,e4.g,e4.b,0.7f), new Vector2(0, -210), new Vector2(320, 54), 17);

        // 9. CODEX button
        MakeButton(center.transform, "CodexButton", "CODEX",
            sNoneBtn, new Color(e4.r,e4.g,e4.b,0.7f), new Vector2(0, -278), new Vector2(320, 54), 17);

        // 10. Version tag
        if (TMP != null)
        {
            GameObject ver; Center(Child(center.transform, "Version", out ver), new Vector2(0, -350), new Vector2(320, 20));
            Text(ver, "V0.4.2 — EARLY ACCESS", 11, new Color(cBorder.r,cBorder.g,cBorder.b,1f));
        }

        // GameManager + MainMenuController
        if (!Object.FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();
        MainMenuController mmc = Object.FindObjectOfType<MainMenuController>();
        if (mmc == null)
        {
            mmc = new GameObject("MainMenuController").AddComponent<MainMenuController>();
        }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(startBtn.onClick, mmc.OnStartRunClicked);

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("MainMenu built!");
    }

    // ─── COMBAT ───────────────────────────────────────────────────────────────

    static void BuildCombat()
    {
        UnityEngine.SceneManagement.Scene s = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");
        GameObject cv = GetOrCreateCanvas("MainCanvas");
        EnsureCamera(); FixEventSystem();

        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
        Color redTxt; ColorUtility.TryParseHtmlString("#ef4444", out redTxt);
        Color goldTxt = cAccent;

        // ── Background (changeable at runtime) ──
        GameObject bg; FullFill(Child(cv.transform, "Background", out bg));
        Image bgImg = Img(bg, Color.white, sBgStage1);

        // ── Dark overlay ──
        GameObject dkOv; FullFill(Child(cv.transform, "DarkOverlay", out dkOv));
        Img(dkOv, new Color(cCard.r, cCard.g, cCard.b, 0.6f));

        // ── Bottom fade (for dice tray readability) ──
        GameObject btmFade; var bfRt = Child(cv.transform, "BottomFade", out btmFade);
        bfRt.anchorMin = Vector2.zero; bfRt.anchorMax = new Vector2(1,0);
        bfRt.pivot = new Vector2(0.5f,0); bfRt.anchoredPosition = Vector2.zero; bfRt.sizeDelta = new Vector2(0,220);
        Img(btmFade, new Color(0,0,0,0.88f));

        // ── Turn badge (top center) ──
        GameObject turnBadge; var tbRt = Child(cv.transform, "TurnBadge", out turnBadge);
        Anchored(tbRt, new Vector2(0.5f,1f), new Vector2(0,-20), new Vector2(340,38));
        Img(turnBadge, new Color(cAccent.r,cAccent.g,cAccent.b,0.08f), sNoneBtn, Image.Type.Sliced);
        if (TMP != null)
        {
            GameObject tt; FullFill(Child(turnBadge.transform, "Text", out tt));
            Text(tt, "● YOUR TURN — ROUND 1", 15, cAccent);
        }

        // ── Candles decorative ──
        if (TMP != null)
        {
            GameObject cL; Anchored(Child(cv.transform,"CandleL",out cL), new Vector2(0,1), new Vector2(40,-50), new Vector2(30,50));
            Text(cL, "|", 28, new Color(cAccent.r,cAccent.g,cAccent.b,0.7f));
            GameObject cR; Anchored(Child(cv.transform,"CandleR",out cR), new Vector2(1,1), new Vector2(-40,-50), new Vector2(30,50));
            Text(cR, "|", 28, new Color(cAccent.r,cAccent.g,cAccent.b,0.7f));
        }

        // ─── HERO SIDE (left) ─────────────────────────────────────────────────
        // HP bar
        var (pFill, pLabel) = HPBar(cv.transform, "Hero", new Vector2(60, -100), new Vector2(240, 16), 64, 70);

        // Portrait (matches mockup: darker frame, hero on left ~x=-440)
        GameObject playerArea = Portrait(cv.transform, "Player", sHero, new Vector2(-440, -50), new Vector2(200, 270));
        BattleEntity playerBE = playerArea.GetComponent<BattleEntity>() ?? playerArea.AddComponent<BattleEntity>();
        playerBE.maxHP = 70;

        // Hero name label
        if (TMP != null)
        {
            GameObject pName; var pnRt = Child(cv.transform, "HeroLabel", out pName);
            Anchored(pnRt, new Vector2(0.5f,0.5f), new Vector2(-440,-200), new Vector2(240,30));
            Text(pName, "HERO", 18, cAccent);
        }

        // Hero status tags
        if (TMP != null)
        {
            void StatusTag(string tagName, Color bgCol, string txt, float xOffset)
            {
                GameObject tag; var tagRt = Child(cv.transform, tagName, out tag);
                Anchored(tagRt, new Vector2(0.5f,0.5f), new Vector2(-440+xOffset,-235), new Vector2(80,22));
                Img(tag, bgCol);
                GameObject tagTxt; FullFill(Child(tag.transform,"T",out tagTxt));
                Text(tagTxt, txt, 10, e4);
            }
            Color guardTagBg; ColorUtility.TryParseHtmlString("#1e3a5f",out guardTagBg);
            Color burnTagBg; ColorUtility.TryParseHtmlString("#5c1a0a",out burnTagBg);
            StatusTag("HeroTagGuard", guardTagBg, "GUARD +2", -42);
            StatusTag("HeroTagBurn", burnTagBg, "BURNING", 42);
        }

        // ─── VS ───────────────────────────────────────────────────────────────
        if (TMP != null)
        {
            GameObject vs; Center(Child(cv.transform,"VS",out vs), new Vector2(0,-30), new Vector2(60,36));
            Text(vs, "VS", 22, new Color(cAccent.r,cAccent.g,cAccent.b,0.4f));
        }

        // ─── ENEMY SIDE (right) ───────────────────────────────────────────────
        // Intent badge (above HP bar)
        GameObject intentBadge; var ibRt = Child(cv.transform, "IntentBadge", out intentBadge);
        Anchored(ibRt, new Vector2(0.5f,0.5f), new Vector2(440,-90), new Vector2(160,30));
        Color intentBg; ColorUtility.TryParseHtmlString("#1a0a0a",out intentBg);
        Img(intentBadge, intentBg, sNoneBtn, Image.Type.Sliced);
        if (TMP != null)
        {
            GameObject it; FullFill(Child(intentBadge.transform,"IntentText",out it));
            Text(it, "ATTACK: 6", 14, redTxt);
        }

        // HP bar (enemy)
        var (eFill, eLabel) = HPBar(cv.transform, "Enemy", new Vector2(1920-60-240,-100), new Vector2(240,16), 20, 20);

        // Portrait
        GameObject enemyArea = Portrait(cv.transform, "Enemy", sGoblin, new Vector2(440,-50), new Vector2(200,270));
        BattleEntity enemyBE = enemyArea.GetComponent<BattleEntity>() ?? enemyArea.AddComponent<BattleEntity>();
        enemyBE.maxHP = 20;

        // Enemy name label
        if (TMP != null)
        {
            GameObject eName; var enRt = Child(cv.transform, "EnemyLabel", out eName);
            Anchored(enRt, new Vector2(0.5f,0.5f), new Vector2(440,-200), new Vector2(240,30));
            Text(eName, "GOBLIN SCOUT", 18, cAccent);
        }

        // Enemy status tags
        if (TMP != null)
        {
            GameObject enTag; var etRt = Child(cv.transform,"EnemyTagEnraged",out enTag);
            Anchored(etRt, new Vector2(0.5f,0.5f), new Vector2(440,-235), new Vector2(90,22));
            Color enrageBg; ColorUtility.TryParseHtmlString("#5c1a0a",out enrageBg);
            Img(enTag, enrageBg);
            GameObject etTxt; FullFill(Child(enTag.transform,"T",out etTxt));
            Text(etTxt,"ENRAGED",11,e4);
        }

        // ─── DICE TRAY ────────────────────────────────────────────────────────
        GameObject tray; var trayRt = Child(cv.transform, "DiceTray", out tray);
        trayRt.anchorMin = Vector2.zero; trayRt.anchorMax = new Vector2(1,0);
        trayRt.pivot = new Vector2(0.5f,0); trayRt.anchoredPosition = Vector2.zero;
        trayRt.sizeDelta = new Vector2(0,210);
        Img(tray, new Color(cBg.r,cBg.g,cBg.b,0.94f));

        // Gold top line
        GameObject topLine; var tlRt = Child(tray.transform, "TopLine", out topLine);
        tlRt.anchorMin = new Vector2(0,1); tlRt.anchorMax = new Vector2(1,1);
        tlRt.pivot = new Vector2(0.5f,1); tlRt.anchoredPosition = Vector2.zero; tlRt.sizeDelta = new Vector2(0,1);
        Img(topLine, new Color(cAccent.r,cAccent.g,cAccent.b,0.4f));

        // "DICE TRAY" label
        if (TMP != null)
        {
            GameObject dtLbl; var dtlRt = Child(tray.transform, "DiceTrayLabel", out dtLbl);
            dtlRt.anchorMin = new Vector2(0,1); dtlRt.anchorMax = new Vector2(0,1);
            dtlRt.pivot = new Vector2(0,1); dtlRt.anchoredPosition = new Vector2(20,-12);
            dtlRt.sizeDelta = new Vector2(140,20);
            Text(dtLbl, "DICE TRAY", 12, cMuted, false, 513);
        }

        // 3 Dice
        Color[] trayDiceColors = {
            new Color(0.38f,0.05f,0.05f,0.9f),
            new Color(0.05f,0.08f,0.38f,0.9f),
            new Color(0.38f,0.17f,0.02f,0.9f)
        };
        string[] trayDiceIds = { "Strike","Guard","Ignite" };
        string[] trayDiceLabels = { "STRIKE","GUARD","IGNITE" };
        int[] trayDiceVals = { 4,2,6 };
        Sprite[] trayDiceIcons = { sStrikeIcon, sBlockIcon, sFireIcon };

        for (int i = 0; i < 3; i++)
        {
            float x = -620 + i * 155;
            DiceTile(tray.transform, trayDiceIds[i], trayDiceLabels[i], trayDiceVals[i],
                trayDiceColors[i], trayDiceIcons[i], new Vector2(x, 90), new Vector2(130, 150));
        }

        // RE-ROLL button
        Button rerollBtn = MakeButton(tray.transform, "RerollButton", "RE-ROLL  2",
            sNoneBtn, goldTxt, new Vector2(560, 140), new Vector2(180, 48), 16);

        // END TURN button
        Button endTurnBtn = MakeButton(tray.transform, "EndTurnButton", "END TURN",
            sRedBtn, Color.white, new Vector2(560, 75), new Vector2(180, 52), 17);

        // ─── Game logic scripts ────────────────────────────────────────────────
        // SimpleCombatManager
        GameObject scmGo; // find or create
        scmGo = GameObject.Find("SimpleCombatManager") ?? new GameObject("SimpleCombatManager");
        SimpleCombatManager scm = scmGo.GetComponent<SimpleCombatManager>() ?? scmGo.AddComponent<SimpleCombatManager>();
        scm.playerEntity = playerBE;
        scm.enemyEntity  = enemyBE;
        scm.playerHPFill = pFill;
        scm.enemyHPFill  = eFill;
        scm.playerHPLabel = pLabel;
        scm.enemyHPLabel  = eLabel;
        scm.backgroundImage = bgImg;
        scm.enemyPortraitImage = enemyArea.transform.Find("EnemySprite")?.GetComponent<Image>();
        scm.endTurnButton  = endTurnBtn;
        scm.rerollButton   = rerollBtn;
        scm.diceSlots = new List<GameObject>();
        foreach (var id in trayDiceIds)
        {
            GameObject d = GameObject.Find("Dice_" + id);
            if (d != null) scm.diceSlots.Add(d);
        }

        // Find enemy label for name update
        if (TMP != null)
        {
            GameObject en = GameObject.Find("EnemyLabel");
            if (en != null) scm.enemyNameLabel = en.GetComponent(TMP);
        }

        // GameManager
        if (!Object.FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("CombatScene built!");
    }

    // ─── THE FORGE ────────────────────────────────────────────────────────────

    static void BuildForge()
    {
        UnityEngine.SceneManagement.Scene s = EditorSceneManager.OpenScene("Assets/Scenes/TheForge.unity");
        GameObject cv = GetOrCreateCanvas("Canvas");
        EnsureCamera(); FixEventSystem();

        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);

        // Background
        GameObject bg; FullFill(Child(cv.transform, "Background", out bg));
        Img(bg, cBg);

        // Radial tint
        GameObject tint; FullFill(Child(cv.transform,"CrimsonTint",out tint));
        Img(tint, new Color(cPrimary.r,cPrimary.g,cPrimary.b,0.04f));

        // ── Header ──
        if (TMP != null)
        {
            // Divider lines + "THE FORGE" label
            GameObject hdr; var hrt = Child(cv.transform,"ForgeHeader",out hdr);
            Anchored(hrt, new Vector2(0.5f,1f), new Vector2(0,-55), new Vector2(360,40));
            Text(hdr, "— THE FORGE —", 24, cAccent);

            GameObject sub; var srt = Child(cv.transform,"ForgeSub",out sub);
            Anchored(srt, new Vector2(0.5f,1f), new Vector2(0,-95), new Vector2(520,26));
            Text(sub, "SELECT A REWARD TO ADD TO YOUR BAG", 13, cMuted);
        }

        // ── YOUR CURRENT DICE section ──
        if (TMP != null)
        {
            GameObject cdLbl; var cdlRt = Child(cv.transform,"CurDiceLabel",out cdLbl);
            Anchored(cdlRt, new Vector2(0,1), new Vector2(60,-130), new Vector2(260,24));
            Text(cdLbl,"YOUR CURRENT DICE",14,e4,false,513);

            GameObject countLbl; var clRt = Child(cv.transform,"DiceCount",out countLbl);
            Anchored(clRt, new Vector2(1,1), new Vector2(-60,-130), new Vector2(60,24));
            Text(countLbl,"6 / 10",13,cMuted,false,517); // right align
        }

        // Current dice container
        GameObject cdCont; var cdRt = Child(cv.transform,"CurrentDiceContainer",out cdCont);
        Anchored(cdRt, new Vector2(0.5f,1f), new Vector2(0,-215), new Vector2(900,120));
        Img(cdCont, new Color(cCard.r,cCard.g,cCard.b,0.85f), sNoneBtn, Image.Type.Sliced);

        // Grid layout for 6 current dice
        HorizontalLayoutGroup hlg = cdCont.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter; hlg.spacing = 12;
        hlg.padding = new RectOffset(16,16,10,10);
        hlg.childForceExpandWidth = hlg.childForceExpandHeight = false;

        string[] curIds    = { "Strike","Guard","Ignite","Pierce","Curse","Bless" };
        string[] curLabels = { "STRIKE","GUARD","IGNITE","PIERCE","CURSE","BLESS" };
        int[]    curVals   = { 4,2,6,3,5,1 };
        Sprite[] curIcons  = { sStrikeIcon,sBlockIcon,sFireIcon,sSkipIcon,sGhostIcon,sStarIcon };
        Color[]  curColors = {
            new Color(0.38f,0.05f,0.05f,0.9f), new Color(0.05f,0.08f,0.38f,0.9f),
            new Color(0.38f,0.17f,0.02f,0.9f), new Color(0.02f,0.28f,0.12f,0.9f),
            new Color(0.22f,0.02f,0.28f,0.9f), new Color(0.32f,0.26f,0.02f,0.9f),
        };

        for (int i = 0; i < 6; i++)
        {
            GameObject d = new GameObject("CurDice_"+curIds[i]);
            d.transform.SetParent(cdCont.transform, false);
            LayoutElement le = d.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 94;
            Img(d, curColors[i]);
            Outline dOl = d.AddComponent<Outline>();
            dOl.effectColor = new Color(cBorder.r,cBorder.g,cBorder.b,0.9f); dOl.effectDistance = new Vector2(1,-1);
            d.AddComponent<Button>();

            if (TMP != null)
            {
                // Icon
                if (curIcons[i] != null)
                {
                    GameObject ico; var iRt = Child(d.transform,"Icon",out ico);
                    Center(iRt, new Vector2(0,5), new Vector2(30,30)); Img(ico,Color.white,curIcons[i]);
                }
                // Value
                GameObject v; var vRt = Child(d.transform,"Val",out v);
                vRt.anchorMin = new Vector2(0,0); vRt.anchorMax = new Vector2(1,0);
                vRt.pivot = new Vector2(0.5f,0); vRt.anchoredPosition = new Vector2(0,4); vRt.sizeDelta = new Vector2(0,34);
                Text(v, curVals[i].ToString(), 28, e4);
                // Name
                GameObject n; var nRt = Child(d.transform,"Name",out n);
                nRt.anchorMin = new Vector2(0,1); nRt.anchorMax = new Vector2(1,1);
                nRt.pivot = new Vector2(0.5f,1); nRt.anchoredPosition = new Vector2(0,-4); nRt.sizeDelta = new Vector2(0,16);
                Text(n, curLabels[i], 9, cMuted);
            }
        }

        // Gold divider
        GameObject div; var divRt = Child(cv.transform,"GoldDivider",out div);
        Anchored(divRt, new Vector2(0.5f,1f), new Vector2(0,-295), new Vector2(740,1));
        Img(div, new Color(cAccent.r,cAccent.g,cAccent.b,0.4f));

        // ── CHOOSE A REWARD ──
        if (TMP != null)
        {
            GameObject rwLbl; var rlRt = Child(cv.transform,"RewardLabel",out rwLbl);
            Anchored(rlRt, new Vector2(0,1), new Vector2(60,-318), new Vector2(260,26));
            Text(rwLbl,"CHOOSE A REWARD",16,cAccent,false,513);

            GameObject pickLbl; var plRt = Child(cv.transform,"PickLabel",out pickLbl);
            Anchored(plRt, new Vector2(1,1), new Vector2(-60,-318), new Vector2(80,26));
            Text(pickLbl,"PICK 1",13,new Color(cAccent.r,cAccent.g,cAccent.b,0.6f),false,517);
        }

        // 3 Reward dice (large, centered)
        GameObject rwCont; var rwRt = Child(cv.transform,"RewardsContainer",out rwCont);
        Anchored(rwRt, new Vector2(0.5f,1f), new Vector2(0,-490), new Vector2(700,175));
        HorizontalLayoutGroup rwHlg = rwCont.AddComponent<HorizontalLayoutGroup>();
        rwHlg.childAlignment = TextAnchor.MiddleCenter; rwHlg.spacing = 45;
        rwHlg.childForceExpandWidth = rwHlg.childForceExpandHeight = false;

        string[] rwIds    = { "Cleave","Inferno","Doom" };
        string[] rwLabels = { "CLEAVE","INFERNO","DOOM" };
        int[]    rwVals   = { 7,9,8 };
        Sprite[] rwIcons  = { sStrikeIcon,sFireIcon,sGhostIcon };
        Color[]  rwColors = {
            new Color(0.38f,0.05f,0.05f,0.92f),
            new Color(0.38f,0.17f,0.02f,0.92f),
            new Color(0.22f,0.02f,0.28f,0.92f),
        };
        string[] rarities = { "UNCOMMON","RARE","EPIC" };

        List<Button> rwButtons = new List<Button>();
        for (int i = 0; i < 3; i++)
        {
            GameObject rd = new GameObject("Reward_"+rwIds[i]);
            rd.transform.SetParent(rwCont.transform, false);
            LayoutElement le = rd.AddComponent<LayoutElement>(); le.preferredWidth = le.preferredHeight = 155;
            Img(rd, rwColors[i]);
            Outline rdOl = rd.AddComponent<Outline>();
            rdOl.effectColor = new Color(cAccent.r,cAccent.g,cAccent.b,0.55f); rdOl.effectDistance = new Vector2(2,-2);
            Button rdBtn = rd.AddComponent<Button>(); rwButtons.Add(rdBtn);

            if (TMP != null)
            {
                // Icon
                if (rwIcons[i] != null)
                {
                    GameObject ico; var iRt = Child(rd.transform,"Icon",out ico);
                    Center(iRt, new Vector2(0,10), new Vector2(42,42)); Img(ico,Color.white,rwIcons[i]);
                }
                // Value
                GameObject v; var vRt = Child(rd.transform,"Val",out v);
                vRt.anchorMin = new Vector2(0,0); vRt.anchorMax = new Vector2(1,0);
                vRt.pivot = new Vector2(0.5f,0); vRt.anchoredPosition = new Vector2(0,6); vRt.sizeDelta = new Vector2(0,55);
                Text(v, rwVals[i].ToString(), 52, e4);
                // Name at top
                GameObject n; var nRt = Child(rd.transform,"Name",out n);
                nRt.anchorMin = new Vector2(0,1); nRt.anchorMax = new Vector2(1,1);
                nRt.pivot = new Vector2(0.5f,1); nRt.anchoredPosition = new Vector2(0,-7); nRt.sizeDelta = new Vector2(0,22);
                Text(n, rwLabels[i], 11, cMuted);
            }
        }

        // Rarity + description labels below rewards container
        if (TMP != null)
        {
            string[] descs = { "Deal 7 damage\nto one foe.", "Engulf in holy flame\nfor 9 dmg.", "Curse all enemies\nfor 8 dmg." };
            Color[] rarityColors = { e4, cAccent, new Color(0.78f,0.5f,1f,1f) };
            for (int i = 0; i < 3; i++)
            {
                float xOff = -230 + i * 230;
                GameObject rar; var rarRt = Child(cv.transform,"Rarity_"+i,out rar);
                Anchored(rarRt, new Vector2(0.5f,1f), new Vector2(xOff,-590), new Vector2(180,20));
                Text(rar, rarities[i], 12, rarityColors[i]);

                GameObject desc; var descRt = Child(cv.transform,"Desc_"+i,out desc);
                Anchored(descRt, new Vector2(0.5f,1f), new Vector2(xOff,-630), new Vector2(160,45));
                Text(desc, descs[i], 11, cMuted, true);
            }
        }

        // Confirm button
        GameObject confirmGo; var confRt = Child(cv.transform,"ConfirmButton",out confirmGo);
        Anchored(confRt, new Vector2(0.5f,1f), new Vector2(0,-740), new Vector2(280,60));
        Img(confirmGo, new Color(cAccent.r,cAccent.g,cAccent.b,0.1f), sNoneBtn, Image.Type.Sliced);
        Outline confOl = confirmGo.AddComponent<Outline>();
        confOl.effectColor = new Color(cAccent.r,cAccent.g,cAccent.b,0.5f); confOl.effectDistance = new Vector2(1,-1);
        Button confirmBtn = confirmGo.AddComponent<Button>();
        if (TMP != null)
        {
            GameObject ct; FullFill(Child(confirmGo.transform,"Text",out ct)); Text(ct,"CONFIRM & NEXT",17,cAccent);

            GameObject hint; var hintRt = Child(cv.transform,"ConfirmHint",out hint);
            Anchored(hintRt, new Vector2(0.5f,1f), new Vector2(0,-778), new Vector2(300,18));
            Text(hint,"SELECT A DIE TO CONTINUE",11,cMuted);
        }

        // ForgeController
        GameObject fcGo = new GameObject("ForgeController");
        ForgeController fc = fcGo.AddComponent<ForgeController>();
        fc.rewardsContainer = rwCont.transform;
        fc.currentDiceContainer = cdCont.transform;
        fc.nextStageButton = confirmBtn;

        List<DiceFaceData> faces = new List<DiceFaceData>();
        foreach (string guid in AssetDatabase.FindAssets("t:DiceFaceData"))
        {
            var face = AssetDatabase.LoadAssetAtPath<DiceFaceData>(AssetDatabase.GUIDToAssetPath(guid));
            if (face != null) faces.Add(face);
        }
        fc.allPossibleFaces = faces.ToArray();
        UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmBtn.onClick, fc.OnNextStageClicked);

        if (!Object.FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();

        EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s);
        Debug.Log("TheForge built!");
    }
}
