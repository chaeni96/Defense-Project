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
        swipeUI.InitializeSwipe();
    }

    private void CreateEpisodeInfo()
    {
        var episodes = D_EpisodeData.FindEntities(null);

        foreach (var episode in episodes)
        {
            var episodeInfo = Instantiate(episodeInfoPrefab, contentTransform);
            episodeInfo.Initialize(this); // �θ� UI ����
            episodeInfo.SetEpisodeInfo(episode);
        }
    }

    public void PauseSwipeUI()
    {
        // SwipeUI�� ������Ʈ ���� ����
        swipeUI.enabled = false;
    }

    public void ResumeSwipeUI()
    {
        // SwipeUI�� ������Ʈ �簳 ����
        swipeUI.enabled = true;
    }
}
