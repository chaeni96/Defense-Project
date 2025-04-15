using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[UIInfo("LobbySettingPopup", "LobbySettingPopup", false)]

public class LobbySettingPopup : FloatingPopupBase
{

    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnClickClosePopup()
    {
        UIManager.Instance.CloseUI<LobbySettingPopup>();
    }

    public void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(); // 어플리케이션 종료
#endif
    }

}
