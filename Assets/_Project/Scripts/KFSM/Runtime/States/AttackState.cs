using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    // ���� Ÿ�̹� ���
    private const float ATTACK_ANIMATION_DURATION = 1.0f; // �� 60������ = 1��
    private const float SKILL_TRIGGER_TIME = 0.667f; // 40������ = 0.667�� (40/60)
    
    private float attackTimer = 0f;
    private bool skillTransitioned = false;
    
    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("���� ���� ����");
        
        if (characterFSM == null) return;

        // Ÿ�� üũ
        if (!IsTargetValidAndInRange())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }
        
        // �ʱ�ȭ
        attackTimer = 0f;
        skillTransitioned = false;
        
        // ���� �ִϸ��̼� ����
        if (characterFSM.animator != null)
        {
            characterFSM.animator.SetTrigger("Attack");
        }
    }

    public override void OnUpdate()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // Ÿ���� ��ȿ���� ��� üũ
        if (!IsTargetValidAndInRange())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // 40������(0.667��)�� ��ų�� ��ȯ
        if (!skillTransitioned && attackTimer >= SKILL_TRIGGER_TIME)
        {
            Controller.RegisterTrigger(Trigger.SkillRequested);
            skillTransitioned = true;
            Debug.Log($"��ų ��ȯ: {attackTimer}�� (��ǥ: {SKILL_TRIGGER_TIME}��)");
        }
    }
    
    private bool IsTargetValidAndInRange()
    {
        var target = characterFSM.CurrentTarget;
        
        // Ÿ�� ���� �� ���� üũ
        if (target == null || !target.isActive || target.GetStat(StatName.CurrentHp) <= 0)
        {
            return false;
        }
        
        // ��Ÿ� üũ
        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        float distance = Vector2.Distance(characterFSM.transform.position, target.transform.position);
        
        return distance <= attackRange;
    }

    public override void OnExit()
    {
        Debug.Log("���� ���� ����");
    }
}