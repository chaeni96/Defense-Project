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

        // ù ��° ���Ǽҵ��� ù ���������� �׻� �ر�
        if (episodeNumber == 1 && stageNumber == 1) return true;

        // ���� ���Ǽҵ尡 Ŭ������ ���Ǽҵ庸�� ū ���
        if (episodeNumber > userData.f_clearEpisodeNumber + 1) return false;

        // ���� ���Ǽҵ尡 Ŭ������ ���Ǽҵ�� ���� ���
        if (episodeNumber == userData.f_clearEpisodeNumber + 1)
        {
            // ���������� ���������� �ر�
            return stageNumber <= userData.f_lastClearedStageNumber + 1;
        }

        // ���� ���Ǽҵ��� ��� ��� �������� �ر�
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
