using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("SelectStagePopup", "SelectStagePopup", true)]
public class SelectStagePopup : FloatingPopupBase
{

    [SerializeField] private Transform contentTransform;
    [SerializeField] private StageCardUI stageCardPrefab;
    [SerializeField] private TMP_Text episodeTitleText;

    private EpisodeSelectPopup parentStageSelectUI;
    private D_EpisodeData currentEpisode;

    public void Initialize(EpisodeSelectPopup stageSelectUI)
    {
        parentStageSelectUI = stageSelectUI;
    }

    public void ShowStages(D_EpisodeData episodeData)
    {
        currentEpisode = episodeData;
        episodeTitleText.text = $"Episode {currentEpisode.f_episodeNumber} : {currentEpisode.f_episodeTitle}";

        // 기존 스테이지 UI 제거
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // 해당 에피소드의 스테이지 데이터 가져오기
        var stages = D_StageData.FindEntities(
            d => d.f_EpisodeData.Id == episodeData.Id,
            sort: (a, b) => a.f_StageNumber.CompareTo(b.f_StageNumber)
        );

        // 스테이지 UI 생성
        float totalHeight = stages.Count * 400f + 100f; // 각 카드 높이 400, 개수에 따라 계산

        foreach (var stage in stages)
        {
            var stageCard = Instantiate(stageCardPrefab, contentTransform);
            stageCard.SetStageInfo(stage);
        }

        RectTransform rectTran = contentTransform.GetComponent<RectTransform>();

        rectTran.sizeDelta = new Vector2(rectTran.sizeDelta.x, totalHeight);


    }

    public override void HideUI()
    {
        base.HideUI();

        CleanUp();

    }

    public void OnClickBack()
    {
        parentStageSelectUI.ResumeSwipeUI();
        UIManager.Instance.CloseUI<SelectStagePopup>();
    }

    private void CleanUp()
    {
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }
        currentEpisode = null;
    }
}
