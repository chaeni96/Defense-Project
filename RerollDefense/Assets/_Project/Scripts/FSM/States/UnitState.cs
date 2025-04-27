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
    private const float TARGET_CHECK_INTERVAL = 0.2f; // 0.2�ʸ��� Ÿ�� Ȯ��
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

        // �ֱ������� Ÿ�� Ȯ��
        targetCheckTimer += Time.deltaTime;
        if (targetCheckTimer >= TARGET_CHECK_INTERVAL)
        {
            // �ֺ��� ���� �ִ��� Ȯ��
            bool hasTarget = CheckForTargets();

            // Ÿ���� ������ Idle ���·� ��ȯ
            if (!hasTarget)
            {
                unitController.ChangeState(new UnitIdleState());
            }

            targetCheckTimer = 0f;
        }
    }

    private bool CheckForTargets()
    {
        // ���� ���� ���� ���� �ִ��� Ȯ��
        List<Enemy> enemies = EnemyManager.Instance.GetAllEnemys();
        if (enemies.Count == 0) return false;

        float attackRange = unitController.GetStat(StatName.AttackRange);

        foreach (Enemy enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(unitController.transform.position, enemy.transform.position);

            if (distance <= attackRange)
            {
                return true; // ���� ���� ���� ���� ����
            }
        }

        return false; // ���� ���� ���� ���� ����
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

        // Ÿ���� ���ų� �ı��Ǿ����� �� Ÿ�� ã��
        if (targetEnemy == null)
        {
            FindNearestEnemy();
            if (targetEnemy == null) return; // Ÿ���� ������ ����
        }

        // Ÿ�ٱ����� �Ÿ� ���
        float distanceToTarget = Vector3.Distance(unitController.transform.position, targetEnemy.position);

        // ���� ���� �ȿ� �������� ���� ���·� ��ȯ
        if (distanceToTarget <= attackRange)
        {
            unitController.ChangeState(new UnitAttackState());
            return;
        }

        // ���� ���� ���� ���̸� Ÿ�� �������� �̵�
        Vector3 direction = (targetEnemy.position - unitController.transform.position).normalized;
        float moveSpeed = unitController.GetStat(StatName.MoveSpeed);
        unitController.transform.position += direction * moveSpeed * Time.deltaTime;

        // �̵� ���⿡ ���� ��������Ʈ ���� ���� (����/������)
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            unitController.unitSprite.flipX = direction.x < 0;
        }
    }

    private void FindNearestEnemy()
    {
        targetEnemy = null;
        float closestDistance = float.MaxValue;

        // EnemyManager���� ��� �� ��������
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