using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class EpisodeInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text episodeName;
    [SerializeField] private TMP_Text episodeDesc;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Image episodeNameBg ;
    [SerializeField] private Image episodeDescBg;
    [SerializeField] private Button episodeButton;

    private D_EpisodeData episodeData;
    private StageSelectUI parentUI;
    public void Initialize(StageSelectUI parent)
    {
        parentUI = parent;
    }

    public void SetEpisodeInfo(D_EpisodeData episode)
    {
        this.episodeData = episode;
        episodeName.text = $"Episode {episodeData.f_episodeNumber} : {episodeData.f_episodeTitle}";
        episodeDesc.text = $"{episodeData.f_episodeDescription}";
        var userData = D_LocalUserData.GetEntity(0);
        bool isUnlocked = episodeData.f_episodeNumber == 1 || episodeData.f_episodeNumber <= userData.f_clearEpisodeNumber + 1;

        lockIcon.SetActive(!isUnlocked);
        episodeNameBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeDescBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeButton.interactable = isUnlocked;
    }

    public async void OnClickEpisode()
    {
        if (GameManager.Instance.SelectEpisode(episodeData.f_episodeNumber))
        {
            // ½ºÅ×ÀÌÁö ¼±ÅÃ UI Ç¥½Ã
            var stageSelectUI = await UIManager.Instance.ShowUI<SelectStagePopup>();
            stageSelectUI.ShowStages(episodeData);
            stageSelectUI.Initialize(parentUI);

            //swipeUIÀÇ update ¸ØÃç¾ßµÊ
            parentUI.PauseSwipeUI();

        }
    }
}
