using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameResultUI : PopupBase
{

    public TMP_Text gameStateText;

    public override void InitializeUI()
    {
        base.InitializeUI();

        gameStateText.text = GameManager.Instance.gameState;
    }


    public override void CloseUI()
    {
        base.CloseUI();
    }

    public void OnResultButton()
    {
        #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
        #else
                    Application.Quit();
        #endif
    }

}
