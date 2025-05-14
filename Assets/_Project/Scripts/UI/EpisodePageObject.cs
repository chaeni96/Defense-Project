using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("EpisodePageObject", "EpisodePageObject", true)]

public class EpisodePageObject : MonoBehaviour
{
    [SerializeField] private TMP_Text episodeName;
    [SerializeField] private TMP_Text episodeDesc;
    [SerializeField] private TMP_Text clearStageNumber;

    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Image episodeNameBg;
    [SerializeField] private Image episodeDescBg;
    [SerializeField] private Image episodeImageBg;


    [SerializeField] private Button episodeButton;
    

    private D_EpisodeData episodeData;
    private D_LocalUserData userData;

    public bool HasEpisode(D_EpisodeData episode)
    {
        return episodeData != null && episode != null && episodeData.Id == episode.Id;
    }

    //클릭한 에피소드의 정보 먼저 보여줘야함
    public void SetEpisodeInfo(D_EpisodeData episode)
    {
        this.episodeData = episode;

        userData = D_LocalUserData.GetEntity(0);

        episodeName.text = $"Episode {episodeData.f_episodeNumber} \n {episodeData.f_episodeTitle}";
        episodeDesc.text = $"{episodeData.f_episodeDescription}";
    
        bool isUnlocked = episodeData.f_episodeNumber == 1 || episodeData.f_episodeNumber <= userData.f_clearEpisodeNumber + 1;
        
        //TODO : 김채현
        //data 수정해야됨 각 에피소드마다 클리어한 넘버 

        if (userData.f_lastClearedStageNumber >= 5)
        {
            clearStageNumber.text = $"올 클리어";
        }
        else
        {
            if(isUnlocked)
            {
                clearStageNumber.text = $"스테이지 돌파 : {userData.f_lastClearedStageNumber} / 5";
            }
            else
            {
                clearStageNumber.text = $"스테이지 돌파 : 0 / 5";
            }
        }

       
        lockIcon.SetActive(!isUnlocked);
        episodeNameBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeDescBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeImageBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeButton.interactable = isUnlocked;
    }

    //에피소드 선택 버튼
    // public async void OnClickSelectEpisodeBtn()
    // {
    //     //EpisodeSelectPopup 닫기
    //     //episodeUI 보여주기 대신에 선택된애가 있으면 얘로 보여주고 아니면 원래 눌렀던 애로
    //
    //     
    //     // EpisodeInfoUI 찾거나 생성
    //     var episodeInfoUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>();
    //
    //     if (episodeInfoUI != null)
    //     {
    //         // 선택한 에피소드 정보 설정
    //         episodeInfoUI.SetEpisodeInfo(episodeData);
    //     }
    //
    //     // EpisodeSelectPopup 닫기
    //     UIManager.Instance.CloseUI<EpisodeSelectPopup>();
    //    
    // }

    public void OnClickCancleBtn()
    {
        UIManager.Instance.CloseUI<EpisodeSelectPopup>();

        //EpisodeSelectPopup 닫기
    }
}
