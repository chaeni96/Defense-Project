using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("EpisodeInfoUI", "EpisodeInfoUI", true)]

public class EpisodeInfoUI : FloatingPopupBase
{
    [SerializeField] private TMP_Text episodeNumber;
    [SerializeField] private TMP_Text episodeTitle;
    [SerializeField] private TMP_Text clearStageNumber;

    [SerializeField] private Button episodeButton;

    private D_EpisodeData episodeData;
    private D_LocalUserData userData;
    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    //Ŭ������ ���Ǽҵ��� ���� ���Ǽҵ� �ε�
    public void CreateEpisodeInfo()
    {
        // ���� ������ Ŭ���� ���Ǽҵ� ���� ��������
        userData = D_LocalUserData.GetEntity(0);
        int nextEpisodeNum = userData.f_clearEpisodeNumber + 1;

        // ǥ���� ���Ǽҵ� ������ ��������
        D_EpisodeData episodeData = D_EpisodeData.FindEntity(e => e.f_episodeNumber == nextEpisodeNum);

        if (episodeData == null)
        {
            // ������ ù ��° ���Ǽҵ� �����ֱ�
            episodeData = D_EpisodeData.FindEntity(e => e.f_episodeNumber == 1);
        }

        // ���Ǽҵ� ���� ����
        SetEpisodeInfo(episodeData);
    }

    public void SetEpisodeInfo(D_EpisodeData episode)
    {
        this.episodeData = episode;
        episodeNumber.text = $"Episode {episodeData.f_episodeNumber}";
        episodeTitle.text = $"{episodeData.f_episodeTitle}";
        //TODO : ��ä��
        //�ش� ���Ǽҵ��� �������� �ѹ� �޾ƿ;ߵ�
        //�� ���Ǽҵ帶�� stage ���� �ٸ��������Ƿ� data �����ؾߵ�
        if(userData.f_lastClearedStageNumber >= 5)
        {
            clearStageNumber.text = $"�� Ŭ����";
        }
        else
        {

            clearStageNumber.text = $"�������� ���� : {userData.f_lastClearedStageNumber} / 5";
        }
    }

    //���Ǽҵ� ���� ��ư
    public async void OnClickSelectEpisodeBtn()
    {
        var selectPopup =  await UIManager.Instance.ShowUI<EpisodeSelectPopup>();

        // ������ �˾��� ���� ���Ǽҵ� ���� ����
        if (selectPopup != null)
        {
            selectPopup.SetCurrentEpisode(episodeData);
        }

    }

    public void OnClickGameStartBtn()
    {
        if (GameManager.Instance.SelectEpisode(episodeData.f_episodeNumber))
        {
            // Ŭ������ ���������� ���� �������� ã��
            int nextStageNumber = userData.f_lastClearedStageNumber + 1;

            // �ش� ���Ǽҵ��� �������� �� ���� �������� ��������
            var nextStage = D_StageData.FindEntity(
                s => s.f_EpisodeData.Id == episodeData.Id && s.f_StageNumber == nextStageNumber
            );

          
            // ���������� ������ �ٷ� �ΰ������� ����
            if (nextStage != null)
            {
                GameManager.Instance.SelectedStageNumber = nextStage.f_StageNumber;

                GameSceneManager.Instance.LoadScene(SceneKind.InGame);

            }

            // EpisodeInfoUI �ݱ�
            UIManager.Instance.CloseUI<EpisodeInfoUI>();

        }
    }
}
