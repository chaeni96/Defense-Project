using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitIdleState : State
{
    public UnitIdleState()
    {
        animTrigger = TriggerKeyword.Idle;
    }
    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);

    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
        BasicObject enemy = obj as BasicObject;
        if (enemy == null) return;



    }
}


public class UnitMoveState : State
{

    public UnitMoveState()
    {
        animTrigger = TriggerKeyword.Run;
    }
    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);

    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
    }
}
public class UnitAttackState : State
{
    private float targetCheckTimer = 0f;
    private const float TARGET_CHECK_INTERVAL = 0.2f; // 0.2초마다 타겟 확인
    private UnitController unitController;

    public UnitAttackState()
    {
        animTrigger = TriggerKeyword.Attack;
    }

    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);
        unitController = obj as UnitController;
    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
        if (unitController == null) return;

        // 주기적으로 타겟 확인
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            // 주변에 적이 있는지 확인
            bool hasTarget = CheckForTargets();

            // 타겟이 없으면 Idle 상태로 전환
            if (!hasTarget)
            {
                unitController.ChangeState(new UnitIdleState());
            }

            targetCheckTimer = 0f;
        }
    }

    private bool CheckForTargets()
    {
        // 공격 범위 내에 적이 있는지 확인
        List<Enemy> enemies = EnemyManager.Instance.GetAllEnemys();
        if (enemies.Count == 0) return false;

        float attackRange = unitController.GetStat(StatName.AttackRange);

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(unitController.transform.position, enemy.transform.position);

            if (distance <= attackRange)
            {
                return true; // 공격 범위 내에 적이 있음
            }
        }

        return false; // 공격 범위 내에 적이 없음
    }
}

public class UniMoveToKillZoneState : State
{
    public UniMoveToKillZoneState()
    {
        animTrigger = TriggerKeyword.Run;
    }
    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);

    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
    }
}

public class UnitMoveToTargetState : State
{
    private Transform targetEnemy;
    private float attackRange;
    private UnitController unitController;

    public UnitMoveToTargetState()
    {
        animTrigger = TriggerKeyword.Run;
    }

    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);
        unitController = obj as UnitController;

        if (unitController != null)
        {
            attackRange = unitController.GetStat(StatName.AttackRange);
            FindNearestEnemy();
        }
    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
        if (unitController == null) return;

        // 타겟이 없거나 파괴되었으면 새 타겟 찾기
        if (targetEnemy == null)
        {
            FindNearestEnemy();
            if (targetEnemy == null) return; // 타겟이 없으면 종료
        }

        // 타겟까지의 거리 계산
        float distanceToTarget = Vector3.Distance(unitController.transform.position, targetEnemy.position);

        // 공격 범위 안에 들어왔으면 공격 상태로 전환
        if (distanceToTarget <= attackRange)
        {
            unitController.ChangeState(new UnitAttackState());
            return;
        }

        // 아직 공격 범위 밖이면 타겟 방향으로 이동
        Vector3 direction = (targetEnemy.position - unitController.transform.position).normalized;
        float moveSpeed = unitController.GetStat(StatName.MoveSpeed);
        unitController.transform.position += direction * moveSpeed * Time.deltaTime;

        // 이동 방향에 따라 스프라이트 방향 설정 (왼쪽/오른쪽)
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            unitController.unitSprite.flipX = direction.x < 0;
        }
    }

    private void FindNearestEnemy()
    {
        targetEnemy = null;
        float closestDistance = float.MaxValue;

        // EnemyManager에서 모든 적 가져오기
        List<Enemy> enemies = EnemyManager.Instance.GetAllEnemys();

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(unitController.transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetEnemy = enemy.transform;
            }
        }
    }
}