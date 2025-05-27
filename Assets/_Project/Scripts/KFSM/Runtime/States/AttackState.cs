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
    private bool skillTransitioned = false; // ��ų ��ȯ ����

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

        if (characterFSM.CurrentTarget == null)
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
        }
        // ���� Ÿ�̸ӿ� �÷���, �ִϸ��̼� ���� �ʱ�ȭ
        ResetAll();
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
                attackTimer = 0f; // Ÿ�̸� ����
            }
            return;
        }

        // ���� Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // ������ �ð��� ��ų�� ��ȯ
        if (!skillTransitioned && attackTimer >= damageApplyTime)
        {
            // Ÿ�� ��ȿ�� üũ
            if (IsTargetValidAndInRange())
            {
                Controller.RegisterTrigger(Trigger.SkillRequested);
                skillTransitioned = true;
            }
            else
            {
                // Ÿ���� ���ų� ��Ÿ� ��
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
        
    }
    
    // Ÿ�� ��ȿ�� �� ��Ÿ� üũ
    private bool IsTargetValidAndInRange()
    {
        var target = characterFSM.CurrentTarget;
        
        // Ÿ�� ���� �� ���� üũ
        if (target == null || target.GetStat(StatName.CurrentHp) <= 0)
        {
            return false;
        }
        
        // ��Ÿ� üũ
        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        float distance = Vector2.Distance(characterFSM.transform.position, target.transform.position);
        
        return distance <= attackRange;
    }
    
    // ���� �ִϸ��̼� ���� ��������
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) 
        {
            // �ִϸ����Ͱ� ������ �⺻�� ���
            return;
        }

        // ���� �ִϸ��̼� Ŭ�� ���� ��������
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // �ִϸ��̼� ���̿� ������ Ÿ�̹� ����
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
            
            Debug.Log($"���� �ִϸ��̼� ����: {attackAnimationLength}, ��ų ��ȯ Ÿ�̹�: {damageApplyTime}");
        }
    }

    // ��� ���� �ʱ�ȭ (�߿�!)
    private void ResetAll()
    {
        // Ÿ�̸� �ʱ�ȭ
        attackTimer = 0f;
        animCheckTimer = 0f;
        
        // �÷��� �ʱ�ȭ
        skillTransitioned = false;
        animLengthChecked = false;
        
        // �ִϸ��̼� ���� �ʱ�ȭ (�⺻��)
        attackAnimationLength = 1.0f;
        damageApplyTime = 0.5f;
    }
    

    public override void OnExit()
    {
        Debug.Log("���� ���� ����");
    }
}