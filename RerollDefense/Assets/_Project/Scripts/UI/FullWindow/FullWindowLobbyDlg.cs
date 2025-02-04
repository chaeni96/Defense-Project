using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("FullWindowLobbyDlg", "FullWindowLobbyDlg", true)]
public class FullWindowLobbyDlg : FullWindowBase
{
    [SerializeField] private SwipeUI swipeUI;
    [SerializeField] private Transform contentTransform;
    [SerializeField] private StageInfoUI stageInfoPrefab;

    public override void InitializeUI()
    {
        base.InitializeUI();
        CreateStageInfo();
        swipeUI.InitializeSwipe();
    }

    private void CreateStageInfo()
    {
        var stages = D_StageData.FindEntities(null);

        foreach (var stage in stages)
        {
            var stageInfo = Instantiate(stageInfoPrefab, contentTransform);
            stageInfo.SetStageInfo(stage.f_StageNumber);
        }
    }
}
