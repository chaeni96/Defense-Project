using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : PlacedObject
{

    public LayerMask targetLayer; // Ÿ������ �� Layer

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
        //���� �����Ÿ� �ȿ� �ִ� ������Ʈ �� ���� ����� ������Ʈ ����
        Collider2D[] targets = Physics2D.OverlapCircleAll(myBody.position, attackRange, targetLayer);
        float nearestDist = Mathf.Infinity;
        DamageableObject nearestTarget = null;

        foreach (var detectTarget in targets)
        {
            DamageableObject detectedUnit = detectTarget.GetComponent<DamageableObject>();

            if (detectedUnit != null)
            {
                Rigidbody2D targetRb = detectTarget.GetComponent<Rigidbody2D>();
                if (targetRb == null) continue; // Rigidbody2D�� ���� ��� ����

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
            ChangeState(new UnitAttackState()); // ���� ã���� ���� ���·� ��ȯ
        }
    }

    public void ClearTarget()
    {
        targetObject = null;
        ChangeState(new UnitIdleState()); // Ÿ���� ������ ��� ���·� ��ȯ
    }
}
