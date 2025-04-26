using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    private Enemy parentEnemy;

    private void Awake()
    {
        // 부모에서 Enemy 컴포넌트 찾기
        parentEnemy = GetComponentInParent<Enemy>();
    }

    // 애니메이션 이벤트에서 호출될 메서드
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
