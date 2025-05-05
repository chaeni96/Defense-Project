using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


[UIInfo("FieldGameResultPopup", "FieldGameResultPopup", true)]
public class FieldGameResultPopup : PopupBase
{

    public TMP_Text gameStateText;

    public override void InitializeUI()
    {
        base.InitializeUI();

        gameStateText.text = GameManager.Instance.gameState;
    }


    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnResultButton()
    {
        GameManager.Instance.ChangeState(new GameLobbyState());
    }

}
