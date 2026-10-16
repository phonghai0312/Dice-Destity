using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    public int playerMaxHP = 70;
    public int playerCurrentHP = 70;
    public List<DiceFaceData> currentDiceFaces = new List<DiceFaceData>();

    [Header("Run Progression")]
    public int currentStageIndex = 1; // 1=Goblin, 2=Orc, 3=Minotaur
    public EnemyData[] stageEnemies;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartNewRun()
    {
        // FULL RESET — xóa sạch state cũ
        currentStageIndex = 1;
        playerCurrentHP = playerMaxHP;
        currentDiceFaces.Clear();
        Debug.Log($"[GameManager] StartNewRun → stage={currentStageIndex}, hp={playerCurrentHP}");
        LoadCombatScene();
    }

    public void LoadCombatScene()  => SceneManager.LoadScene("CombatScene");
    public void LoadForgeScene()   => SceneManager.LoadScene("TheForge");
    public void LoadMainMenu()     => SceneManager.LoadScene("MainMenu");

    public void OnEnemyDefeated()
    {
        playerCurrentHP = playerCurrentHP; // Giữ HP hiện tại qua scene
        Debug.Log($"[GameManager] Enemy defeated! Stage {currentStageIndex} done. Loading Forge...");
        LoadForgeScene();
    }
}
