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

    //클리어한 에피소드의 다음 에피소드 로드
    public void CreateEpisodeInfo()
    {
        // 현재 유저의 클리어 에피소드 정보 가져오기
        userData = D_LocalUserData.GetEntity(0);
        int nextEpisodeNum = userData.f_clearEpisodeNumber + 1;

        // 표시할 에피소드 데이터 가져오기
        D_EpisodeData episodeData = D_EpisodeData.FindEntity(e => e.f_episodeNumber == nextEpisodeNum);

        if (episodeData == null)
        {
            // 없으면 첫 번째 에피소드 보여주기
            episodeData = D_EpisodeData.FindEntity(e => e.f_episodeNumber == 1);
        }

        // 에피소드 정보 설정
        SetEpisodeInfo(episodeData);
    }

    public void SetEpisodeInfo(D_EpisodeData episode)
    {
        this.episodeData = episode;
        episodeNumber.text = $"Episode {episodeData.f_episodeNumber}";
        episodeTitle.text = $"{episodeData.f_episodeTitle}";
        //TODO : 김채현
        //해당 에피소드의 스테이지 넘버 받아와야됨
        //각 에피소드마다 stage 개수 다를수있으므로 data 수정해야됨
        if(userData.f_lastClearedStageNumber >= 5)
        {
            clearStageNumber.text = $"올 클리어";
        }
        else
        {

            clearStageNumber.text = $"스테이지 돌파 : {userData.f_lastClearedStageNumber} / 5";
        }
    }

    //에피소드 선택 버튼
    public async void OnClickSelectEpisodeBtn()
    {
        var selectPopup =  await UIManager.Instance.ShowUI<EpisodeSelectPopup>();

        // 생성된 팝업에 현재 에피소드 정보 전달
        if (selectPopup != null)
        {
            selectPopup.SetCurrentEpisode(episodeData);
        }

    }

    public void OnClickGameStartBtn()
    {
        if (GameManager.Instance.SelectEpisode(episodeData.f_episodeNumber))
        {
            // 클리어한 스테이지의 다음 스테이지 찾기
            int nextStageNumber = userData.f_lastClearedStageNumber + 1;

            // 해당 에피소드의 스테이지 중 다음 스테이지 가져오기
            var nextStage = D_StageData.FindEntity(
                s => s.f_EpisodeData.Id == episodeData.Id && s.f_StageNumber == nextStageNumber
            );

          
            // 스테이지가 있으면 바로 인게임으로 진입
            if (nextStage != null)
            {
                GameManager.Instance.SelectedStageNumber = nextStage.f_StageNumber;

                GameSceneManager.Instance.LoadScene(SceneKind.InGame);

            }

            // EpisodeInfoUI 닫기
            UIManager.Instance.CloseUI<EpisodeInfoUI>();

        }
    }
}
