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

    private int episodeNumber;
 
    public void SetStageInfo(int episodeNumber)
    {
        this.episodeNumber = episodeNumber;
        episodeName.text = $"Episode {episodeNumber}";
        episodeDesc.text = $"Episode {episodeNumber}에 대한 어쩌구저쩌구..";
        var userData = D_LocalUserData.GetEntity(0);
        bool isUnlocked = episodeNumber == 1 || episodeNumber <= userData.f_clearEpisodeNumber + 1;

        lockIcon.SetActive(!isUnlocked);
        episodeNameBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeDescBg.color = isUnlocked ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        episodeButton.interactable = isUnlocked;
    }

    public void OnClickEpisode()
    {
        GameManager.Instance.SelectEpisode(episodeNumber);
        
        //씬넘어가는게 아니라 해당 에피소드의 스테이지를 보여줘야함
        
        //GameSceneManager.Instance.LoadScene(SceneKind.InGame);
    }
}
