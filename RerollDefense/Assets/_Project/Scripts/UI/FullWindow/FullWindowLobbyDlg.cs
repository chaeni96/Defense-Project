using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FullWindowLobbyDlg : MonoBehaviour
{
    public void OnClickGamePlayBtn()
    {
        GameSceneManager.Instance.LoadScene(SceneKind.InGame);

    }
}
