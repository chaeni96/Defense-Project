using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("StageSelectUI", "StageSelectUI", true)]
public class StageSelectUI : FloatingPopupBase
{
    [SerializeField] private SwipeUI swipeUI;
    [SerializeField] private Transform contentTransform;
    [SerializeField] private EpisodeInfoUI episodeInfoPrefab;

    public override void InitializeUI()
    {
        base.InitializeUI();
        CreateEpisodeInfo();
    }

    private void CreateEpisodeInfo()
    {
        var episodes = D_EpisodeData.FindEntities(null);

        foreach (var episode in episodes)
        {
            var episodeInfo = Instantiate(episodeInfoPrefab, contentTransform);
            episodeInfo.SetEpisodeInfo(episode);
        }


        swipeUI.InitializeSwipe();


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
