using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[UIInfo("BoosterSelectUI", "BoosterSelectUI", false)]
public class BoosterSelectUI : FullWindowBase
{
    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnClickPlayBtn()
    {
        GameSceneManager.Instance.LoadScene(SceneKind.InGame);
    }

    public void OnClickBackBtn()
    {
        UIManager.Instance.CloseUI<BoosterSelectUI>();
    }
}
