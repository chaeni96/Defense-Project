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
    private bool damageApplied = false; // 데미지 적용 여부

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
        
        // 공격 타이머와 플래그 초기화
        ResetAttack();

        // 애니메이션 길이 체크 초기화
        animLengthChecked = false;
        animCheckTimer = 0f;
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

                // 공격 타이머 리셋 (애니메이션 체크 딜레이 보정)
                attackTimer = 0f;
            }
            return;
        }

        // 공격 타이머 업데이트
        attackTimer += Time.deltaTime;

        // 적절한 시간에 데미지 적용
        if (!damageApplied && attackTimer >= damageApplyTime)
        {
            //공격 메서드 필요
            damageApplied = true;
        }

        // 공격 애니메이션 완료 여부 체크
        if (attackTimer >= attackAnimationLength)
        {
            // 타겟이 여전히 유효하고 사거리 내에 있는지 확인
            // if (IsTargetValid() && IsTargetInRange())
            // {
            //     // 다음 공격을 위한 초기화
            //     ResetAttack();
            //     animLengthChecked = false;
            //     animCheckTimer = 0f;
            // }
            // else
            // {
            //     // 타겟이 더 이상 유효하지 않거나 사거리 밖에 있으면 공격 상태 종료
            //     Controller.RegisterTrigger(Trigger.TargetMiss);
            // }
        }
        
    }
    
    // 현재 애니메이션 길이 가져오기
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) return;

        // 현재 애니메이션 클립 정보 가져오기
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // 애니메이션 길이와 데미지 타이밍 설정
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
        }
    }
    

    // 공격 상태 초기화
    private void ResetAttack()
    {
        attackTimer = 0f;
        damageApplied = false;
    }

    // 타겟이 사거리 내에 있는지 확인
    private bool IsTargetInRange()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
            return false;

        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        return false;
    }

    // 타겟 유효성 체크
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // 현재 타겟이 없으면 새 타겟 찾기
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

        // 타겟이 활성화 상태인지 체크
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
        Debug.Log("공격 상태 종료");
    }
}