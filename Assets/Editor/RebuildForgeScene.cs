using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// RebuildForgeScene — Xây lại TheForge với background xưởng rèn + font lớn + animation.
/// Menu: Tools → Rebuild Forge Scene
/// </summary>
public class RebuildForgeScene
{
    static System.Type TMP;
    static Color cBg, cCard, cAccent, cBorder, cMuted, cPrimary;

    [MenuItem("Tools/Rebuild Forge Scene")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode!"); return; }

        TMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        ColorUtility.TryParseHtmlString("#0a090d", out cBg);
        ColorUtility.TryParseHtmlString("#1a1520", out cCard);
        ColorUtility.TryParseHtmlString("#b8860b", out cAccent);
        ColorUtility.TryParseHtmlString("#2a2535", out cBorder);
        ColorUtility.TryParseHtmlString("#9a8a75", out cMuted);
        ColorUtility.TryParseHtmlString("#a81c1c", out cPrimary);

        Scene s = EditorSceneManager.OpenScene("Assets/Scenes/TheForge.unity");
        
        // Canvas
        GameObject cvGo = GameObject.Find("Canvas");
        if (cvGo == null) cvGo = new GameObject("Canvas");
        for (int i = cvGo.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(cvGo.transform.GetChild(i).gameObject);
        
        Canvas cv = cvGo.GetComponent<Canvas>() ?? cvGo.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = cvGo.GetComponent<CanvasScaler>() ?? cvGo.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        if (!cvGo.GetComponent<GraphicRaycaster>()) cvGo.AddComponent<GraphicRaycaster>();

        EnsureCamera();
        FixEventSystem();
        BuildForge(cvGo.transform);

        EditorSceneManager.MarkSceneDirty(s);
        EditorSceneManager.SaveScene(s);
        Debug.Log("=== Forge Scene Rebuilt! ===");
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path);

    static RectTransform Child(Transform parent, string name, out GameObject go)
    {
        go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return (go.transform as RectTransform) ?? go.AddComponent<RectTransform>();
    }

    static RectTransform FullFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero; rt.anchoredPosition = Vector2.zero;
        return rt;
    }

    static RectTransform AnchoredRT(RectTransform rt, Vector2 anc, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anc;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }

    static Image Img(GameObject go, Color col, Sprite spr = null, Image.Type t = Image.Type.Simple)
    {
        Image i = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        i.color = col;
        if (spr != null) { i.sprite = spr; i.type = t; }
        return i;
    }

    static Component Text(GameObject go, string text, float size, Color col, bool wrap = true, int align = 514)
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

    static Button Btn(Transform parent, string name, string label, Sprite bg, Color textCol,
        Vector2 anc, Vector2 pos, Vector2 size, float fontSize = 20)
    {
        GameObject go; RectTransform rt = Child(parent, name, out go);
        AnchoredRT(rt, anc, pos, size);
        Img(go, Color.white, bg, Image.Type.Sliced);
        Button btn = go.AddComponent<Button>();
        if (TMP != null)
        {
            GameObject t; FullFill(Child(go.transform, "Text", out t));
            Text(t, label, fontSize, textCol, false);
        }
        return btn;
    }

    static void EnsureCamera()
    {
        if (Object.FindObjectOfType<Camera>() != null) return;
        GameObject g = new GameObject("Main Camera"); g.tag = "MainCamera";
        Camera c = g.AddComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor; c.backgroundColor = Color.black; c.depth = -1;
        g.AddComponent<AudioListener>();
    }

