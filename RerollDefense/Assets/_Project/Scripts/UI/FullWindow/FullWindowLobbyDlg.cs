using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullWindowLobbyDlg : MonoBehaviour
{
    public async void OnClickGamePlayBtn()
    {
        await UIManager.Instance.ShowUI<BoosterSelectUI>();
    }
}
