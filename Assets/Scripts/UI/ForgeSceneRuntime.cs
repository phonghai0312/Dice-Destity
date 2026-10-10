using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ForgeSceneRuntime — Gắn vào ForgeController object, tự động thêm animation vào tất cả UI.
/// Kích hoạt tự động khi vào scene The Forge.
/// </summary>
public class ForgeSceneRuntime : MonoBehaviour
{
    [Header("Dice Reward Buttons")]
    public List<Button> rewardDiceButtons = new List<Button>();

    private int selectedRewardIndex = -1;
    private List<GameObject> rewardObjects = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SetupScene());
    }

    IEnumerator SetupScene()
    {
        yield return null; // Đợi 1 frame để tất cả objects được init

        // Thêm animation trôi nổi cho reward dice
        AddFloatingToRewards();
        // Thêm animation entry cho tất cả sections
        AddEntryAnimations();
        // Wire up reward selection
        SetupRewardSelection();
    }

    void AddFloatingToRewards()
    {
        string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };
        
        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null) continue;

            rewardObjects.Add(rw);
            ForgeAnimator fa = rw.GetComponent<ForgeAnimator>() ?? rw.AddComponent<ForgeAnimator>();
            fa.floatAmplitude = 10f;
            fa.floatSpeed = 0.9f + i * 0.15f;   // Mỗi dice nổi ở tốc độ khác nhau → không đồng bộ, tự nhiên hơn
            fa.floatOffset = i * 1.2f;            // Phase offset
            fa.playEntryAnim = true;
            fa.entryDelay = 0.1f + i * 0.12f;    // Xuất hiện lần lượt
        }
    }

    void AddEntryAnimations()
    {
        // Current dice box trượt vào từ trái
        AddEntrySlide("CurrentDiceBox", 0.05f);
        // Divider fade in
        AddEntrySlide("Divider", 0.2f);
        // Header
        AddEntrySlide("ForgeTitle", 0f);
        // Confirm button
        AddEntrySlide("ConfirmButton", 0.5f);
    }

    void AddEntrySlide(string objName, float delay)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        ForgeAnimator fa = go.GetComponent<ForgeAnimator>() ?? go.AddComponent<ForgeAnimator>();
        fa.playEntryAnim = true;
        fa.entryDelay = delay;
        fa.floatAmplitude = 0f; // Không nổi, chỉ entry
    }

    void SetupRewardSelection()
    {
        // Lấy tất cả reward button bằng cách tìm theo tên
        string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };
        
        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null) continue;

            int idx = i; // Capture
            Button btn = rw.GetComponent<Button>();
            if (btn == null) continue;

            rewardDiceButtons.Add(btn);
            btn.onClick.AddListener(() => OnRewardSelected(idx));
        }
    }

    void OnRewardSelected(int index)
    {
        selectedRewardIndex = index;

        // Visual feedback: làm sáng selected, tối các cái còn lại
        for (int i = 0; i < rewardObjects.Count; i++)
        {
            if (rewardObjects[i] == null) continue;
            
            Image img = rewardObjects[i].GetComponent<Image>();
            ForgeAnimator fa = rewardObjects[i].GetComponent<ForgeAnimator>();
            Outline ol = rewardObjects[i].GetComponent<Outline>();
            
            if (i == index)
            {
                // Selected: viền vàng sáng + bounce animation
                if (ol != null)
                {
                    ol.effectColor = new Color(0.72f, 0.53f, 0.05f, 1f);
                    ol.effectDistance = new Vector2(3, -3);
                }
                if (fa != null)
                {
                    fa.enableGlowPulse = false;
                    fa.Bounce();
                }
                if (img != null) img.color = new Color(img.color.r * 1.3f, img.color.g * 1.3f, img.color.b * 1.3f, 1f);
            }
            else
            {
                // Not selected: tối đi
                if (ol != null)
                {
                    ol.effectColor = new Color(0.16f, 0.14f, 0.18f, 0.6f);
                    ol.effectDistance = new Vector2(1, -1);
                }
                if (img != null) img.color = new Color(img.color.r * 0.6f, img.color.g * 0.6f, img.color.b * 0.6f, 0.7f);
            }
        }

        // Enable confirm button (nếu nó bị disabled)
        GameObject confirmGo = GameObject.Find("ConfirmButton");
        if (confirmGo != null)
        {
            Button confirmBtn = confirmGo.GetComponent<Button>();
            if (confirmBtn != null) confirmBtn.interactable = true;
        }

        // Lưu lựa chọn vào GameManager để phase sau dùng
        if (GameManager.Instance != null)
        {
            Debug.Log($"[Forge] Selected reward dice index: {index}");
        }
    }

    public int GetSelectedRewardIndex() => selectedRewardIndex;
}
