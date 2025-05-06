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
    /// ��� �˾� �ݱ��ư ���ε��� �޼���
    /// ��� PopupBase�� ��ӹ��� �˾��� �ݱ� ��ư�� OnClickClose�� ���ε��ϸ� ��
    /// ���� Close ������ OnClose Override �ؼ� ���..
    /// </summary>
    public void OnClickClose()
    {
        OnClose();
    }

    /// <summary>
    /// �˾��� ���� �� ���� �� ����
    /// </summary>
    protected virtual void OnClose()
    {
        HideUI();
    }
}
