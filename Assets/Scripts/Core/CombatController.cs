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

        // Ensure we don't go out of bounds
        if (GameManager.Instance.stageEnemies == null || stage > GameManager.Instance.stageEnemies.Length)
        {
            Debug.Log("Game Cleared! YOU WIN!");
            GameManager.Instance.LoadMainMenu();
            return;
        }

        EnemyData data = GameManager.Instance.stageEnemies[stage - 1]; // stage is 1-indexed

        if (data != null)
        {
            if (background != null) background.sprite = data.backgroundSprite;
            if (enemySpriteRenderer != null) enemySpriteRenderer.sprite = data.enemySprite;
            if (enemyEntity != null) enemyEntity.Initialize(data);
        }

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
