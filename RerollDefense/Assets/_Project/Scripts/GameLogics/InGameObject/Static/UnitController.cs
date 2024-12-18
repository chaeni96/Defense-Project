using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : PlacedObject
{

    public LayerMask targetLayer; // 타겟으로 할 Layer

    public BasicObject targetObject;

    public bool isAttacking;
    public override void Initialize()
    {
        base.Initialize();

        isAttacking = false;
        ChangeState(new UnitIdleState());
    }

    public override void Update()
    {
        base.Update();
    }

    public void DetectEnemy()
    {
        //공격 사정거리 안에 있는 오브젝트 중 가장 가까운 오브젝트 공격
        Collider2D[] targets = Physics2D.OverlapCircleAll(myBody.position, attackRange, targetLayer);
        float nearestDist = Mathf.Infinity;
        DamageableObject nearestTarget = null;

        foreach (var detectTarget in targets)
        {
            DamageableObject detectedUnit = detectTarget.GetComponent<DamageableObject>();

            if (detectedUnit != null)
            {
                Rigidbody2D targetRb = detectTarget.GetComponent<Rigidbody2D>();
                if (targetRb == null) continue; // Rigidbody2D가 없는 경우 무시

                float distance = Vector2.Distance(myBody.position, targetRb.position);
                if (distance < nearestDist)
                {
                    nearestDist = distance;
                    nearestTarget = detectedUnit;
                }
            }
        }

        if (nearestTarget != null)
        {
            targetObject = nearestTarget;
            ChangeState(new UnitAttackState()); // 적을 찾으면 공격 상태로 전환
        }
    }

    public void ClearTarget()
    {
        targetObject = null;
        ChangeState(new UnitIdleState()); // 타겟을 잃으면 대기 상태로 전환
    }
}
