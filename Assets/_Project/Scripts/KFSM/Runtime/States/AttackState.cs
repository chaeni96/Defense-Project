using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    // ���� �ִϸ��̼� Ÿ�̹�
    private float attackAnimationLength = 1.0f; // �⺻��
    private float damageApplyTime = 0.5f; // �⺻��
    private float attackTimer = 0f; // ���� Ÿ�̸�
    private bool damageApplied = false; // ������ ���� ����

    // �ִϸ��̼� ���� üũ
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f; // �ִϸ��̼� üũ ������
    private float animCheckTimer = 0f; // �ִϸ��̼� üũ Ÿ�̸�

    // �ִϸ��̼� Ÿ�̹� ���
    private const float DAMAGE_TIMING_RATIO = 0.4f;
    private const int LAYER_INDEX = 0;
    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("���� ���� ����");
        Debug.Log($"���� Ÿ�� hp : {characterFSM.CurrentTarget.GetStat(StatName.CurrentHp)}");

        // ĳ���� FSM ���� Ȯ��
        if (characterFSM == null) return;
        
        // ���� Ÿ�̸ӿ� �÷��� �ʱ�ȭ
        ResetAttack();

        // �ִϸ��̼� ���� üũ �ʱ�ȭ
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // ĳ���� FSM ���� Ȯ��
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.AttackFinished);
            return;
        }

        // ���� üũ���� �ʾҴٸ� �ִϸ��̼� ���� üũ
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // ������ �� �ִϸ��̼� ���� ��������
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // ���� Ÿ�̸� ���� (�ִϸ��̼� üũ ������ ����)
                attackTimer = 0f;
            }
            return;
        }

        // ���� Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // ������ �ð��� ������ ����
        if (!damageApplied && attackTimer >= damageApplyTime)
        {
            //���� �޼��� �ʿ�
            damageApplied = true;
        }

        // ���� �ִϸ��̼� �Ϸ� ���� üũ
        if (attackTimer >= attackAnimationLength)
        {
            // Ÿ���� ������ ��ȿ�ϰ� ��Ÿ� ���� �ִ��� Ȯ��
            // if (IsTargetValid() && IsTargetInRange())
            // {
            //     // ���� ������ ���� �ʱ�ȭ
            //     ResetAttack();
            //     animLengthChecked = false;
            //     animCheckTimer = 0f;
            // }
            // else
            // {
            //     // Ÿ���� �� �̻� ��ȿ���� �ʰų� ��Ÿ� �ۿ� ������ ���� ���� ����
            //     Controller.RegisterTrigger(Trigger.TargetMiss);
            // }
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
            // �ִϸ��̼� ���̿� ������ Ÿ�̹� ����
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
        }
    }
    

    // ���� ���� �ʱ�ȭ
    private void ResetAttack()
    {
        attackTimer = 0f;
        damageApplied = false;
    }

    // Ÿ���� ��Ÿ� ���� �ִ��� Ȯ��
    private bool IsTargetInRange()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
            return false;

        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        return false;
    }

    // Ÿ�� ��ȿ�� üũ
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // ���� Ÿ���� ������ �� Ÿ�� ã��
        if (target == null)
        {
            //characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
            return true;
        }

        // Ÿ���� Ȱ��ȭ �������� üũ
        if (!target.isActive)
        {
           // characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        return true;
    }

    public override void OnExit()
    {
        Debug.Log("���� ���� ����");
    }
}