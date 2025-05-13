using System;
using UnityEngine;
using Kylin.FSM;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    private float targetCheckInterval = 0.2f; // Ÿ�� �� ���� üũ �ֱ�
    private float lastTargetCheckTime = 0f;
    private CharacterFSMObject characterFSM;

    // ���� �ִϸ��̼� ���� Ÿ�̸� ����
    private float attackAnimationLength = 1.0f; // �⺻��
    private float damageApplyTime = 0.5f; // �⺻��
    private float attackTimer = 0f; // ���� Ÿ�̸�
    private bool damageApplied = false; // ������ ���� ����

    // �ִϸ��̼� ���� �������� ���� ����
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f; // �ִϸ��̼� ���� �������� �� ��� �ð�
    private float animCheckTimer = 0f; // �ִϸ��̼� üũ Ÿ�̸�

    // �ִϸ��̼� ���� ���
    private const float DAMAGE_TIMING_RATIO = 0.5f; // �ִϸ��̼��� 40% �������� ������ ����
    private const int LAYER_INDEX = 0; // �⺻ �ִϸ��̼� ���̾�

    public override void OnEnter()
    {
        Debug.Log("AttackState ����");
        lastTargetCheckTime = 0f;

        // Owner�� CharacterFSMObject�� ĳ����
        characterFSM = Owner as CharacterFSMObject;
        if (characterFSM == null) return;

        // Ÿ���� ���ٸ� ã��
        if (characterFSM.CurrentTarget == null)
        {
            characterFSM.UpdateTarget();
            // Ÿ���� ������ Chase ���·� 
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }


        // ���� Ÿ�̸� �� ���� �ʱ�ȭ
        ResetAttack();

        // �ִϸ��̼� ���� �������� �ʱ�ȭ
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject Ȯ��
        if (characterFSM == null) return;

        // �ִϸ��̼� ������ ���� �������� �ʾҴٸ�
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // ���� �ð� �� �ִϸ��̼� ���� ��������
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // Ÿ�̸� ���� (�ִϸ��̼� ������ ��Ȯ�� ������ �������� ī��Ʈ)
                attackTimer = 0f;
            }
            return;
        }

        // ���� Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // ������ ���� ������ �����߰� ���� �������� �������� �ʾҴٸ�
        if (!damageApplied && attackTimer >= damageApplyTime)
        {
            // ������ ����
            ApplyDamage();
            damageApplied = true;
        }

        // ���� �ִϸ��̼��� �����ٸ�
        if (attackTimer >= attackAnimationLength)
        {
            // Ÿ�� ��ȿ�� �� �Ÿ� Ȯ��
            if (IsTargetValid() && IsTargetInRange())
            {

                // ���� ���� �ʱ�ȭ 
                ResetAttack();

                // �ִϸ��̼� üũ ���� �ʱ�ȭ
                animLengthChecked = false;
                animCheckTimer = 0f;
            }
            else
            {
                // Ÿ���� ���ų� ���� ���̸� �ٸ� ���·� ��ȯ
                Controller.RegisterTrigger(Trigger.AttackFinished);
            }
        }

        // �ֱ������� Ÿ�� ���¿� ���� Ȯ��
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            // Ÿ�� ��ȿ���� Ȯ��
            if (!IsTargetValid())
            {
                return;
            }

            // ���� ���� üũ
            if (!IsTargetInRange())
            {
                // Ÿ���� ���� ������ ������� �߰� ���·�
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    // ���� �ִϸ��̼� ���� ��������
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) return;

        // ���� �ִϸ��̼� Ŭ�� ���� ��������
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // ���� ��� ���� Ŭ���� ���� ��������
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;

            Debug.Log($"���� ���� �ִϸ��̼� ����:{clipInfo[0].clip.name} {attackAnimationLength:F2}��, ������ ���� ����: {damageApplyTime:F2}��");
        }
        else
        {
            // Ŭ�� ������ �� ã�� ��� - �⺻�� ���
            attackAnimationLength = 1.0f; // �⺻��
            damageApplyTime = 0.4f; // �⺻��
            Debug.LogWarning("���� ��� ���� �ִϸ��̼� Ŭ���� ã�� ���߽��ϴ�. �⺻�� ���");
        }
    }

    // ������ ���� �޼���
    private void ApplyDamage()
    {
        if (characterFSM != null && characterFSM.CurrentTarget != null)
        {
            if (characterFSM.CurrentTarget.isEnemy)
            {
                var enemyObj = characterFSM.CurrentTarget.GetComponent<Enemy>();
                if (enemyObj != null)
                {
                    // ������ ��� �� ����
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    enemyObj.onDamaged(characterFSM.basicObject, damage);
                }
            }
            else
            {
                var unitObj = characterFSM.CurrentTarget.GetComponent<UnitController>();
                if (unitObj != null)
                {
                    // ������ ��� �� ����
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    unitObj.onDamaged(characterFSM.basicObject, damage);
                }
            }
        }
    }

    // ���� ���� �ʱ�ȭ �޼���
    private void ResetAttack()
    {
        attackTimer = 0f;
        damageApplied = false;
    }

    // Ÿ���� ���� ���� ���� �ִ��� Ȯ���ϴ� �޼���
    private bool IsTargetInRange()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
            return false;

        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        return characterFSM.GetDistanceToTarget() <= attackRange;
    }

    // Ÿ�� ��ȿ üũ �޼���
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;
        // Ÿ���� ������ �� Ÿ�� ã��
        if (target == null)
        {
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ ���� ��ȯ
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            Controller.RegisterTrigger(Trigger.AttackRequested);

            return true; // �� Ÿ���� ����
        }

        // Ÿ���� ��Ȱ��ȭ�Ǿ����� Ȯ�� (�׾��ų� Ǯ�� ��ȯ��)
        if (!target.gameObject.activeSelf)
        {
            // Ÿ���� �׾����� �� Ÿ�� ã��
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ ���� ��ȯ
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // �� Ÿ���� ������ Chase ���·�
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        // Ÿ���� ü�� Ȯ�� (�߰���)
        if (target.GetStat(StatName.CurrentHp) <= 0)
        {
            // Ÿ���� �׾����� �� Ÿ�� ã��
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ ���� ��ȯ
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // �� Ÿ���� ������ ���� Ÿ�ٰ� �ٸ��� Chase ���·�
                if (characterFSM.CurrentTarget != target)
                {
                    Controller.RegisterTrigger(Trigger.TargetMiss);
                    return false;
                }
            }
        }

        return true;
    }

    public override void OnExit()
    {
        Debug.Log("AttackState ����");
    }
}