using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyIdleState : State
{

    public EnemyIdleState()
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
    }
}

public class EnemyMoveState : State
{
    private float targetCheckTimer = 0f;
    private const float TARGET_CHECK_INTERVAL = 0.2f; // 0.2초마다 타겟 확인

    public EnemyMoveState()
    {
        animTrigger = TriggerKeyword.Run;
    }

    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);

        Enemy enemy = obj as Enemy;
        if (enemy != null)
        {
            enemy.SetReadyToMove(true);
            targetCheckTimer = 0f;
        }
    }

    public override void ExitState(BasicObject obj)
    {
        // 필요한 정리 작업
    }

    public override void UpdateState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // 일정 간격으로 타겟 확인
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            CheckForTargets(enemy);
            targetCheckTimer = 0f;
        }
    }

    private void CheckForTargets(Enemy enemy)
    {
        // 현재 타일 위치 확인
        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(enemy.transform.position);

        // 공격 범위 내의 타일들 확인
        int rangeInt = Mathf.CeilToInt(enemy.GetStat(StatName.AttackRange));
        UnitController closestTarget = null;
        float closestDistance = float.MaxValue;

        for (int x = -rangeInt; x <= rangeInt; x++)
        {
            for (int y = -rangeInt; y <= rangeInt; y++)
            {
                Vector2 checkTilePos = new Vector2(currentTilePos.x + x, currentTilePos.y + y);
                TileData tileData = TileMapManager.Instance.GetTileData(checkTilePos);

                if (tileData != null && tileData.placedUnit != null && tileData.placedUnit.canAttack)
                {

                    // Base 유닛은 공격하지 않음
                    if (tileData.placedUnit.unitType == UnitType.Base)
                        continue;


                    float distance = Vector2.Distance(enemy.transform.position, tileData.placedUnit.transform.position);
                    if (distance <= enemy.GetStat(StatName.AttackRange) && distance < closestDistance)
                    {
                        closestTarget = tileData.placedUnit;
                        closestDistance = distance;
                    }
                }
            }
        }

        // 타겟이 있으면 공격 상태로 전환
        if (closestTarget != null)
        {
            enemy.attackTarget = closestTarget;
            enemy.ChangeState(new EnemyAttackState());
        }
    }
}

public class EnemyAttackState : State
{
    public EnemyAttackState()
    {
        animTrigger = TriggerKeyword.Attack;
    }

    public override void EnterState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // 이동 중지
        enemy.SetReadyToMove(false);
    }

    public override void ExitState(BasicObject obj)
    {
        // 필요한 정리 작업
    }

    public override void UpdateState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // 타겟이 사라졌거나 범위를 벗어났는지 확인
        if (enemy.attackTarget == null || !enemy.attackTarget.canAttack ||
            Vector2.Distance(enemy.transform.position, enemy.attackTarget.transform.position) > enemy.GetStat(StatName.AttackRange))
        {
            // 이동 상태로 돌아가기
            enemy.ChangeState(new EnemyMoveState());
            return;
        }

        // 공격 시도
        enemy.AttackTarget();
    }
}