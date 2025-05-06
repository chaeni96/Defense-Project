using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PopupBase : UIBase
{
    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    /// <summary>
    /// 모든 팝업 닫기버튼 바인딩용 메서드
    /// 모든 PopupBase를 상속받은 팝업은 닫기 버튼에 OnClickClose를 바인딩하면 됨
    /// 실제 Close 로직은 OnClose Override 해서 사용..
    /// </summary>
    public void OnClickClose()
    {
        OnClose();
    }

    /// <summary>
    /// 팝업이 닫힐 때 실행 될 로직
    /// </summary>
    protected virtual void OnClose()
    {
        HideUI();
    }
}
