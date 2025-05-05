using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("EpisodeSelectPopup", "EpisodeSelectPopup", true)]
public class EpisodeSelectPopup : FloatingPopupBase
{
    [SerializeField] private SwipeUI swipeUI;
    [SerializeField] private Transform contentTransform;
    [SerializeField] private EpisodePageObject episodePagePrefab;

    private D_EpisodeData currentEpisode; // 현재 선택된 에피소드

    public override void InitializeUI()
    {
        base.InitializeUI();
        CreateEpisodeInfo();
    }


    public void SetCurrentEpisode(D_EpisodeData episode)
    {
        this.currentEpisode = episode;

        // 이미 페이지가 생성된 상태라면 적절한 페이지로 이동
        if (swipeUI != null && contentTransform.childCount > 0)
        {
            MoveToEpisode(episode);
        }
    }

    private void MoveToEpisode(D_EpisodeData episode)
    {
        // 현재 에피소드에 해당하는 페이지 인덱스 찾기
        if (episode == null) return;

        for (int i = 0; i < contentTransform.childCount; i++)
        {
            EpisodePageObject pageObj = contentTransform.GetChild(i).GetComponent<EpisodePageObject>();
            if (pageObj != null && pageObj.HasEpisode(episode))
            {
                // 해당 페이지로 이동
                swipeUI.SetScrollBarValue(i);
                break;
            }
        }
    }

    private void CreateEpisodeInfo()
    {
        var episodes = D_EpisodeData.FindEntities(null);

        // 에피소드 번호로 정렬
        List<D_EpisodeData> sortedEpisodes = new List<D_EpisodeData>(episodes);
        sortedEpisodes.Sort((a, b) => a.f_episodeNumber.CompareTo(b.f_episodeNumber));

        // 페이지 생성
        foreach (var episode in sortedEpisodes)
        {
            var episodeInfo = Instantiate(episodePagePrefab, contentTransform);
            episodeInfo.SetEpisodeInfo(episode);
        }

        swipeUI.InitializeSwipe();

        // 현재 에피소드가 설정된 경우 해당 페이지로 이동
        if (currentEpisode != null)
        {
            MoveToEpisode(currentEpisode);
        }
        else
        {
            // 기본값으로 첫 번째 에피소드 또는 
            // 클리어한 에피소드의 다음 에피소드로 이동
            D_LocalUserData userData = D_LocalUserData.GetEntity(0);
            int nextEpisodeNum = userData.f_clearEpisodeNumber + 1;

            D_EpisodeData nextEpisodeData = null;
            foreach (var episode in sortedEpisodes)
            {
                if (episode.f_episodeNumber == nextEpisodeNum)
                {
                    nextEpisodeData = episode;
                    break;
                }
            }

            if (nextEpisodeData != null)
            {
                MoveToEpisode(nextEpisodeData);
            }
        }
    }

    public void PauseSwipeUI()
    {
        // SwipeUI의 업데이트 중지 로직
        swipeUI.enabled = false;
    }

    public void ResumeSwipeUI()
    {
        // SwipeUI의 업데이트 재개 로직
        swipeUI.enabled = true;
    }
}
