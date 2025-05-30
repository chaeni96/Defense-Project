using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    // 공격 타이밍 상수
    private const float ATTACK_ANIMATION_DURATION = 1.0f; // 총 60프레임 = 1초
    private const float SKILL_TRIGGER_TIME = 0.667f; // 40프레임 = 0.667초 (40/60)
    
    private float attackTimer = 0f;
    private bool skillTransitioned = false;
    
    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("공격 상태 진입");
        
        if (characterFSM == null) return;

        // 타겟 체크
        if (!IsTargetValidAndInRange())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }
        
        // 초기화
        attackTimer = 0f;
        skillTransitioned = false;
        
        // 공격 애니메이션 시작
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

        // 타겟이 유효한지 계속 체크
        if (!IsTargetValidAndInRange())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // 타이머 업데이트
        attackTimer += Time.deltaTime;

        // 40프레임(0.667초)에 스킬로 전환
        if (!skillTransitioned && attackTimer >= SKILL_TRIGGER_TIME)
        {
            Controller.RegisterTrigger(Trigger.SkillRequested);
            skillTransitioned = true;
            Debug.Log($"스킬 전환: {attackTimer}초 (목표: {SKILL_TRIGGER_TIME}초)");
        }
    }
    
    private bool IsTargetValidAndInRange()
    {
        var target = characterFSM.CurrentTarget;
        
        // 타겟 존재 및 생존 체크
        if (target == null || !target.isActive || target.GetStat(StatName.CurrentHp) <= 0)
        {
            return false;
        }
        
        // 사거리 체크
        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        float distance = Vector2.Distance(characterFSM.transform.position, target.transform.position);
        
        return distance <= attackRange;
    }

    public override void OnExit()
    {
        Debug.Log("공격 상태 종료");
    }
}