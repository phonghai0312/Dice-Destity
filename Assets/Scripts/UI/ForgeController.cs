using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ForgeController — Tự tìm tất cả UI objects theo tên khi Scene load.
/// Không cần wiring thủ công qua Editor.
/// </summary>
public class ForgeController : MonoBehaviour
{
    [Header("Auto-discovered (không cần assign)")]
    public Button nextStageButton;
    public Transform rewardsContainer;
    public Transform currentDiceContainer;
    public DiceFaceData[] allPossibleFaces;

    // Runtime data
    private int selectedRewardIndex = -1;
    private List<GameObject> rewardCards = new List<GameObject>();
    private static readonly Color unselectedDim = new Color(0.55f, 0.55f, 0.55f, 0.7f);
    private static readonly Color selectedBright = Color.white;

    // Tên reward cards khớp với tên trong scene
    private readonly string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };

    private System.Type TMP;

    private void Start()
    {
        TMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        StartCoroutine(InitAfterFrame());
    }

    IEnumerator InitAfterFrame()
    {
        yield return null; // Đợi 1 frame để scene load xong

        AutoDiscover();
        SetupRewardButtons();
        SetupConfirmButton();
        AddFloatingAnimations();
    }

    // ─── Auto Discovery ────────────────────────────────────────────────────────

    void AutoDiscover()
    {
        // Tìm Confirm button
        GameObject confGo = GameObject.Find("ConfirmButton");
        if (confGo != null && nextStageButton == null)
            nextStageButton = confGo.GetComponent<Button>();

        // Tìm containers
        GameObject cdBox = GameObject.Find("CurrentDiceBox");
        if (cdBox != null) currentDiceContainer = cdBox.transform;

        GameObject rwCont = GameObject.Find("RewardsContainer");
        if (rwCont != null) rewardsContainer = rwCont.transform;

        // Tắt Confirm button cho đến khi chọn reward
        if (nextStageButton != null) nextStageButton.interactable = false;
    }

    // ─── Reward Buttons ────────────────────────────────────────────────────────

    void SetupRewardButtons()
    {
        rewardCards.Clear();

        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null) continue;

            rewardCards.Add(rw);

            // Thêm ForgeAnimator cho hiệu ứng trôi nổi
            ForgeAnimator fa = rw.GetComponent<ForgeAnimator>() ?? rw.AddComponent<ForgeAnimator>();
            fa.floatAmplitude = 10f;
            fa.floatSpeed = 0.9f + i * 0.15f;
            fa.floatOffset = i * 1.2f;
            fa.playEntryAnim = true;
            fa.entryDelay = 0.15f + i * 0.12f;

            int idx = i;
            Button btn = rw.GetComponent<Button>() ?? rw.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnRewardSelected(idx));
        }
    }

    void OnRewardSelected(int index)
    {
        selectedRewardIndex = index;

        // Visual feedback
        for (int i = 0; i < rewardCards.Count; i++)
        {
            if (rewardCards[i] == null) continue;
            Image img = rewardCards[i].GetComponent<Image>();
            Outline ol = rewardCards[i].GetComponent<Outline>();
            ForgeAnimator fa = rewardCards[i].GetComponent<ForgeAnimator>();

            if (i == index)
            {
                if (img != null) img.color = selectedBright;
                if (ol != null) { ol.effectColor = new Color(0.72f, 0.53f, 0.05f, 1f); ol.effectDistance = new Vector2(3, -3); }
                if (fa != null) fa.Bounce();
            }
            else
            {
                if (img != null) img.color = unselectedDim;
                if (ol != null) { ol.effectColor = new Color(0.15f, 0.12f, 0.18f, 0.5f); ol.effectDistance = new Vector2(1, -1); }
            }
        }

        // Mở nút Confirm
        if (nextStageButton != null) nextStageButton.interactable = true;

        Debug.Log($"[Forge] Selected reward: {rewardNames[index]}");
    }

    // ─── Confirm & Next ────────────────────────────────────────────────────────

    void SetupConfirmButton()
    {
        if (nextStageButton == null) return;
        nextStageButton.onClick.RemoveAllListeners();
        nextStageButton.onClick.AddListener(OnNextStageClicked);
    }

    public void OnNextStageClicked()
    {
        if (GameManager.Instance == null) return;
        if (selectedRewardIndex < 0)
        {
            Debug.LogWarning("[Forge] Chưa chọn reward dice!");
            return;
        }

        // Tăng stage và load combat tiếp theo
        GameManager.Instance.currentStageIndex++;

        // Nếu vượt qua cả 3 stage → về Main Menu (Win!)
        if (GameManager.Instance.currentStageIndex > 3)
        {
            Debug.Log("[Forge] You WIN! All 3 stages cleared.");
            GameManager.Instance.LoadMainMenu();
        }
        else
        {
            GameManager.Instance.LoadCombatScene();
        }
    }

    // ─── Floating Animations (các đối tượng phụ) ──────────────────────────────

    void AddFloatingAnimations()
    {
        // Current dice box entry slide
        AddEntryAnim("CurrentDiceBox", 0f);
        AddEntryAnim("ForgeTitle", 0f);
        AddEntryAnim("ForgeSub", 0.05f);
        AddEntryAnim("RewardTitle", 0.1f);
        AddEntryAnim("ConfirmButton", 0.4f);
    }

    void AddEntryAnim(string objName, float delay)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        ForgeAnimator fa = go.GetComponent<ForgeAnimator>() ?? go.AddComponent<ForgeAnimator>();
        fa.playEntryAnim = true;
        fa.entryDelay = delay;
        fa.floatAmplitude = 0f;
    }
}
