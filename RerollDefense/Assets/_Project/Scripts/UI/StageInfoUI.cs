using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text stageName;
    [SerializeField] private TMP_Text stageDesc;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Image stageNameBg ;
    [SerializeField] private Image stageDescBg;
    [SerializeField] private Button stageButton;

    private int stageNumber;
 
    public void SetStageInfo(int stageNumber)
    {
        this.stageNumber = stageNumber;
        stageName.text = $"Stage {stageNumber}";
        stageDesc.text = $"Stage {stageNumber}¿¡ ´ëÇÑ ¾îÂ¼±¸ÀúÂ¼±¸..";
        var userData = D_LocalUserData.GetEntity(0);
        bool isUnlocked = stageNumber == 1 || stageNumber <= userData.f_lastClearedStageNumber + 1;

        lockIcon.SetActive(!isUnlocked);
        stageNameBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        stageDescBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        stageButton.interactable = isUnlocked;
    }

    public void OnClickStage()
    {
        GameManager.Instance.SelectStage(stageNumber);
        GameSceneManager.Instance.LoadScene(SceneKind.InGame);
    }
}
