using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static GameManager ResolveInstance()
    {
        GameManager sceneManager = FindFirstObjectByType<GameManager>();

        // A stale, unconfigured manager can survive scene changes. Prefer the
        // configured manager serialized in the active scene over that instance.
        if (sceneManager != null && sceneManager.HasStageConfiguration())
        {
            if (Instance != sceneManager)
            {
                if (Instance != null)
                {
                    Destroy(Instance.gameObject);
                }

                Instance = sceneManager;
                DontDestroyOnLoad(sceneManager.gameObject);
            }
        }
        else if (Instance == null)
        {
            Instance = sceneManager;
        }

        return Instance;
    }

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
            if (!Instance.HasStageConfiguration() && HasStageConfiguration())
            {
                Destroy(Instance.gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private bool HasStageConfiguration()
    {
        if (stageEnemies == null || stageEnemies.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < stageEnemies.Length; i++)
        {
            if (stageEnemies[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    public void StartNewRun()
    {
        if (stageEnemies == null || stageEnemies.Length == 0)
        {
            Debug.LogError("[GameManager] Cannot start a run: Stage Enemies is empty. Assign the three enemy assets in the MainMenu scene.");
            return;
        }

        for (int i = 0; i < stageEnemies.Length; i++)
        {
            if (stageEnemies[i] == null)
            {
                Debug.LogError($"[GameManager] Cannot start a run: Stage Enemies element {i} is unassigned.");
                return;
            }
        }

        // FULL RESET — xóa sạch state cũ
        currentStageIndex = 1;
        playerCurrentHP = playerMaxHP;
        currentDiceFaces.Clear();
        Debug.Log($"[GameManager] StartNewRun → stage={currentStageIndex}, hp={playerCurrentHP}, enemies={stageEnemies.Length}");
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
