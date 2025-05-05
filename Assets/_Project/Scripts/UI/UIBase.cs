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
        // UIInfoAttribute�� �о� �� ����
        var uiInfoAttribute = (UIInfoAttribute)Attribute.GetCustomAttribute(this.GetType(), typeof(UIInfoAttribute));
        if (uiInfoAttribute != null)
        {
            addressableKey = uiInfoAttribute.AddressableKey;
            objectName = uiInfoAttribute.ObjectName;
            DestroyOnHide = uiInfoAttribute.DestroyOnHide;
        }
        else
        {
            Debug.LogError(this.GetType().Name + " Ŭ������ UIInfoAttribute�� �����ϴ�.");
        }
    }

    /// <summary>
    /// UI �ʱ�ȭ �޼���. �ڽ� Ŭ�������� �������̵��Ͽ� ���.
    /// </summary>
    public virtual void InitializeUI()
    {
        // �ڽ� Ŭ���������� �ʱ�ȭ ����


    }

    /// <summary>
    /// UI�� ����� �޼���. DestroyOnHide ���� ���� Destroy �Ǵ� ��Ȱ��ȭ.
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

    //TODO : hide �ִϸ��̼� �߰��Ҷ� �� �޼���� ����
    //public virtual void HideUI()
    //{
    //    // ���ӿ�����Ʈ�� Ȱ��ȭ�� ������ ���� �ڷ�ƾ ����
    //    if (gameObject.activeInHierarchy)
    //    {
    //        StartCoroutine(CoHideUIAnimation());
    //    }
    //    else
    //    {
    //        // �̹� ��Ȱ��ȭ�� ���¶�� ��� ó��
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
    /// �ݴ� �ִϸ��̼��� ����ϰ� UI�� ����� �ڷ�ƾ.
    /// </summary>
    protected virtual IEnumerator CoHideUIAnimation()
    {
        // �ִϸ��̼� �Ǵ� ����Ʈ ���
        yield return PlayHideAnimation();

        // ������Ʈ�� �ı��Ǿ����� Ȯ��
        if (this == null || gameObject == null)
        {
            yield break;
        }

        // DestroyOnHide ���� ���� ó��
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
    /// �ݴ� �ִϸ��̼� �Ǵ� ����Ʈ�� ����ϴ� �޼���. �ڽ� Ŭ�������� �������̵��Ͽ� ����.
    /// </summary>
    protected virtual IEnumerator PlayHideAnimation()
    {
        // �⺻�����δ� �ٷ� ��ȯ
        yield return null;
    }
}


