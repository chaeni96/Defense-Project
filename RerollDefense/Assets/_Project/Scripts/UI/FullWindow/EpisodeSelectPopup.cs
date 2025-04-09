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

    private D_EpisodeData currentEpisode; // ���� ���õ� ���Ǽҵ�

    public override void InitializeUI()
    {
        base.InitializeUI();
        CreateEpisodeInfo();
    }


    public void SetCurrentEpisode(D_EpisodeData episode)
    {
        this.currentEpisode = episode;

        // �̹� �������� ������ ���¶�� ������ �������� �̵�
        if (swipeUI != null && contentTransform.childCount > 0)
        {
            MoveToEpisode(episode);
        }
    }

    private void MoveToEpisode(D_EpisodeData episode)
    {
        // ���� ���Ǽҵ忡 �ش��ϴ� ������ �ε��� ã��
        if (episode == null) return;

        for (int i = 0; i < contentTransform.childCount; i++)
        {
            EpisodePageObject pageObj = contentTransform.GetChild(i).GetComponent<EpisodePageObject>();
            if (pageObj != null && pageObj.HasEpisode(episode))
            {
                // �ش� �������� �̵�
                swipeUI.SetScrollBarValue(i);
                break;
            }
        }
    }

    private void CreateEpisodeInfo()
    {
        var episodes = D_EpisodeData.FindEntities(null);

        // ���Ǽҵ� ��ȣ�� ����
        List<D_EpisodeData> sortedEpisodes = new List<D_EpisodeData>(episodes);
        sortedEpisodes.Sort((a, b) => a.f_episodeNumber.CompareTo(b.f_episodeNumber));

        // ������ ����
        foreach (var episode in sortedEpisodes)
        {
            var episodeInfo = Instantiate(episodePagePrefab, contentTransform);
            episodeInfo.SetEpisodeInfo(episode);
        }

        swipeUI.InitializeSwipe();

        // ���� ���Ǽҵ尡 ������ ��� �ش� �������� �̵�
        if (currentEpisode != null)
        {
            MoveToEpisode(currentEpisode);
        }
        else
        {
            // �⺻������ ù ��° ���Ǽҵ� �Ǵ� 
            // Ŭ������ ���Ǽҵ��� ���� ���Ǽҵ�� �̵�
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
        // SwipeUI�� ������Ʈ ���� ����
        swipeUI.enabled = false;
    }

    public void ResumeSwipeUI()
    {
        // SwipeUI�� ������Ʈ �簳 ����
        swipeUI.enabled = true;
    }
}
