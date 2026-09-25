using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    public int playerMaxHP = 20;
    public int playerCurrentHP = 20;
    public List<DiceFaceData> currentDiceFaces = new List<DiceFaceData>();

    [Header("Run Progression")]
    public int currentStageIndex = 1; // 1 = Goblin, 2 = Orc, 3 = Minotaur
    public EnemyData[] stageEnemies; // Assign via setup

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    // Cấu trúc tạm thời để chuẩn bị cho Phase 2 & 3 & 4
    public void StartNewRun()
    {
        playerCurrentHP = playerMaxHP;
        currentStageIndex = 1;
        // Khởi tạo các mặt xúc xắc cơ bản ban đầu (Sẽ nạp ở Phase 2)
        
        LoadCombatScene();
    }

    public void LoadCombatScene()
    {
        SceneManager.LoadScene("CombatScene");
    }

    public void LoadForgeScene()
    {
        SceneManager.LoadScene("TheForge");
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void OnEnemyDefeated()
    {
        Debug.Log("Enemy Defeated! Loading Forge...");
        LoadForgeScene();
    }
}
