using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class UIBase : MonoBehaviour
{

    /// <summary>
    /// UI 초기화 메서드. 자식 클래스에서 오버라이드하여 사용.
    /// </summary>
    public virtual void InitializeUI()
    {
        // 자식 클래스에서의 초기화 로직


    }


    public virtual void CloseUI()
    {

    }
}

