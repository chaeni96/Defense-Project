using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("FullWindowLobbyDlg", "FullWindowLobbyDlg", true)]
public class FullWindowLobbyDlg : FullWindowBase
{
    public override void InitializeUI()
    {
        base.InitializeUI();
   
    }

  
    public  async void OnclcikDungeonBtn()
    {
        await UIManager.Instance.ShowUI<StageSelectUI>();
    }
}
