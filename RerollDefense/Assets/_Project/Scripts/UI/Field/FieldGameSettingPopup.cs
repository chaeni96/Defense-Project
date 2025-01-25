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


    // 게임 계속하기 버튼 클릭 핸들러
    public void OnClickContinueGameButton()
    {
        // 현재 일시정지 상태에서 이전 상태로 복귀
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ResumeGame();
        }
    }

    // 로비로 돌아가기 버튼 클릭 핸들러
    public void OnClickReturnToLobbyButton()
    {
        if (GameManager.Instance.currentState is GamePauseState pauseState)
        {
            pauseState.ReturnToLobby();
        }
    }

}
