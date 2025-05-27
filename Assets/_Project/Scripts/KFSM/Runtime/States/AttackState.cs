using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    // 공격 애니메이션 타이밍
    private float attackAnimationLength = 1.0f; // 기본값
    private float damageApplyTime = 0.5f; // 기본값
    private float attackTimer = 0f; // 공격 타이머
    private bool skillTransitioned = false; // 스킬 전환 여부

    // 애니메이션 길이 체크
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f; // 애니메이션 체크 딜레이
    private float animCheckTimer = 0f; // 애니메이션 체크 타이머

    // 애니메이션 타이밍 상수
    private const float DAMAGE_TIMING_RATIO = 0.4f;
    private const int LAYER_INDEX = 0;
    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("공격 상태 진입");
        Debug.Log($"현재 타겟 hp : {characterFSM.CurrentTarget.GetStat(StatName.CurrentHp)}");

        // 캐릭터 FSM 존재 확인
        if (characterFSM == null) return;

        if (characterFSM.CurrentTarget == null)
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
        }
        // 공격 타이머와 플래그, 애니메이션 관련 초기화
        ResetAll();
    }

    public override void OnUpdate()
    {
        // 캐릭터 FSM 존재 확인
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.AttackFinished);
            return;
        }

         // 아직 체크하지 않았다면 애니메이션 길이 체크
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // 딜레이 후 애니메이션 길이 가져오기
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;
                attackTimer = 0f; // 타이머 리셋
            }
            return;
        }

        // 공격 타이머 업데이트
        attackTimer += Time.deltaTime;

        // 적절한 시간에 스킬로 전환
        if (!skillTransitioned && attackTimer >= damageApplyTime)
        {
            // 타겟 유효성 체크
            if (IsTargetValidAndInRange())
            {
                Controller.RegisterTrigger(Trigger.SkillRequested);
                skillTransitioned = true;
            }
            else
            {
                // 타겟이 없거나 사거리 밖
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
        
    }
    
    // 타겟 유효성 및 사거리 체크
    private bool IsTargetValidAndInRange()
    {
        var target = characterFSM.CurrentTarget;
        
        // 타겟 존재 및 생존 체크
        if (target == null || target.GetStat(StatName.CurrentHp) <= 0)
        {
            return false;
        }
        
        // 사거리 체크
        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        float distance = Vector2.Distance(characterFSM.transform.position, target.transform.position);
        
        return distance <= attackRange;
    }
    
    // 현재 애니메이션 길이 가져오기
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) 
        {
            // 애니메이터가 없으면 기본값 사용
            return;
        }

        // 현재 애니메이션 클립 정보 가져오기
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // 애니메이션 길이와 데미지 타이밍 설정
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
            
            Debug.Log($"공격 애니메이션 길이: {attackAnimationLength}, 스킬 전환 타이밍: {damageApplyTime}");
        }
    }

    // 모든 상태 초기화 (중요!)
    private void ResetAll()
    {
        // 타이머 초기화
        attackTimer = 0f;
        animCheckTimer = 0f;
        
        // 플래그 초기화
        skillTransitioned = false;
        animLengthChecked = false;
        
        // 애니메이션 길이 초기화 (기본값)
        attackAnimationLength = 1.0f;
        damageApplyTime = 0.5f;
    }
    

    public override void OnExit()
    {
        Debug.Log("공격 상태 종료");
    }
}