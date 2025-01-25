using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[UIInfo("FullWindowLobbyDlg", "FullWindowLobbyDlg", true)]
public class FullWindowLobbyDlg : FullWindowBase
{
    public void OnClickStageButton(int stageNumber)
    {
        if (GameManager.Instance.SelectStage(stageNumber))
        {
            OnClickGamePlayBtn();
        }
        else
        {
            // 스테이지가 해금되지 않았을 때 처리
            Debug.Log($"Stage {stageNumber}에 들어갈수없음");
        }
    }

    public async void OnClickGamePlayBtn()
    {
        await UIManager.Instance.ShowUI<BoosterSelectUI>();
    }
}
