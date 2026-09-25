using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ForgeController : MonoBehaviour
{
    public Transform rewardsContainer;
    public Transform currentDiceContainer;
    public Button nextStageButton;

    public DiceFaceData[] allPossibleFaces;

    private DiceFaceData selectedReward;
    private bool hasReplaced = false;

    private void Start()
    {
        if (nextStageButton != null) nextStageButton.gameObject.SetActive(false);
        GenerateRewards();
        PopulateCurrentDice();
    }

    private void GenerateRewards()
    {
        if (rewardsContainer == null || allPossibleFaces == null || allPossibleFaces.Length == 0) return;

        Button[] rewardBtns = rewardsContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < rewardBtns.Length; i++)
        {
            if (i < 3)
            {
                DiceFaceData randomFace = allPossibleFaces[Random.Range(0, allPossibleFaces.Length)];
                int index = i;
                rewardBtns[i].onClick.AddListener(() => OnSelectReward(randomFace, rewardBtns[index]));
                
                Image img = rewardBtns[i].GetComponent<Image>();
                if (randomFace.icon != null) img.sprite = randomFace.icon;
            }
        }
    }

    private void PopulateCurrentDice()
    {
        if (GameManager.Instance == null || currentDiceContainer == null) return;

        List<DiceFaceData> playerFaces = GameManager.Instance.currentDiceFaces;
        
        // Debug fallback
        if (playerFaces.Count == 0 && allPossibleFaces.Length > 0)
        {
            for(int i=0; i<6; i++) playerFaces.Add(allPossibleFaces[0]);
        }

        Button[] currentBtns = currentDiceContainer.GetComponentsInChildren<Button>();
        for (int i = 0; i < currentBtns.Length; i++)
        {
            if (i < playerFaces.Count)
            {
                int index = i;
                currentBtns[i].onClick.AddListener(() => OnSelectCurrentDice(index, currentBtns[index]));
                
                if (playerFaces[i].icon != null)
                {
                    currentBtns[i].GetComponent<Image>().sprite = playerFaces[i].icon;
                }
            }
        }
    }

    public void OnSelectReward(DiceFaceData face, Button btnSelected)
    {
        if (hasReplaced) return;
        selectedReward = face;
        Debug.Log("Forge: Selected Reward -> " + face.faceName);
    }

    public void OnSelectCurrentDice(int replaceIndex, Button btnSelected)
    {
        if (hasReplaced) return;
        if (selectedReward == null)
        {
            Debug.LogWarning("Forge: Please select a reward face first!");
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentDiceFaces[replaceIndex] = selectedReward;
            btnSelected.GetComponent<Image>().sprite = selectedReward.icon;
            hasReplaced = true;
            Debug.Log("Forge: Dice face replaced successfully!");
            
            if (nextStageButton != null) nextStageButton.gameObject.SetActive(true);
        }
    }

    public void OnNextStageClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentStageIndex++;
            GameManager.Instance.LoadCombatScene();
        }
    }
}
