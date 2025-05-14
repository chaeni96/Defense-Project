using AutoBattle.Scripts.DataController;
using TMPro;
using UnityEngine;

public class EpisodeInfoUI : MonoBehaviour
{
    [Header("Episode Info")]
    [SerializeField] private TMP_Text episodeNumber;
    [SerializeField] private TMP_Text episodeTitle;
    [SerializeField] private TMP_Text clearStageNumber;

    [Header("Episode Control")] 
    [SerializeField] private GameObject prevEpisodeDimObject;
    [SerializeField] private GameObject nextEpisodeDimObject;
    [SerializeField] private GameObject gameStartDimObject;

    private EpisodeInfoParam currentEpisodeInfoParam;
    
    public void InitializeEpisodeInfo(EpisodeInfoParam episodeInfoParam)
    {
        currentEpisodeInfoParam = episodeInfoParam;

        var episodeData = currentEpisodeInfoParam.EpisodeData;
        if (episodeData == null)
        {
            Debug.LogError("EpisodeData is null");
            return;
        }
        
        prevEpisodeDimObject.SetActive(episodeData.f_episodeNumber - 1 <= 0);
        nextEpisodeDimObject.SetActive(episodeData.f_episodeNumber + 1 > D_EpisodeData.GetMaxEpisodeNumber());
        
        SetEpisodeInfo(currentEpisodeInfoParam.EpisodeData);
    }

    private void SetEpisodeInfo(D_EpisodeData episode)
    {
        episodeNumber.text = $"Episode {episode.f_episodeNumber}";
        episodeTitle.text = $"{episode.f_episodeTitle}";

        var maxStageNumber = D_StageData.GetEntitiesByKeyEpisodeKey(episode).Count;
        
        if(currentEpisodeInfoParam.UserBestRecordStage >= maxStageNumber)
        {
            clearStageNumber.text = $"올 클리어";
        }
        else
        {
            clearStageNumber.text = $"스테이지 돌파 : {currentEpisodeInfoParam.UserBestRecordStage} / {maxStageNumber}";
        }
        
        // 게임 시작 버튼 활성화
        gameStartDimObject.SetActive(currentEpisodeInfoParam.CanPlay == false);
    }

    public void OnClickNextEpisode()
    {
        // 다음 에피소드로 이동
        if (currentEpisodeInfoParam.EpisodeData.f_episodeNumber + 1 <= D_EpisodeData.GetMaxEpisodeNumber())
        {
            var nextEpisode = D_EpisodeData.FindEntity(
                e => e.f_episodeNumber == currentEpisodeInfoParam.EpisodeData.f_episodeNumber + 1
            );
            
            var nextEpisodeStageDataList = D_StageData.GetEntitiesByKeyEpisodeKey(nextEpisode);
            
            if (nextEpisode != null && nextEpisodeStageDataList is {Count: > 0})
            {
                var nextMaxStageCount = nextEpisodeStageDataList.Count;

                var userData = D_LocalUserData.GetEntity(0);
                var canPlay = nextEpisode.f_episodeNumber == 1 || userData.f_clearEpisodeNumber >= nextEpisode.f_episodeNumber;
                var userBestRecordStage = GetUserBestRecordStageByEpisode(nextEpisode);
                InitializeEpisodeInfo(new EpisodeInfoParam(nextEpisode, userBestRecordStage, nextMaxStageCount, canPlay));
            }
        }
        else
        {
            Debug.Log("다음 에피소드가 없습니다.");
        }
        
    }
    
    public void OnClickPrevEpisode()
    {
        // 이전 에피소드로 이동
        if (currentEpisodeInfoParam.EpisodeData.f_episodeNumber - 1 > 0)
        {
            var prevEpisode = D_EpisodeData.FindEntity(
                e => e.f_episodeNumber == currentEpisodeInfoParam.EpisodeData.f_episodeNumber - 1
            );
            
            var prevEpisodeStageDataList = D_StageData.GetEntitiesByKeyEpisodeKey(prevEpisode);
            
            if (prevEpisode != null && prevEpisodeStageDataList is {Count: > 0})
            {
                var prevMaxStageCount = prevEpisodeStageDataList.Count;
                
                var userData = D_LocalUserData.GetEntity(0);
                var canPlay = prevEpisode.f_episodeNumber == 1 || userData.f_clearEpisodeNumber >= prevEpisode.f_episodeNumber;
                var userBestRecordStage = GetUserBestRecordStageByEpisode(prevEpisode);
                InitializeEpisodeInfo(new EpisodeInfoParam(prevEpisode, userBestRecordStage, prevMaxStageCount, canPlay));
            }
        }
        else
        {
            Debug.Log("이전 에피소드가 없습니다.");
        }
    }

    public void OnClickGameStartBtn()
    {
        var heartData = CurrencyDataController.Instance.GetCurrencyData(CurrencyType.Heart);
        
        if(heartData.f_amount <= 0) return;
        
        if (GameManager.Instance.SelectEpisode(currentEpisodeInfoParam.EpisodeData.f_episodeNumber))
        {
            // 클리어한 스테이지의 다음 스테이지 찾기
            int nextStageNumber = currentEpisodeInfoParam.UserBestRecordStage + 1;

            // 해당 에피소드의 스테이지 중 다음 스테이지 가져오기
            var nextStage = D_StageData.FindEntity(
                s => s.f_EpisodeData.Id == currentEpisodeInfoParam.EpisodeData.Id && s.f_StageNumber == nextStageNumber
            );
          
            // 스테이지가 있으면 바로 인게임으로 진입
            if (nextStage != null)
            {
                GameManager.Instance.SelectedStageNumber = nextStage.f_StageNumber;

                CurrencyDataController.Instance.AddCurrency(CurrencyType.Heart, -1);

                GameSceneManager.Instance.LoadScene(SceneKind.InGame);
            }
        }
    }

    private int GetUserBestRecordStageByEpisode(D_EpisodeData episodeData)
    {
        var userData = D_LocalUserData.GetEntity(0);

        if (Mathf.Abs(userData.f_clearEpisodeNumber - episodeData.f_episodeNumber) == 1)
        {
            return userData.f_lastClearedStageNumber;
        }

        if (episodeData.f_episodeNumber <= userData.f_clearEpisodeNumber)
        {
            var stageDataList = D_StageData.GetEntitiesByKeyEpisodeKey(episodeData);
            if (stageDataList != null && stageDataList.Count > 0)
            {
                return stageDataList.Count;
            }
        }

        return 0;
    }
}

public class EpisodeInfoParam
{
    public D_EpisodeData EpisodeData { get; }
    public int UserBestRecordStage { get; }
    public int MaxStageCount { get; }
    
    public bool CanPlay { get; }

    public EpisodeInfoParam(D_EpisodeData episodeData, int userBestRecordStage, int maxStageCount, bool canPlay)
    {
        EpisodeData = episodeData;
        UserBestRecordStage = userBestRecordStage;
        MaxStageCount = maxStageCount;
        CanPlay = canPlay;
    }
}
