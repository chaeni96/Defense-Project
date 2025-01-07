using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class UIInfoAttribute : Attribute
{
    public string AddressableKey { get; private set; }
    public string ObjectName { get; private set; }
    public bool DestroyOnHide { get; private set; }

    public UIInfoAttribute(string addressableKey, string objectName, bool destroyOnHide)
    {
        AddressableKey = addressableKey;
        ObjectName = objectName;
        DestroyOnHide = destroyOnHide;
    }
}

public class UIBase : MonoBehaviour
{

    protected string addressableKey;
    protected string objectName;
    public bool DestroyOnHide { get; private set; }

    protected virtual void Awake()
    {
        // UIInfoAttribute를 읽어 값 설정
        var uiInfoAttribute = (UIInfoAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(UIInfoAttribute));
        if (uiInfoAttribute != null)
        {
            addressableKey = uiInfoAttribute.AddressableKey;
            objectName = uiInfoAttribute.ObjectName;
            DestroyOnHide = uiInfoAttribute.DestroyOnHide;
        }
        else
        {
            Debug.LogError(this.GetType().Name + " 클래스에 UIInfoAttribute가 없습니다.");
        }
    }

    /// <summary>
    /// UI 초기화 메서드. 자식 클래스에서 오버라이드하여 사용.
    /// </summary>
    public virtual void InitializeUI()
    {
        // 자식 클래스에서의 초기화 로직


    }

    /// <summary>
    /// UI를 숨기는 메서드. DestroyOnHide 값에 따라 Destroy 또는 비활성화.
    /// </summary>
    /// 

    public virtual void HideUI()
    {
        if (gameObject.activeInHierarchy)
        {
            if (DestroyOnHide)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
            return;
        }

    
    }

    //TODO : hide 애니메이션 추가할때 이 메서드로 쓰기
    //public virtual void HideUI()
    //{
    //    // 게임오브젝트가 활성화된 상태일 때만 코루틴 실행
    //    if (gameObject.activeInHierarchy)
    //    {
    //        StartCoroutine(CoHideUIAnimation());
    //    }
    //    else
    //    {
    //        // 이미 비활성화된 상태라면 즉시 처리
    //        if (DestroyOnHide)
    //        {
    //            Destroy(gameObject);
    //        }
    //        else
    //        {
    //            gameObject.SetActive(false);
    //        }
    //    }
    //}


    /// <summary>
    /// 닫는 애니메이션을 재생하고 UI를 숨기는 코루틴.
    /// </summary>
    protected virtual IEnumerator CoHideUIAnimation()
    {
        // 애니메이션 또는 이펙트 재생
        yield return PlayHideAnimation();

        // 오브젝트가 파괴되었는지 확인
        if (this == null || gameObject == null)
        {
            yield break;
        }

        // DestroyOnHide 값에 따라 처리
        if (DestroyOnHide)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 닫는 애니메이션 또는 이펙트를 재생하는 메서드. 자식 클래스에서 오버라이드하여 구현.
    /// </summary>
    protected virtual IEnumerator PlayHideAnimation()
    {
        // 기본적으로는 바로 반환
        yield return null;
    }
}


