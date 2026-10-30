using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static GameManager ResolveInstance()
    {
        if (Instance != null)
        {
            // Nếu có bản sao GameManager thứ hai trong scene hiện tại (do đặt sẵn trong Scene Editor), dọn dẹp nó
            GameManager[] allGms = FindObjectsByType<GameManager>();
            for (int i = 0; i < allGms.Length; i++)
            {
                if (allGms[i] != null && allGms[i] != Instance)
                {
                    // Nếu Instance hiện tại chưa có stageEnemies, kế thừa từ scene trước khi hủy bản sao thừa
                    if ((Instance.stageEnemies == null || Instance.stageEnemies.Length == 0) && allGms[i].HasStageConfiguration())
                    {
                        Instance.stageEnemies = allGms[i].stageEnemies;
                    }
                    Destroy(allGms[i].gameObject);
                }
            }

            Instance.EnsureDiceBagInitialized();
            return Instance;
        }

        GameManager sceneManager = FindAnyObjectByType<GameManager>();
        if (sceneManager != null)
        {
            Instance = sceneManager;
            DontDestroyOnLoad(sceneManager.gameObject);
            Instance.EnsureDiceBagInitialized();
            return Instance;
        }

        GameObject go = new GameObject("GameManager");
        Instance = go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
        Instance.EnsureDiceBagInitialized();
        return Instance;
    }

    [Header("Player Data (GDD 3.1)")]
    public int playerMaxHP = 70;
    public int playerCurrentHP = 70;
    public int maxRerollsPerTurn = 1; // GDD 3.1: Mặc định R_max = 1

    [Header("Character Active Skills Progression (GDD 3.1 & 3.2)")]
    public int normalSkillLevel = 1;
    public int normalSkillBaseCost = 2;
    public int normalSkillBaseDamage = 5;

    public int ultimateSkillLevel = 1;
    public int ultimateSkillBaseCost = 6;
    public int ultimateSkillBaseDamage = 14;
    public int ultimateSkillBaseBurnStacks = 4;

    [Tooltip("Túi xúc xắc lục diện (mỗi viên 6 mặt) đang trang bị")]
    public List<DiceInstance> playerDiceBag = new List<DiceInstance>();

    [Tooltip("Túi mặt xúc xắc dự trữ của người chơi (dùng để rèn đè và cường hóa trong Lò Rèn)")]
    public List<DiceFaceData> playerFacePool = new List<DiceFaceData>();

    [Tooltip("Danh sách mặt xúc xắc (giữ tương thích ngược)")]
    public List<DiceFaceData> currentDiceFaces = new List<DiceFaceData>();

    [Header("Run Progression (GDD 4)")]
    public int currentStageIndex = 1; // 1=Goblin, 2=Orc, 3=Minotaur
    public int heroMapStageIndex = 1; // Vị trí hiện tại của Hiệp Sĩ trên Bản Đồ Ải (1..3)
    public int highestUnlockedStageIndex = 1; // Ải cao nhất đã mở khóa trên Bản Đồ Ải
    public int lastClearedStageIndex = 0; // Ải gần nhất vừa đánh thắng
    public int pendingForgeUpgrades = 0; // Số lượt rèn xúc xắc khả dụng sau khi qua ải
    public bool isRunVictory = false; // Đã chiến thắng toàn bộ các ải
    public EnemyData[] stageEnemies;

    private readonly List<DiceFaceData> cachedForgeRewards = new List<DiceFaceData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Đã có Singleton đang hoạt động (chứa túi xúc xắc, pool mặt và tiến trình chơi của game thủ)
            // Kế thừa stageEnemies nếu Singleton chưa có
            if ((Instance.stageEnemies == null || Instance.stageEnemies.Length == 0) && HasStageConfiguration())
            {
                Instance.stageEnemies = stageEnemies;
            }

            // Hủy bản sao thừa trong scene này để TUYỆT ĐỐI KHÔNG làm mất dữ liệu người chơi
            Destroy(gameObject);
            return;
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

        if (playerFacePool == null || playerFacePool.Count == 0)
        {
            playerFacePool = DiceCatalog.CreateStarterReservePool();
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
        heroMapStageIndex = 1;
        highestUnlockedStageIndex = 1;
        lastClearedStageIndex = 0;
        pendingForgeUpgrades = 0;
        isRunVictory = false;
        cachedForgeRewards.Clear();

        normalSkillLevel = 1;
        ultimateSkillLevel = 1;

        playerCurrentHP = playerMaxHP;
        playerDiceBag = DiceCatalog.CreateStarterDiceBag();
        playerFacePool = DiceCatalog.CreateStarterReservePool();
        SyncLegacyDiceFaceList();

        Debug.Log($"[GameManager] StartNewRun → stage={currentStageIndex}, hp={playerCurrentHP}, diceCount={playerDiceBag.Count}, enemies={stageEnemies.Length}");
        LoadCombatScene();
    }

    public int GetTotalStages()
    {
        return (stageEnemies != null && stageEnemies.Length > 0) ? stageEnemies.Length : 3;
    }

    public List<DiceFaceData> GetOrRollForgeRewards(int count)
    {
        if (cachedForgeRewards.Count != count)
        {
            cachedForgeRewards.Clear();
            List<DiceFaceData> rolled = DiceCatalog.RollRandomRewards(count);
            if (rolled != null)
            {
                cachedForgeRewards.AddRange(rolled);
            }
        }
        return new List<DiceFaceData>(cachedForgeRewards);
    }

    public void ConsumeForgeUpgrade()
    {
        if (pendingForgeUpgrades > 0)
        {
            pendingForgeUpgrades--;
        }
        cachedForgeRewards.Clear();
    }

    public int GetNormalSkillDamage()
    {
        return normalSkillBaseDamage + Mathf.Max(0, normalSkillLevel - 1) * 3;
    }

    public int GetUltimateSkillDamage()
    {
        return ultimateSkillBaseDamage + Mathf.Max(0, ultimateSkillLevel - 1) * 4;
    }

    public int GetUltimateSkillBurnStacks()
    {
        return ultimateSkillBaseBurnStacks + Mathf.Max(0, ultimateSkillLevel - 1) * 2;
    }

    public void UpgradeNormalSkill()
    {
        normalSkillLevel = Mathf.Max(1, normalSkillLevel) + 1;
        Debug.Log($"[GameManager] Nâng cấp Kỹ năng Thường lên Cấp {normalSkillLevel}: Gây {GetNormalSkillDamage()} sát thương!");
    }

    public void UpgradeUltimateSkill()
    {
        ultimateSkillLevel = Mathf.Max(1, ultimateSkillLevel) + 1;
        Debug.Log($"[GameManager] Nâng cấp Kỹ năng Tối thượng lên Cấp {ultimateSkillLevel}: Gây {GetUltimateSkillDamage()} sát thương + {GetUltimateSkillBurnStacks()} Thiêu đốt!");
    }

    public void LoadCombatScene()   => SceneManager.LoadScene("CombatScene");
    public void LoadStageMapScene() => SceneManager.LoadScene("StageMap");
    public void LoadForgeScene()    => SceneManager.LoadScene("TheForge");
    public void LoadMainMenu()      => SceneManager.LoadScene("MainMenu");

    public void OnEnemyDefeated()
    {
        int totalStages = GetTotalStages();
        lastClearedStageIndex = Mathf.Max(lastClearedStageIndex, currentStageIndex);
        heroMapStageIndex = currentStageIndex;
        pendingForgeUpgrades = Mathf.Max(1, pendingForgeUpgrades + 1);
        cachedForgeRewards.Clear();

        if (currentStageIndex < totalStages)
        {
            currentStageIndex++;
            highestUnlockedStageIndex = Mathf.Max(highestUnlockedStageIndex, currentStageIndex);
            isRunVictory = false;
        }
        else
        {
            highestUnlockedStageIndex = totalStages;
            isRunVictory = true;
        }

        Debug.Log($"[GameManager] Enemy defeated! Cleared Stage {lastClearedStageIndex}/{totalStages} (Next Stage={currentStageIndex}, Player HP={playerCurrentHP}/{playerMaxHP}). Loading StageMap...");
        LoadStageMapScene();
    }

    /// <summary>
    /// Mở khóa tuần tự 1 ô mặt xúc xắc bị khóa (bắt đầu từ Viên 1, rồi Viên 2, Viên 3).
    /// </summary>
    public bool UnlockNextLockedSlot(out int unlockedDieIndex, out int unlockedSlotIndex)
    {
        unlockedDieIndex = -1;
        unlockedSlotIndex = -1;
        EnsureDiceBagInitialized();

        if (playerDiceBag == null) return false;

        for (int d = 0; d < playerDiceBag.Count; d++)
        {
            DiceInstance die = playerDiceBag[d];
            if (die == null) continue;

            for (int s = 0; s < DiceInstance.FACES_COUNT; s++)
            {
                if (die.IsSlotLocked(s))
                {
                    die.UnlockSlot(s);
                    // Đảm bảo ô mới mở là mặt Trống để người chơi dán mặt lên
                    if (die.faces[s] == null)
                    {
                        die.faces[s] = DiceFaceData.CreateRuntimeFace("Trống", DiceFaceType.Special, DiceSkillKind.Blank, 0, DiceFaceRarity.Common, null, null, 1);
                    }
                    unlockedDieIndex = d;
                    unlockedSlotIndex = s;
                    SyncLegacyDiceFaceList();
                    Debug.Log($"[GameManager] Đã mở khóa ô mặt mới: Viên #{d + 1} - Ô #{s + 1}!");
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Thêm 1 mặt xúc xắc mới vào Pool dự trữ của người chơi.
    /// </summary>
    public void AddFaceToPool(DiceFaceData face)
    {
        if (face == null) return;
        if (playerFacePool == null) playerFacePool = new List<DiceFaceData>();
        playerFacePool.Add(face);
        Debug.Log($"[GameManager] Thêm mặt '{face.faceName}' Lv.{face.level} vào Pool dự trữ (Tổng: {playerFacePool.Count} mặt).");
    }

    /// <summary>
    /// Đếm tổng số mặt thuộc loại kind trên cả 3 viên xúc xắc (chỉ tính các ô đã mở khóa và không phải mặt Trống).
    /// </summary>
    public int CountTotalFaceKindAcrossAllDice(DiceSkillKind kind)
    {
        if (playerDiceBag == null) return 0;
        int total = 0;
        for (int d = 0; d < playerDiceBag.Count; d++)
        {
            DiceInstance die = playerDiceBag[d];
            if (die == null) continue;
            total += die.CountFaceKind(kind);
        }
        return total;
    }

    /// <summary>
    /// Gán / Ghi đè 1 mặt xúc xắc từ Pool vào ô mặt của một viên xúc xắc:
    /// - Không thể gán vào ô đang khóa.
    /// - Mỗi loại mặt chỉ được xuất hiện tối đa 2 lần trên cùng 1 viên xúc xắc.
    /// - Mỗi loại mặt chỉ được xuất hiện tối đa 4 lần trên toàn bộ 3 viên xúc xắc.
    /// - Nếu vi phạm logic, trả về false và lý do chi tiết.
    /// - Mặt trong Pool KHÔNG BỊ BIẾN MẤT (chỉ hiển thị thư viện để gán).
    /// </summary>
    public bool EquipFaceFromPoolToDie(int poolIndex, int dieIndex, int slotIndex, out string errorMessage)
    {
        errorMessage = null;
        EnsureDiceBagInitialized();

        if (playerFacePool == null || poolIndex < 0 || poolIndex >= playerFacePool.Count)
        {
            errorMessage = "Mặt xúc xắc trong túi không tồn tại.";
            return false;
        }

        if (playerDiceBag == null || dieIndex < 0 || dieIndex >= playerDiceBag.Count)
        {
            errorMessage = "Viên xúc xắc không hợp lệ.";
            return false;
        }

        DiceInstance die = playerDiceBag[dieIndex];
        DiceFaceData faceToEquip = playerFacePool[poolIndex];

        if (slotIndex < 0 || slotIndex >= DiceInstance.FACES_COUNT)
        {
            errorMessage = "Vị trí ô xúc xắc không hợp lệ.";
            return false;
        }

        if (die.IsSlotLocked(slotIndex))
        {
            errorMessage = "Ô này đang bị khóa! Hãy vượt thêm Ải trên Bản Đồ để mở khóa.";
            return false;
        }

        DiceFaceData currentFace = die.faces[slotIndex];
        DiceSkillKind currentKind = (currentFace != null) ? currentFace.GetResolvedSkillKind() : DiceSkillKind.Blank;
        DiceSkillKind newKind = faceToEquip.GetResolvedSkillKind();

        // 1. Kiểm tra Giới hạn tối đa 2 mặt cùng loại trên 1 viên
        int countOnThisDie = die.CountFaceKind(newKind);
        if (currentKind != newKind && countOnThisDie >= 2)
        {
            errorMessage = $"Loại mặt '{faceToEquip.faceName}' đã có {countOnThisDie}/2 mặt trên viên này! (Tối đa 2 mặt cùng loại trên 1 viên xúc xắc)";
            return false;
        }

        // 2. Kiểm tra Giới hạn tối đa 4 mặt cùng loại trên cả 3 viên xúc xắc
        int countAcrossAllDice = CountTotalFaceKindAcrossAllDice(newKind);
        if (currentKind != newKind && countAcrossAllDice >= 4)
        {
            errorMessage = $"Loại mặt '{faceToEquip.faceName}' đã có {countAcrossAllDice}/4 mặt trên toàn bộ bộ xúc xắc! (Tối đa 4 mặt cùng loại trên cả 3 viên)";
            return false;
        }

        // Thực hiện gán / ghi đè mặt mới (mặt trong pool KHÔNG biến mất)
        die.ReplaceFace(slotIndex, faceToEquip.Clone());
        SyncLegacyDiceFaceList();
        Debug.Log($"[GameManager] Gán thành công mặt '{faceToEquip.faceName}' Lv.{faceToEquip.level} vào {die.diceName} - Ô #{slotIndex + 1} (Mặt trong pool vẫn được giữ nguyên).");
        return true;
    }

    /// <summary>
    /// Cường hóa (kết hợp) 2 mặt giống nhau trong Pool để nâng cấp lên cấp cao hơn
    /// VÀ TỰ ĐỘNG THAY THẾ các mặt cũ tương ứng trên cả 3 viên xúc xắc thành mặt mới được cường hóa.
    /// </summary>
    public bool MergeFacesInPoolAndUpgradeDice(int indexA, int indexB, out DiceFaceData upgradedFace, out int autoUpgradedDiceCount)
    {
        upgradedFace = null;
        autoUpgradedDiceCount = 0;

        if (playerFacePool == null) return false;
        if (indexA < 0 || indexA >= playerFacePool.Count) return false;
        if (indexB < 0 || indexB >= playerFacePool.Count) return false;
        if (indexA == indexB) return false;

        DiceFaceData faceA = playerFacePool[indexA];
        DiceFaceData faceB = playerFacePool[indexB];

        if (faceA == null || faceB == null) return false;
        if (!faceA.CanMergeWith(faceB)) return false;

        DiceSkillKind oldKind = faceA.GetResolvedSkillKind();
        int oldLevel = faceA.level;

        upgradedFace = DiceFaceData.CreateUpgradedFace(faceA, faceB);

        // Gộp trong Pool: Thay indexA bằng mặt nâng cấp, loại bỏ indexB
        playerFacePool[indexA] = upgradedFace;
        playerFacePool.RemoveAt(indexB);

        // Tự động thay thế các mặt cũ tương ứng trên cả 3 viên xúc xắc
        if (playerDiceBag != null)
        {
            for (int d = 0; d < playerDiceBag.Count; d++)
            {
                DiceInstance die = playerDiceBag[d];
                if (die == null || die.faces == null) continue;

                for (int s = 0; s < die.faces.Length; s++)
                {
                    if (die.IsSlotLocked(s)) continue;
                    DiceFaceData currentFace = die.faces[s];
                    if (currentFace != null && currentFace.GetResolvedSkillKind() == oldKind && currentFace.level == oldLevel)
                    {
                        die.faces[s] = upgradedFace.Clone();
                        autoUpgradedDiceCount++;
                    }
                }
            }
        }

        SyncLegacyDiceFaceList();
        Debug.Log($"[GameManager] Cường hóa thành công: {faceA.faceName} + {faceB.faceName} -> {upgradedFace.faceName} Lv.{upgradedFace.level}. Tự động nâng cấp {autoUpgradedDiceCount} mặt trên các viên xúc xắc!");
        return true;
    }

    /// <summary>
    /// Wrapper giữ tương thích ngược cho MergeFacesInPool
    /// </summary>
    public bool MergeFacesInPool(int indexA, int indexB, out DiceFaceData upgradedFace)
    {
        return MergeFacesInPoolAndUpgradeDice(indexA, indexB, out upgradedFace, out _);
    }
}