    static void FixEventSystem()
    {
        GameObject es = GameObject.Find("EventSystem");
        if (es == null) { es = new GameObject("EventSystem"); es.AddComponent<UnityEngine.EventSystems.EventSystem>(); }
        foreach (var m in es.GetComponents<UnityEngine.EventSystems.BaseInputModule>()) Object.DestroyImmediate(m);
        System.Type t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) { var mod = es.AddComponent(t); t.GetMethod("AssignDefaultActions")?.Invoke(mod, null); }
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
    }

    // ─── Main Build ───────────────────────────────────────────────────────────

    static void BuildForge(Transform root)
    {
        Color e4; ColorUtility.TryParseHtmlString("#e4ddd0", out e4);
        Color warmWhite; ColorUtility.TryParseHtmlString("#f5e6c8", out warmWhite);

        Sprite sForge   = Load<Sprite>("Assets/Sprites/Backgrounds 1/bg_forge.jpg");
        Sprite sNoneBtn = Load<Sprite>("Assets/Sprites/UI 1/none_button.png");
        Sprite sRedBtn  = Load<Sprite>("Assets/Sprites/UI 1/red_button.png");
        Sprite sFireIcon= Load<Sprite>("Assets/Sprites/UI 1/fire.png");
        Sprite sStrike  = Load<Sprite>("Assets/Sprites/UI 1/strike.png");
        Sprite sBlock   = Load<Sprite>("Assets/Sprites/UI 1/block.png");
        Sprite sGhost   = Load<Sprite>("Assets/Sprites/UI 1/ghost.png");
        Sprite sStar    = Load<Sprite>("Assets/Sprites/UI 1/star.png");
        Sprite sSkip    = Load<Sprite>("Assets/Sprites/UI 1/skip.png");

        // ── 1. Background ──────────────────────────────────────────────────────
        GameObject bg; FullFill(Child(root, "Background", out bg));
        Img(bg, Color.white, sForge);

        // Overlay tối từ trên xuống để UI dễ đọc
        GameObject ov; FullFill(Child(root, "Overlay", out ov));
        Img(ov, new Color(0.04f, 0.02f, 0.06f, 0.75f));

        // Bottom dark gradient để section dice nổi bật
        GameObject bot; var bRt = Child(root, "BottomPanel", out bot);
        bRt.anchorMin = Vector2.zero; bRt.anchorMax = new Vector2(1, 0.52f);
        bRt.pivot = new Vector2(0.5f, 0f); bRt.anchoredPosition = Vector2.zero; bRt.sizeDelta = Vector2.zero;
        Img(bot, new Color(0.05f, 0.02f, 0.07f, 0.92f));

        // ── 2. Header "THE FORGE" ──────────────────────────────────────────────
        // Glow line trên
        GameObject gl; var glRt = Child(root, "GoldLine", out gl);
        AnchoredRT(glRt, new Vector2(0.5f, 1f), new Vector2(0, -66), new Vector2(600, 1));
        Img(gl, new Color(cAccent.r, cAccent.g, cAccent.b, 0.5f));

        if (TMP != null)
        {
            // "THE FORGE" — to, vàng
            GameObject hdr; var hRt = Child(root, "ForgeTitle", out hdr);
            AnchoredRT(hRt, new Vector2(0.5f, 1f), new Vector2(0, -48), new Vector2(500, 55));
            var hdrTxt = Text(hdr, "THE FORGE", 42, cAccent, false, 514);

            // Subtitle nhỏ hơn
            GameObject sub; var sRt = Child(root, "ForgeSub", out sub);
            AnchoredRT(sRt, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(560, 28));
            Text(sub, "SELECT A REWARD TO ADD TO YOUR BAG", 15, cMuted, false, 514);
        }

        // ── 3. YOUR CURRENT DICE ──────────────────────────────────────────────
        if (TMP != null)
        {
            GameObject cdLbl; var cdlRt = Child(root, "CurDiceTitle", out cdLbl);
            AnchoredRT(cdlRt, new Vector2(0f, 1f), new Vector2(70, -128), new Vector2(300, 28));
            Text(cdLbl, "YOUR CURRENT DICE", 16, e4, false, 513);

            GameObject cntLbl; var cntRt = Child(root, "DiceCount", out cntLbl);
            AnchoredRT(cntRt, new Vector2(1f, 1f), new Vector2(-70, -128), new Vector2(80, 28));
            Text(cntLbl, "6 / 10", 14, cMuted, false, 517);
        }

        // Khung dice hiện tại
        GameObject cdBox; var cdRt = Child(root, "CurrentDiceBox", out cdBox);
        AnchoredRT(cdRt, new Vector2(0.5f, 1f), new Vector2(0, -220), new Vector2(1060, 136));
        Img(cdBox, new Color(0.08f, 0.04f, 0.1f, 0.92f), sNoneBtn, Image.Type.Sliced);
        Outline cdOl = cdBox.AddComponent<Outline>();
        cdOl.effectColor = new Color(cAccent.r, cAccent.g, cAccent.b, 0.25f); cdOl.effectDistance = new Vector2(1, -1);

        // 6 dice hiện tại (smaller)
        string[] ids    = { "Strike", "Guard", "Ignite", "Pierce", "Curse", "Bless" };
        string[] labels = { "STRIKE", "GUARD", "IGNITE", "PIERCE", "CURSE", "BLESS" };
        int[]    vals   = { 4, 2, 6, 3, 5, 1 };
        Sprite[] icons  = { sStrike, sBlock, sFireIcon, sSkip, sGhost, sStar };
        Color[]  cols   = {
            new Color(0.38f,0.05f,0.05f,0.92f),
            new Color(0.05f,0.08f,0.38f,0.92f),
            new Color(0.38f,0.17f,0.02f,0.92f),
            new Color(0.02f,0.28f,0.12f,0.92f),
            new Color(0.22f,0.02f,0.28f,0.92f),
            new Color(0.32f,0.26f,0.02f,0.92f),
        };

        for (int i = 0; i < 6; i++)
        {
            float x = -440 + i * 178;
            GameObject d; var dRt = Child(cdBox.transform, "CurDice_"+ids[i], out d);
            dRt.anchorMin = dRt.anchorMax = dRt.pivot = new Vector2(0.5f, 0.5f);
            dRt.anchoredPosition = new Vector2(x, 0); dRt.sizeDelta = new Vector2(100, 108);
            Img(d, cols[i]);
            Outline dOl = d.AddComponent<Outline>();
            dOl.effectColor = new Color(cBorder.r,cBorder.g,cBorder.b,0.8f); dOl.effectDistance = new Vector2(1,-1);

            // Icon
            if (icons[i] != null)
            {
                GameObject ic; var icRt = Child(d.transform, "Icon", out ic);
                icRt.anchorMin = icRt.anchorMax = icRt.pivot = new Vector2(0.5f, 0.5f);
                icRt.anchoredPosition = new Vector2(0, 8); icRt.sizeDelta = new Vector2(30, 30);
                Img(ic, Color.white, icons[i]);
            }
            // Value
            if (TMP != null)
            {
                GameObject v; var vRt = Child(d.transform, "Val", out v);
                vRt.anchorMin = new Vector2(0,0); vRt.anchorMax = new Vector2(1,0);
                vRt.pivot = new Vector2(0.5f,0); vRt.anchoredPosition = new Vector2(0,6); vRt.sizeDelta = new Vector2(0,40);
                Text(v, vals[i].ToString(), 32, warmWhite, false, 514);
                // Name top
                GameObject n; var nRt = Child(d.transform, "Name", out n);
                nRt.anchorMin = new Vector2(0,1); nRt.anchorMax = new Vector2(1,1);
                nRt.pivot = new Vector2(0.5f,1); nRt.anchoredPosition = new Vector2(0,-4); nRt.sizeDelta = new Vector2(0,18);
                Text(n, labels[i], 10, cMuted, false, 514);
            }

            // ForgeAnimator trôi nổi nhẹ với delay stagger
            // (Sẽ tự thêm vào runtime)
        }

        // Gold divider giữa
        GameObject div; var divRt = Child(root, "Divider", out div);
        AnchoredRT(divRt, new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(900, 1));
        Img(div, new Color(cAccent.r, cAccent.g, cAccent.b, 0.45f));

        // Diamond icon trang trí
        if (TMP != null)
        {
            GameObject dm; var dmRt = Child(root, "DiamondDeco", out dm);
            AnchoredRT(dmRt, new Vector2(0.5f, 1f), new Vector2(0, -310), new Vector2(24, 24));
            Text(dm, "◆", 14, cAccent, false, 514);
        }

        // ── 4. CHOOSE A REWARD ────────────────────────────────────────────────
        if (TMP != null)
        {
            GameObject rwL; var rwLRt = Child(root, "RewardTitle", out rwL);
            AnchoredRT(rwLRt, new Vector2(0f, 1f), new Vector2(70, -336), new Vector2(300, 30));
            Text(rwL, "CHOOSE A REWARD", 18, cAccent, false, 513);

            GameObject pk; var pkRt = Child(root, "PickHint", out pk);
            AnchoredRT(pkRt, new Vector2(1f, 1f), new Vector2(-70, -336), new Vector2(80, 30));
            Text(pk, "PICK 1", 14, new Color(cAccent.r,cAccent.g,cAccent.b,0.6f), false, 517);
        }

        // 3 Reward dice (lớn, có đầy đủ thông tin)
        string[] rwIds    = { "Cleave",  "Inferno",  "Doom"   };
        string[] rwNames  = { "CLEAVE",  "INFERNO",  "DOOM"   };
        int[]    rwVals   = { 7,          9,           8        };
        Sprite[] rwIcons  = { sStrike,   sFireIcon,  sGhost   };
        Color[]  rwColors = {
            new Color(0.38f,0.05f,0.05f,0.95f),
            new Color(0.38f,0.17f,0.02f,0.95f),
            new Color(0.22f,0.02f,0.28f,0.95f),
        };
        string[] rarities = { "UNCOMMON ★★",  "RARE ★★★",     "EPIC ★★★★"  };
        Color[]  rarCol   = { e4, cAccent, new Color(0.78f,0.5f,1f,1f) };
        string[] descs    = {
            "Deal 7 damage\nto one foe.",
            "Engulf in holy\nflame for 9 dmg.",
            "Curse all enemies\nfor 8 dmg."
        };

        List<Button> rwBtns = new List<Button>();
        for (int i = 0; i < 3; i++)
        {
            float x = -290 + i * 290;
            GameObject rw; var rwRt = Child(root, "Reward_"+rwIds[i], out rw);
            AnchoredRT(rwRt, new Vector2(0.5f,1f), new Vector2(x, -530), new Vector2(230, 195));
            Img(rw, rwColors[i]);
            Outline rwOl = rw.AddComponent<Outline>();
            rwOl.effectColor = new Color(cAccent.r,cAccent.g,cAccent.b,0.5f); rwOl.effectDistance = new Vector2(2,-2);
            Button btn = rw.AddComponent<Button>();
            rwBtns.Add(btn);

            // Icon
            if (rwIcons[i] != null)
            {
                GameObject ic; var icRt = Child(rw.transform,"Icon",out ic);
                icRt.anchorMin = icRt.anchorMax = icRt.pivot = new Vector2(0.5f,0.5f);
                icRt.anchoredPosition = new Vector2(0,20); icRt.sizeDelta = new Vector2(56,56);
                Img(ic, Color.white, rwIcons[i]);
            }
            // Value (số lớn nhất)
            if (TMP != null)
            {
                GameObject v; var vRt = Child(rw.transform,"Val",out v);
                vRt.anchorMin = new Vector2(0,0); vRt.anchorMax = new Vector2(1,0);
                vRt.pivot = new Vector2(0.5f,0); vRt.anchoredPosition = new Vector2(0,10); vRt.sizeDelta = new Vector2(0,64);
                Text(v, rwVals[i].ToString(), 60, warmWhite, false, 514);
                // Name
                GameObject n; var nRt = Child(rw.transform,"DiceName",out n);
                nRt.anchorMin = new Vector2(0,1); nRt.anchorMax = new Vector2(1,1);
                nRt.pivot = new Vector2(0.5f,1); nRt.anchoredPosition = new Vector2(0,-8); nRt.sizeDelta = new Vector2(0,22);
                Text(n, rwNames[i], 13, cMuted, false, 514);
            }
        }

        // Rarity + description labels (bên dưới mỗi card)
        if (TMP != null)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = -290 + i * 290;

                GameObject rar; var rarRt = Child(root, "Rarity_"+i, out rar);
                AnchoredRT(rarRt, new Vector2(0.5f,1f), new Vector2(x,-640), new Vector2(240,24));
                Text(rar, rarities[i], 13, rarCol[i], false, 514);

                GameObject desc; var descRt = Child(root, "Desc_"+i, out desc);
                AnchoredRT(descRt, new Vector2(0.5f,1f), new Vector2(x,-685), new Vector2(220,54));
                Text(desc, descs[i], 12, cMuted, true, 514);
            }
        }

        // ── 5. CONFIRM BUTTON ─────────────────────────────────────────────────
        GameObject confGo; var confRt = Child(root, "ConfirmButton", out confGo);
        AnchoredRT(confRt, new Vector2(0.5f, 1f), new Vector2(0, -790), new Vector2(300, 64));
        Img(confGo, new Color(cAccent.r,cAccent.g,cAccent.b,0.12f), sNoneBtn, Image.Type.Sliced);
        Outline confOl = confGo.AddComponent<Outline>();
        confOl.effectColor = new Color(cAccent.r,cAccent.g,cAccent.b,0.55f); confOl.effectDistance = new Vector2(2,-2);
        Button confirmBtn = confGo.AddComponent<Button>();

        if (TMP != null)
        {
            GameObject ct; FullFill(Child(confGo.transform,"Label",out ct));
            Text(ct, "CONFIRM & NEXT", 20, cAccent, false, 514);

            GameObject hint; var hintRt = Child(root,"ConfirmHint",out hint);
            AnchoredRT(hintRt, new Vector2(0.5f,1f), new Vector2(0,-832), new Vector2(360,24));
            Text(hint, "SELECT A REWARD DICE TO CONTINUE", 12, new Color(cMuted.r,cMuted.g,cMuted.b,0.6f), false, 514);
        }

        // GameManager
        if (!Object.FindObjectOfType<GameManager>()) new GameObject("GameManager").AddComponent<GameManager>();

        // ForgeController
        ForgeController fc = Object.FindObjectOfType<ForgeController>();
        if (fc == null)
        {
            GameObject fcGo = new GameObject("ForgeController");
            fc = fcGo.AddComponent<ForgeController>();
        }
        fc.nextStageButton = confirmBtn;
        UnityEditor.Events.UnityEventTools.AddPersistentListener(confirmBtn.onClick, fc.OnNextStageClicked);

        Debug.Log("Forge rebuilt with new background + animations!");
    }
}
