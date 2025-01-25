using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UIInfo("FieldGameSettingPopup", "FieldGameSettingPopup", false)]
public class FieldGameSettingPopup : FloatingPopupBase
{
    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    public override void HideUI()
    {
        base.HideUI();
    }


    // ���� ����ϱ� ��ư Ŭ�� �ڵ鷯
    public void OnClickContinueGameButton()
    {
        // ���� �Ͻ����� ���¿��� ���� ���·� ����
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ResumeGame();
        }
    }

    // �κ�� ���ư��� ��ư Ŭ�� �ڵ鷯
    public void OnClickReturnToLobbyButton()
    {
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ReturnToLobby();
        }
    }

}
