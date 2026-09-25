using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;

/// <summary>
/// Wires up SimpleCombatManager to all UI elements in CombatScene.
/// Run: Tools/Wire Up Combat Logic
/// </summary>
public class WireCombatLogic
{
    [MenuItem("Tools/Wire Up Combat Logic")]
    public static void DoWire()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play mode first!"); return; }

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");

        // ─── Find or create SimpleCombatManager ───────────────────────────────
        GameObject scmGo = GameObject.Find("SimpleCombatManager");
        if (scmGo == null) scmGo = new GameObject("SimpleCombatManager");
        SimpleCombatManager scm = scmGo.GetComponent<SimpleCombatManager>() ?? scmGo.AddComponent<SimpleCombatManager>();

        // ─── Entities ─────────────────────────────────────────────────────────
        // PlayerArea - find or create BattleEntity
        GameObject playerArea = GameObject.Find("PlayerArea");
        if (playerArea != null)
        {
            BattleEntity pe = playerArea.GetComponent<BattleEntity>() ?? playerArea.AddComponent<BattleEntity>();
            pe.maxHP = 70;
            scm.playerEntity = pe;
        }

        // EnemyArea - find or create BattleEntity
        GameObject enemyArea = GameObject.Find("EnemyArea");
        if (enemyArea != null)
        {
            BattleEntity ee = enemyArea.GetComponent<BattleEntity>() ?? enemyArea.AddComponent<BattleEntity>();
            scm.enemyEntity = ee;
        }

        // ─── HP Bars ──────────────────────────────────────────────────────────
        GameObject pHPFill = GameObject.Find("Hero_HPFill");
        if (pHPFill != null) scm.playerHPFill = pHPFill.GetComponent<Image>();

        GameObject eHPFill = GameObject.Find("Enemy_HPFill");
        if (eHPFill != null) scm.enemyHPFill = eHPFill.GetComponent<Image>();

        // ─── HP Labels ────────────────────────────────────────────────────────
        System.Type t = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        GameObject pLbl = GameObject.Find("Hero_HPLabel");
        if (pLbl != null && t != null) scm.playerHPLabel = pLbl.GetComponent(t);

        GameObject eLbl = GameObject.Find("Enemy_HPLabel");
        if (eLbl != null && t != null) scm.enemyHPLabel = eLbl.GetComponent(t);

        // ─── Enemy Name + Portrait + Background ───────────────────────────────
        GameObject eName = GameObject.Find("EnemyLabel");
        if (eName != null && t != null) scm.enemyNameLabel = eName.GetComponent(t);

        GameObject bgGo = GameObject.Find("Background");
        if (bgGo != null) scm.backgroundImage = bgGo.GetComponent<Image>();

        // EnemySprite is child of EnemyArea named "EnemySprite"
        GameObject eSprGo = GameObject.Find("EnemySprite");
        if (eSprGo != null) scm.enemyPortraitImage = eSprGo.GetComponent<Image>();
        else if (enemyArea != null)
        {
            Image img = enemyArea.GetComponentInChildren<Image>();
            if (img != null) scm.enemyPortraitImage = img;
        }

        // ─── Dice Slots ───────────────────────────────────────────────────────
        scm.diceSlots.Clear();
        string[] diceNames = { "Dice_Strike", "Dice_Guard", "Dice_Ignite" };
        foreach (var dn in diceNames)
        {
            GameObject d = GameObject.Find(dn);
            if (d != null) scm.diceSlots.Add(d);
        }
        // Also try DiceTray children
        if (scm.diceSlots.Count == 0)
        {
            GameObject tray = GameObject.Find("DiceTray");
            if (tray != null)
            {
                foreach (Transform child in tray.transform)
                {
                    if (child.name.StartsWith("Dice_"))
                        scm.diceSlots.Add(child.gameObject);
                }
            }
        }

        // ─── Buttons ──────────────────────────────────────────────────────────
        GameObject endTurnGo = GameObject.Find("EndTurnButton");
        if (endTurnGo != null)
        {
            scm.endTurnButton = endTurnGo.GetComponent<Button>();
            scm.endTurnButton?.onClick.RemoveAllListeners();
        }

        GameObject rerollGo = GameObject.Find("RerollButton");
        if (rerollGo != null)
        {
            scm.rerollButton = rerollGo.GetComponent<Button>();
            scm.rerollButton?.onClick.RemoveAllListeners();
        }

        // Wire Debug Kill button to GameManager.OnEnemyDefeated
        GameObject killGo = GameObject.Find("DebugKillBtn");
        if (killGo != null)
        {
            Button killBtn = killGo.GetComponent<Button>();
            if (killBtn != null)
            {
                killBtn.onClick.RemoveAllListeners();
                GameManager gm = Object.FindObjectOfType<GameManager>();
                if (gm != null)
                    UnityEditor.Events.UnityEventTools.AddPersistentListener(killBtn.onClick, gm.OnEnemyDefeated);
            }
        }

        // Remove old CombatManager if exists
        GameObject oldCM = GameObject.Find("CombatManager");
        if (oldCM != null) Object.DestroyImmediate(oldCM);

        // ─── Save ─────────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log($"Combat Logic Wired! Dice slots: {scm.diceSlots.Count}, " +
                  $"PlayerEntity: {(scm.playerEntity != null ? "OK" : "MISSING")}, " +
                  $"EnemyEntity: {(scm.enemyEntity != null ? "OK" : "MISSING")}, " +
                  $"EndTurn: {(scm.endTurnButton != null ? "OK" : "MISSING")}");
    }
}
