using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class StageCardUI : MonoBehaviour
{
    [SerializeField] private TMP_Text stageName;
    [SerializeField] private TMP_Text stageDesc;
    [SerializeField] private Button stageButton;

    [SerializeField] private GameObject lockIcon;

    private D_StageData stageData;

    public void SetStageInfo(D_StageData stage)
    {
        this.stageData = stage;

        stageName.text = $"Stage {stage.f_StageNumber}";
        stageDesc.text = stage.f_StageDescription;

        var userData = D_LocalUserData.GetEntity(0);
        bool isUnlocked = IsStageUnlocked(stage, userData);

        lockIcon.SetActive(!isUnlocked);
        stageButton.interactable = isUnlocked;
    }

    private bool IsStageUnlocked(D_StageData stage, D_LocalUserData userData)
    {
        int episodeNumber = stage.f_EpisodeData.f_episodeNumber;
        int stageNumber = stage.f_StageNumber;

        // 첫 번째 에피소드의 첫 스테이지는 항상 해금
        if (episodeNumber == 1 && stageNumber == 1) return true;

        // 현재 에피소드가 클리어한 에피소드보다 큰 경우
        if (episodeNumber > userData.f_clearEpisodeNumber + 1) return false;

        // 현재 에피소드가 클리어한 에피소드와 같은 경우
        if (episodeNumber == userData.f_clearEpisodeNumber + 1)
        {
            // 스테이지는 순차적으로 해금
            return stageNumber <= userData.f_lastClearedStageNumber + 1;
        }

        // 이전 에피소드의 경우 모든 스테이지 해금
        return episodeNumber <= userData.f_clearEpisodeNumber;
    }

    public void OnClickStage()
    {
        if (stageData != null)
        {
            GameManager.Instance.SelectedStageNumber = stageData.f_StageNumber;

            GameSceneManager.Instance.LoadScene(SceneKind.InGame);
        }
    }
}
