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

        // ���� �������� UI ����
        foreach (Transform child in contentTransform)
        {
            Destroy(child.gameObject);
        }

        // �ش� ���Ǽҵ��� �������� ������ ��������
        var stages = D_StageData.FindEntities(
            d => d.f_EpisodeData.Id == episodeData.Id,
            sort: (a, b) => a.f_StageNumber.CompareTo(b.f_StageNumber)
        );

        // �������� UI ����
        float totalHeight = stages.Count * 400f + 100f; // �� ī�� ���� 400, ������ ���� ���

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
