using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

/// <summary>
/// ���� ����� �� Ÿ������ ����    
/// </summary>
[FSMContextFolder("Create/State/Targeting")]
public class TargetingNearestState : StateBase
{
    private float targetCheckInterval = 0.2f;  // Ÿ�� �� ���� üũ ����
    private float lastTargetCheckTime = 0f;

    // �ִϸ��̼� ���� ����
    private float attackAnimationLength = 1.0f;  // �⺻��
    private float damageApplyTime = 0.5f;        // �⺻��
    private float attackTimer = 0f;              // ���� Ÿ�̸�
    private bool damageApplied = false;          // ������ ���� ����

    // �ִϸ��̼� ���� üũ ���� ����
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f;        // �ִϸ��̼� ���� Ȯ���� ���� ���� �ð�
    private float animCheckTimer = 0f;           // �ִϸ��̼� üũ Ÿ�̸�

    // �ִϸ��̼� ���� ���
    private const float DAMAGE_TIMING_RATIO = 0.4f;
    private const int LAYER_INDEX = 0;

    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("NearestTargetState ����");
        lastTargetCheckTime = 0f;

        // Owner�� CharacterFSMObject���� Ȯ��
        if (characterFSM == null) return;

        // ���� ����� �� ã��
        FindNearestTarget();

        // Ÿ���� ������ Ÿ�� �̽� Ʈ���� �߻�
        if (characterFSM.CurrentTarget == null)
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // ���� Ÿ�̸� �� ���� �ʱ�ȭ
        ResetAttack();

        // �ִϸ��̼� üũ ���� �ʱ�ȭ
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject Ȯ��
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.AttackFinished);
            return;
        }

        // �ִϸ��̼� ���� üũ�� �Ϸ���� �ʾҴٸ�
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // ������ �ð� �� �ִϸ��̼� ���� Ȯ��
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // Ÿ�̸� ���� (�ִϸ��̼� ���̰� ��Ȯ�� �Ǵܵ� �� ī��Ʈ)
                attackTimer = 0f;
            }
            return;
        }

        // ���� Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // ������ ���� ������ �����߰�, ���� �������� ������� �ʾ�����
        if (!damageApplied && attackTimer >= damageApplyTime)
        {

            //TODO : �ó��� �б⹮ �ʿ�

            // ���� ���·� ����
            Controller.RegisterTrigger(Trigger.TargetSelected);
            damageApplied = true;
        }

        // ���� �ִϸ��̼��� ��������
        if (attackTimer >= attackAnimationLength)
        {
            // Ÿ�� ��ȿ�� �� ���� Ȯ��
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
                // Ÿ���� ���ų� ���� ���̸� ���� ���� Ʈ����
                Controller.RegisterTrigger(Trigger.AttackFinished);
            }
        }

        // �ֱ������� Ÿ�� ��ȿ�� �� ���� üũ
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            if (!IsTargetValid())
            {
                return;
            }

            // ���� üũ
            if (!IsTargetInRange())
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    // ���� ����� ���� ã�� �޼���
    private void FindNearestTarget()
    {
        if (characterFSM == null || characterFSM.basicObject == null) return;

        // BasicObject.GetTarget() �޼���� �̹� ���� ����� ���� ��ȯ��
        BasicObject nearestTarget = characterFSM.basicObject.GetNearestTarget();

        // Ÿ�� ��ȿ�� �˻�
        if (nearestTarget != null && (!nearestTarget.isActive || nearestTarget.GetStat(StatName.CurrentHp) <= 0))
        {
            nearestTarget = null;
        }

        // Ÿ�� ����
        characterFSM.CurrentTarget = nearestTarget;

        if (nearestTarget != null)
        {
            Debug.Log($"���� ����� �� Ÿ�� ����: {nearestTarget.name}");
        }
        else
        {
            Debug.Log("��ȿ�� Ÿ���� �����ϴ�.");
        }
    }

    // ���� �ִϸ��̼� ���� Ȯ�� �޼���
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) return;

        // ���� �ִϸ��̼� Ŭ�� ���� ��������
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // ���� ������� Ŭ���� ���� ��������
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
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

    // Ÿ�� ��ȿ�� üũ �޼���
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;
        // Ÿ���� ������ �� Ÿ�� ã��
        if (target == null)
        {
            FindNearestTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ ���� ����
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            return true; // �� Ÿ���� ã��
        }

        if (!target.isActive || target.GetStat(StatName.CurrentHp) <= 0)
        {
            // Ÿ���� ��Ȱ��ȭ �����̰ų� ü���� 0 ���ϸ� �� Ÿ�� ã��
            FindNearestTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ���� ������ ���� ����
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // �� Ÿ���� ã������ ���� ���·� ��ȯ
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        return true;
    }

    public override void OnExit()
    {
        Debug.Log("NearestTargetState ����");
    }
}