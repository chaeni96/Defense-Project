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
    private const float TARGET_CHECK_INTERVAL = 0.2f; // 0.2�ʸ��� Ÿ�� Ȯ��

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
        // �ʿ��� ���� �۾�
    }

    public override void UpdateState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // ���� �������� Ÿ�� Ȯ��
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            CheckForTargets(enemy);
            targetCheckTimer = 0f;
        }
    }

    private void CheckForTargets(Enemy enemy)
    {
        // ���� Ÿ�� ��ġ Ȯ��
        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(enemy.transform.position);

        // ���� ���� ���� Ÿ�ϵ� Ȯ��
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

                    // Base ������ �������� ����
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

        // Ÿ���� ������ ���� ���·� ��ȯ
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
    private const float TARGET_CHECK_INTERVAL = 0.1f; // �� ���� Ÿ�� ���� Ȯ��

    public EnemyAttackState()
    {
        animTrigger = TriggerKeyword.Attack;
    }

    public override void EnterState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // �̵� ����
        enemy.SetReadyToMove(false);
        targetCheckTimer = 0f;
    }

    public override void ExitState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // ���� ��ȯ �� �ִϸ��̼� �÷��� ����
        enemy.isAttackAnimationPlaying = false;
    }

    public override void UpdateState(BasicObject obj)
    {
        Enemy enemy = obj as Enemy;
        if (enemy == null) return;

        // �� ���� Ÿ�� ���� Ȯ��
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            // Ÿ���� ������ų�, �׾��ų�, ������ ������� Ȯ��
            if (enemy.attackTarget == null ||
                !enemy.attackTarget.canAttack ||
                enemy.attackTarget.GetStat(StatName.CurrentHp) <= 0 ||
                Vector2.Distance(enemy.transform.position, enemy.attackTarget.transform.position) > enemy.GetStat(StatName.AttackRange))
            {
                // �̵� ���·� ���ư���
                enemy.attackTarget = null; // Ÿ�� ����
                enemy.ChangeState(new EnemyMoveState());
                return;
            }

            targetCheckTimer = 0f;
        }

        // ���� �õ�
        enemy.AttackTarget();
    }
}