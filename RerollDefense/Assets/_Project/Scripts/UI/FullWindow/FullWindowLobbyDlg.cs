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
            // ���������� �رݵ��� �ʾ��� �� ó��
            Debug.Log($"Stage {stageNumber}�� ��������");
        }
    }

    public async void OnClickGamePlayBtn()
    {
        await UIManager.Instance.ShowUI<BoosterSelectUI>();
    }
}
