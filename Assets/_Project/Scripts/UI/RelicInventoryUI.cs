using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UIInfo("RelicInventoryUI", "RelicInventoryUI", false)]

public class RelicInventoryUI : FloatingPopupBase
{
    private FullWindowLobbyDlg lobbyDlg;

    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    public void InitLobbyDlg(FullWindowLobbyDlg lobby)
    {
        lobbyDlg = lobby;
    }

    public override void HideUI()
    {
        base.HideUI();
    }


    public void OnClickCancelBtn()
    {
        UIManager.Instance.CloseUI<RelicInventoryUI>();
        lobbyDlg.SwitchToCampPanel();
    }
}
