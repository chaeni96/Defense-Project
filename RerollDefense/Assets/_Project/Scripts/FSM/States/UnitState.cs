using System.Collections;
using UnityEditor;
using UnityEngine;

public class UnitIdleState : State
{
    //적 탐지 간격 두기

    private float detectInterval = 0.5f; // 탐지 간격
    private float nextDetectTime = 0f; // 다음 탐지 시간

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

    //TODO : 공격 쿨타임도 데이터로 넣기
    private float attackCooldown = 1.0f; // 공격 간격
    private float attackTimer = 0.0f;

    public UnitAttackState()
    {
    }

    public override void EnterState(BasicObject controller)
    {
        if (controller is UnitController unit)
        {
            unit.isAttacking = true; // 공격 상태 활성화
            attackTimer = 0.0f;      // 타이머 초기화
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
                    unit.ChangeState(new UnitIdleState()); // 타겟이 없으면 Idle 상태로 전환
                    return;
                }


                attackTimer += Time.deltaTime;

                if (attackTimer >= attackCooldown)
                {
                    //각자 해당하는 공격 실행
                    //unit.AttackTarget(); 
                    unit.targetObject.GetComponent<EnemyController>().onDamaged(unit, unit.attack);
                    attackTimer = 0.0f;
                    unit.ChangeState(new UnitIdleState()); // 공격 후 Idle 상태로 전환
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
