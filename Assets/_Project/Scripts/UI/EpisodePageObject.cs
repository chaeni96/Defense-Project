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

    //Ŭ���� ���Ǽҵ��� ���� ���� ���������
    public void SetEpisodeInfo(D_EpisodeData episode)
    {
        this.episodeData = episode;

        userData = D_LocalUserData.GetEntity(0);

        episodeName.text = $"Episode {episodeData.f_episodeNumber} \n {episodeData.f_episodeTitle}";
        episodeDesc.text = $"{episodeData.f_episodeDescription}";
    
        bool isUnlocked = episodeData.f_episodeNumber == 1 || episodeData.f_episodeNumber <= userData.f_clearEpisodeNumber + 1;
        
        //TODO : ��ä��
        //data �����ؾߵ� �� ���Ǽҵ帶�� Ŭ������ �ѹ� 

        if (userData.f_lastClearedStageNumber >= 5)
        {
            clearStageNumber.text = $"�� Ŭ����";
        }
        else
        {
            if(isUnlocked)
            {
                clearStageNumber.text = $"�������� ���� : {userData.f_lastClearedStageNumber} / 5";
            }
            else
            {
                clearStageNumber.text = $"�������� ���� : 0 / 5";
            }
        }

       
        lockIcon.SetActive(!isUnlocked);
        episodeNameBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeDescBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeImageBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeButton.interactable = isUnlocked;
    }

    //���Ǽҵ� ���� ��ư
    // public async void OnClickSelectEpisodeBtn()
    // {
    //     //EpisodeSelectPopup �ݱ�
    //     //episodeUI �����ֱ� ��ſ� ���õȾְ� ������ ��� �����ְ� �ƴϸ� ���� ������ �ַ�
    //
    //     
    //     // EpisodeInfoUI ã�ų� ����
    //     var episodeInfoUI = await UIManager.Instance.ShowUI<EpisodeInfoUI>();
    //
    //     if (episodeInfoUI != null)
    //     {
    //         // ������ ���Ǽҵ� ���� ����
    //         episodeInfoUI.SetEpisodeInfo(episodeData);
    //     }
    //
    //     // EpisodeSelectPopup �ݱ�
    //     UIManager.Instance.CloseUI<EpisodeSelectPopup>();
    //    
    // }

    public void OnClickCancleBtn()
    {
        UIManager.Instance.CloseUI<EpisodeSelectPopup>();

        //EpisodeSelectPopup �ݱ�
    }
}
