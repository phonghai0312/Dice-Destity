using UnityEngine;
using UnityEngine.UI;

public class CombatController : MonoBehaviour
{
    public Image background;
    public Image enemySpriteRenderer;
    public BattleEntity enemyEntity;
    public BattleEntity playerEntity;

    private void Start()
    {
        // Nếu SimpleCombatManager đang điều phối CombatScene thì để SimpleCombatManager quản lý khởi tạo
        if (FindAnyObjectByType<SimpleCombatManager>() != null)
        {
            return;
        }

        InitializeCombat();
    }

    public void InitializeCombat()
    {
        if (GameManager.Instance == null) return;

        int stage = GameManager.Instance.currentStageIndex;
        EnemyData[] stages = GameManager.Instance.stageEnemies;
        int totalStages = (stages != null && stages.Length > 0) ? stages.Length : 3;

        if (stage > totalStages)
        {
            Debug.Log("Game Cleared! YOU WIN!");
            GameManager.Instance.LoadMainMenu();
            return;
        }

        EnemyData data = (stages != null && stage - 1 < stages.Length) ? stages[stage - 1] : null;
        if (data != null)
        {
            if (background != null && data.backgroundSprite != null) background.sprite = data.backgroundSprite;
            if (enemySpriteRenderer != null && data.enemySprite != null) enemySpriteRenderer.sprite = data.enemySprite;
        }

        if (enemyEntity != null)
        {
            if (data != null)
            {
                data.GetCanonicalStats(stage, out int resolvedHP, out int resolvedShield);
                enemyEntity.maxHP = resolvedHP;
                enemyEntity.Initialize(data);
                if (resolvedShield > enemyEntity.CurrentShield)
                {
                    enemyEntity.AddShield(resolvedShield - enemyEntity.CurrentShield);
                }
            }
            else
            {
                int[] defaultHps = { 20, 35, 60 };
                enemyEntity.maxHP = stage <= defaultHps.Length ? defaultHps[stage - 1] : 20;
                enemyEntity.Initialize(null);
                if (stage == 2) enemyEntity.AddShield(5);
            }
        }

        if (playerEntity != null)
        {
            playerEntity.maxHP = GameManager.Instance.playerMaxHP;
            playerEntity.Initialize(null);
        }
    }
}
