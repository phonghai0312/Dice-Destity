using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class Phase4Setup
{
    [MenuItem("Tools/Setup Phase 4")]
    public static void DoSetup()
    {
        if (EditorApplication.isPlaying) {
            Debug.LogError("Hãy tắt Play mode trước khi chạy Setup!");
            return;
        }

        // 1. Setup CombatScene
        Scene combatScene = EditorSceneManager.OpenScene("Assets/Scenes/CombatScene.unity");
        GameObject combatManager = GameObject.Find("CombatManager");
        if (combatManager == null) combatManager = new GameObject("CombatManager");
        
        CombatController cc = combatManager.GetComponent<CombatController>();
        if (cc == null) cc = combatManager.AddComponent<CombatController>();

        GameObject bg = GameObject.Find("Background");
        if (bg) cc.background = bg.GetComponent<Image>();

        GameObject enemySprite = GameObject.Find("EnemySprite");
        if (enemySprite) cc.enemySpriteRenderer = enemySprite.GetComponent<Image>();

        GameObject enemyArea = GameObject.Find("EnemyArea");
        if (enemyArea) cc.enemyEntity = enemyArea.GetComponent<BattleEntity>();

        GameObject playerArea = GameObject.Find("PlayerArea");
        if (playerArea) cc.playerEntity = playerArea.GetComponent<BattleEntity>();

        // Remove old unused GameManager in CombatScene to prevent conflicts
        GameObject oldGM = GameObject.Find("GameManager");
        if (oldGM != null) Object.DestroyImmediate(oldGM);

        EditorSceneManager.MarkSceneDirty(combatScene);
        EditorSceneManager.SaveScene(combatScene);

        // 2. Setup MainMenu (inject EnemyData into GameManager)
        Scene menuScene = EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        GameObject gmObj = GameObject.Find("GameManager");
        if (gmObj != null)
        {
            GameManager gm = gmObj.GetComponent<GameManager>();
            if (gm != null)
            {
                EnemyData goblin = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage1_Goblin.asset");
                EnemyData orc = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage2_Orc.asset");
                EnemyData mino = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/ScriptableObjects/Stage3_Minotaur.asset");
                
                gm.stageEnemies = new EnemyData[] { goblin, orc, mino };
            }
        }
        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene);

        Debug.Log("PHASE 4 SETUP COMPLETE! Ready to Play full run from MainMenu.");
    }
}
