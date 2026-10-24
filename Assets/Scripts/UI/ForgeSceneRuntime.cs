using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ForgeSceneRuntime — Giữ tương thích với Scene TheForge.
/// Nếu ForgeController đã có mặt trong Scene, ForgeController sẽ trực tiếp điều phối toàn bộ UI và hiệu ứng.
/// </summary>
public class ForgeSceneRuntime : MonoBehaviour
{
    [Header("Dice Reward Buttons")]
    public List<Button> rewardDiceButtons = new List<Button>();

    private int selectedRewardIndex = -1;
    private readonly List<GameObject> rewardObjects = new List<GameObject>();

    private void Start()
    {
        // Nếu ForgeController đang hoạt động trong Scene thì nhường quyền điều khiển cho ForgeController
        if (FindAnyObjectByType<ForgeController>() != null)
        {
            return;
        }

        StartCoroutine(SetupScene());
    }

    private IEnumerator SetupScene()
    {
        yield return null;

        AddFloatingToRewards();
        AddEntryAnimations();
        SetupRewardSelection();
    }

    private void AddFloatingToRewards()
    {
        string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };

        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null) continue;

            rewardObjects.Add(rw);
            ForgeAnimator fa = rw.GetComponent<ForgeAnimator>();
            if (fa == null) fa = rw.AddComponent<ForgeAnimator>();
            fa.floatAmplitude = 10f;
            fa.floatSpeed = 0.9f + i * 0.15f;
            fa.floatOffset = i * 1.2f;
            fa.playEntryAnim = true;
            fa.entryDelay = 0.1f + i * 0.12f;
        }
    }

    private void AddEntryAnimations()
    {
        AddEntrySlide("CurrentDiceContainer", 0.05f);
        AddEntrySlide("Divider", 0.2f);
        AddEntrySlide("ForgeTitle", 0f);
        AddEntrySlide("ConfirmButton", 0.5f);
    }

    private void AddEntrySlide(string objName, float delay)
    {
        GameObject go = GameObject.Find(objName);
        if (go == null) return;
        ForgeAnimator fa = go.GetComponent<ForgeAnimator>();
        if (fa == null) fa = go.AddComponent<ForgeAnimator>();
        fa.playEntryAnim = true;
        fa.entryDelay = delay;
        fa.floatAmplitude = 0f;
    }

    private void SetupRewardSelection()
    {
        string[] rewardNames = { "Reward_Cleave", "Reward_Inferno", "Reward_Doom" };

        for (int i = 0; i < rewardNames.Length; i++)
        {
            GameObject rw = GameObject.Find(rewardNames[i]);
            if (rw == null) continue;

            int idx = i;
            Button btn = rw.GetComponent<Button>();
            if (btn == null) continue;

            rewardDiceButtons.Add(btn);
            btn.onClick.AddListener(() => OnRewardSelected(idx));
        }
    }

    private void OnRewardSelected(int index)
    {
        selectedRewardIndex = index;

        for (int i = 0; i < rewardObjects.Count; i++)
        {
            if (rewardObjects[i] == null) continue;

            Image img = rewardObjects[i].GetComponent<Image>();
            ForgeAnimator fa = rewardObjects[i].GetComponent<ForgeAnimator>();
            Outline ol = rewardObjects[i].GetComponent<Outline>();

            if (i == index)
            {
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
                if (ol != null)
                {
                    ol.effectColor = new Color(0.16f, 0.14f, 0.18f, 0.6f);
                    ol.effectDistance = new Vector2(1, -1);
                }
                if (img != null) img.color = new Color(img.color.r * 0.6f, img.color.g * 0.6f, img.color.b * 0.6f, 0.7f);
            }
        }

        GameObject confirmGo = GameObject.Find("ConfirmButton");
        if (confirmGo != null)
        {
            Button confirmBtn = confirmGo.GetComponent<Button>();
            if (confirmBtn != null) confirmBtn.interactable = true;
        }
    }

    public int GetSelectedRewardIndex() => selectedRewardIndex;
}
