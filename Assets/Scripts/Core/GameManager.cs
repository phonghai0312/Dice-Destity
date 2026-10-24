using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static GameManager ResolveInstance()
    {
        GameManager sceneManager = FindAnyObjectByType<GameManager>();

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

        if (Instance != null)
        {
            Instance.EnsureDiceBagInitialized();
        }

        return Instance;
    }

    [Header("Player Data (GDD 3.1)")]
    public int playerMaxHP = 70;
    public int playerCurrentHP = 70;
    public int maxRerollsPerTurn = 1; // GDD 3.1: Mặc định R_max = 1

    [Tooltip("Túi xúc xắc lục diện (mỗi viên 6 mặt) đang trang bị")]
    public List<DiceInstance> playerDiceBag = new List<DiceInstance>();

    [Tooltip("Danh sách mặt xúc xắc (giữ tương thích ngược)")]
    public List<DiceFaceData> currentDiceFaces = new List<DiceFaceData>();

    [Header("Run Progression (GDD 4)")]
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
        EnsureDiceBagInitialized();
    }

    public void EnsureDiceBagInitialized()
    {
        if (playerDiceBag == null || playerDiceBag.Count == 0)
        {
            playerDiceBag = DiceCatalog.CreateStarterDiceBag();
            SyncLegacyDiceFaceList();
        }
    }

    public void SyncLegacyDiceFaceList()
    {
        if (currentDiceFaces == null)
        {
            currentDiceFaces = new List<DiceFaceData>();
        }
        currentDiceFaces.Clear();

        if (playerDiceBag == null) return;
        for (int i = 0; i < playerDiceBag.Count; i++)
        {
            DiceInstance die = playerDiceBag[i];
            if (die == null || die.faces == null) continue;
            for (int f = 0; f < die.faces.Length; f++)
            {
                if (die.faces[f] != null)
                {
                    currentDiceFaces.Add(die.faces[f]);
                }
            }
        }
    }

    /// <summary>
    /// Rèn (ghi đè) 1 mặt xúc xắc mới lên vị trí mặt cũ (GDD 3.2: Dice-Crafting).
    /// </summary>
    public bool ReplacePlayerDiceFace(int diceIndex, int faceSlotIndex, DiceFaceData newFace)
    {
        EnsureDiceBagInitialized();
        if (newFace == null || diceIndex < 0 || diceIndex >= playerDiceBag.Count)
        {
            return false;
        }

        bool replaced = playerDiceBag[diceIndex].ReplaceFace(faceSlotIndex, newFace);
        if (replaced)
        {
            SyncLegacyDiceFaceList();
            Debug.Log($"[GameManager] Rèn thành công: Đè mặt '{newFace.faceName}' ({newFace.valueV}) vào Viên #{diceIndex + 1} - Ô #{faceSlotIndex + 1}");
        }
        return replaced;
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

        // FULL RESET — xóa sạch state cũ và cấp lại bộ xúc xắc khởi đầu chuẩn GDD
        currentStageIndex = 1;
        playerCurrentHP = playerMaxHP;
        playerDiceBag = DiceCatalog.CreateStarterDiceBag();
        SyncLegacyDiceFaceList();

        Debug.Log($"[GameManager] StartNewRun → stage={currentStageIndex}, hp={playerCurrentHP}, diceCount={playerDiceBag.Count}, enemies={stageEnemies.Length}");
        LoadCombatScene();
    }

    public void LoadCombatScene() => SceneManager.LoadScene("CombatScene");
    public void LoadForgeScene()  => SceneManager.LoadScene("TheForge");
    public void LoadMainMenu()    => SceneManager.LoadScene("MainMenu");

    public void OnEnemyDefeated()
    {
        Debug.Log($"[GameManager] Enemy defeated! Stage {currentStageIndex} done (Player HP={playerCurrentHP}/{playerMaxHP}). Loading Forge...");
        LoadForgeScene();
    }
}
