using System.Collections;
using UnityEditor;
using UnityEngine;

public class UnitIdleState : State
{
    //�� Ž�� ���� �α�

    private float detectInterval = 0.5f; // Ž�� ����
    private float nextDetectTime = 0f; // ���� Ž�� �ð�

    public UnitIdleState()
    {
    }

    public override void EnterState(BasicObject controller)
    {
        if(controller is UnitController unit)
        {
            unit.isAttacking = false;
        }
    }

    public override void UpdateState(BasicObject controller)
    {
        if(controller is UnitController unit)
        {
            if(unit != null)
            {
                if (Time.time >= nextDetectTime)
                {
                    nextDetectTime = Time.time + detectInterval;
                    
                    unit.DetectEnemy();

                    if (unit.targetObject != null)
                    {
                        unit.ChangeState(new UnitAttackState());
                    }
                }
            }
        }
        
    }

    public override void ExitState(BasicObject controller)
    {
    }
}

public class UnitAttackState : State
{

    //TODO : ���� ��Ÿ�ӵ� �����ͷ� �ֱ�
    private float attackCooldown = 1.0f; // ���� ����
    private float attackTimer = 0.0f;

    public UnitAttackState()
    {
    }

    public override void EnterState(BasicObject controller)
    {
        if (controller is UnitController unit)
        {
            unit.isAttacking = true; // ���� ���� Ȱ��ȭ
            attackTimer = 0.0f;      // Ÿ�̸� �ʱ�ȭ
        }

    }

    public override void UpdateState(BasicObject controller)
    {

        if (controller is UnitController unit)
        {
            if (unit != null)
            {

                if (unit.targetObject == null || Vector2.Distance(unit.myBody.position, unit.targetObject.myBody.position) > unit.attackRange )
                {
                    unit.ChangeState(new UnitIdleState()); // Ÿ���� ������ Idle ���·� ��ȯ
                    return;
                }


                attackTimer += Time.deltaTime;

                if (attackTimer >= attackCooldown)
                {
                    //���� �ش��ϴ� ���� ����
                    //unit.AttackTarget(); 
                    unit.targetObject.GetComponent<EnemyController>().onDamaged(unit, unit.attack);
                    attackTimer = 0.0f;
                    unit.ChangeState(new UnitIdleState()); // ���� �� Idle ���·� ��ȯ
                }


            }
            else
            {
                unit?.ClearTarget();
            }
        }
    }

    public override void ExitState(BasicObject controller)
    {

    }
}
