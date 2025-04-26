using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private Enemy parentEnemy;

    private void Awake()
    {
        // �θ𿡼� Enemy ������Ʈ ã��
        parentEnemy = GetComponentInParent<Enemy>();
    }

    // �ִϸ��̼� �̺�Ʈ���� ȣ��� �޼���
    public void ApplyDamage()
    {
        if (parentEnemy != null)
        {
            parentEnemy.ApplyDamage();
        }
    }

    public void OnAttackAnimationEnd()
    {
        if (parentEnemy != null)
        {
            parentEnemy.OnAttackAnimationEnd();
        }
    }
}
