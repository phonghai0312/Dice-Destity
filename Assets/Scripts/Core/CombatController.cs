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
        InitializeCombat();
    }

    private void InitializeCombat()
    {
        if (GameManager.Instance == null) return;

        int stage = GameManager.Instance.currentStageIndex;

        EnemyData[] stages = GameManager.Instance.stageEnemies;
        if (stages == null || stages.Length == 0)
        {
            Debug.LogError("[CombatController] Cannot start combat: no stage enemies are configured on GameManager.");
            return;
        }

        // A configured run is victorious only after the final stage has been cleared.
        if (stage > stages.Length)
        {
            Debug.Log("Game Cleared! YOU WIN!");
            GameManager.Instance.LoadMainMenu();
            return;
        }

        EnemyData data = stages[stage - 1]; // stage is 1-indexed
        if (data == null)
        {
            Debug.LogError($"[CombatController] Enemy data for stage {stage} is not assigned on GameManager.");
            return;
        }

        if (background != null) background.sprite = data.backgroundSprite;
        if (enemySpriteRenderer != null) enemySpriteRenderer.sprite = data.enemySprite;
        if (enemyEntity != null) enemyEntity.Initialize(data);

        // Apply Player HP from GameManager
        if (playerEntity != null)
        {
            playerEntity.maxHP = GameManager.Instance.playerMaxHP;
            // Force reset stats but keep current HP logic if we had it, but for now reset to Max
            // In a full roguelike, we'd sync playerEntity.currentHP = GameManager.Instance.playerCurrentHP;
            playerEntity.Initialize(null); 
        }
    }
}
