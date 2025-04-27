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
    private float targetCheckTimer = 0f;
    private const float TARGET_CHECK_INTERVAL = 0.1f; // 더 자주 타겟 상태 확인

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
        targetCheckTimer = 0f;
    }

    public override void ExitState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // 상태 전환 시 애니메이션 플래그 리셋
        enemy.isAttackAnimationPlaying = false;
    }

    public override void UpdateState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // 더 자주 타겟 상태 확인
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            // 타겟이 사라졌거나, 죽었거나, 범위를 벗어났는지 확인
            if (enemy.attackTarget == null ||
                !enemy.attackTarget.canAttack ||
                enemy.attackTarget.GetStat(StatName.CurrentHp) <= 0 ||
                Vector2.Distance(enemy.transform.position, enemy.attackTarget.transform.position) > enemy.GetStat(StatName.AttackRange))
            {
                // 이동 상태로 돌아가기
                enemy.attackTarget = null; // 타겟 제거
                enemy.ChangeState(new EnemyMoveState());
                return;
            }

            targetCheckTimer = 0f;
        }

        // 공격 시도
        enemy.AttackTarget();
    }
}



public class EnemyMoveToKillZoneState : State
{
    public EnemyMoveToKillZoneState()
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


public class EnemyMoveToTargetState : State
{
    private Transform targetUnit;
    private float attackRange;
    private Enemy enemyController;
    private float targetCheckTimer = 0f;
    private const float TARGET_CHECK_INTERVAL = 0.2f; // 0.2초마다 타겟 확인

    public EnemyMoveToTargetState()
    {
        animTrigger = TriggerKeyword.Run;
    }

    public override void EnterState(BasicObject obj)
    {
        obj.animator.CrossFade(animTrigger.ToString(), 0.1f);
        enemyController = obj as Enemy;

        if (enemyController != null)
        {
            // 공격 준비 활성화
            enemyController.SetReadyToMove(false); // 기존 경로 이동 시스템 비활성화
            attackRange = enemyController.GetStat(StatName.AttackRange);
            FindNearestUnit();
        }
    }

    public override void ExitState(BasicObject obj)
    {
    }

    public override void UpdateState(BasicObject obj)
    {
        if (enemyController == null) return;

        // 주기적으로 타겟 확인
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            FindNearestUnit();
            targetCheckTimer = 0f;
        }

        // 타겟이 없으면 종료
        if (targetUnit == null) return;

        // 타겟까지의 거리 계산
        float distanceToTarget = Vector3.Distance(enemyController.transform.position, targetUnit.position);

        // 공격 범위 안에 들어왔으면 공격 상태로 전환
        if (distanceToTarget <= attackRange)
        {
            UnitController unit = targetUnit.GetComponent<UnitController>();
            if (unit != null)
            {
                enemyController.attackTarget = unit;
                enemyController.ChangeState(new EnemyAttackState());
            }
            return;
        }

        // 공격 범위 밖이면 타겟 방향으로 이동
        Vector3 direction = (targetUnit.position - enemyController.transform.position).normalized;
        float moveSpeed = enemyController.GetStat(StatName.MoveSpeed);
        enemyController.transform.position += direction * moveSpeed * Time.deltaTime;

        // 이동 방향에 따라 스프라이트 방향 설정
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            enemyController.spriteRenderer.flipX = direction.x < 0;
        }
    }

    private void FindNearestUnit()
    {
        targetUnit = null;
        float closestDistance = float.MaxValue;

        // 모든 유닛 가져오기
        List<UnitController> units = UnitManager.Instance.GetUnits();

        foreach (var unit in units)
        {
            if (unit == null || !unit.canAttack) continue;

            // 베이스 유닛은 건너뛰기 (원한다면)
            if (unit.unitType == UnitType.Base) continue;

            float distance = Vector3.Distance(enemyController.transform.position, unit.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                targetUnit = unit.transform;
            }
        }
    }
}