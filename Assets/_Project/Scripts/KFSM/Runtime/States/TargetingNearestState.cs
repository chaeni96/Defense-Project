using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

/// <summary>
/// 가장 가까운 적 타겟으로 설정    
/// </summary>
[FSMContextFolder("Create/State/Targeting")]
public class TargetingNearestState : StateBase
{
    private float targetCheckInterval = 0.2f;  // 타겟 및 범위 체크 간격
    private float lastTargetCheckTime = 0f;

    // 애니메이션 관련 변수
    private float attackAnimationLength = 1.0f;  // 기본값
    private float damageApplyTime = 0.5f;        // 기본값
    private float attackTimer = 0f;              // 공격 타이머
    private bool damageApplied = false;          // 데미지 적용 여부

    // 애니메이션 길이 체크 관련 변수
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f;        // 애니메이션 길이 확인을 위한 지연 시간
    private float animCheckTimer = 0f;           // 애니메이션 체크 타이머

    // 애니메이션 관련 상수
    private const float DAMAGE_TIMING_RATIO = 0.4f;
    private const int LAYER_INDEX = 0;

    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("NearestTargetState 진입");
        lastTargetCheckTime = 0f;

        // Owner가 CharacterFSMObject인지 확인
        if (characterFSM == null) return;

        // 가장 가까운 적 찾기
        FindNearestTarget();

        // 타겟이 없으면 타겟 미스 트리거 발생
        if (characterFSM.CurrentTarget == null)
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // 공격 타이머 및 변수 초기화
        ResetAttack();

        // 애니메이션 체크 변수 초기화
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject 확인
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.AttackFinished);
            return;
        }

        // 애니메이션 길이 체크가 완료되지 않았다면
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // 지정된 시간 후 애니메이션 길이 확인
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // 타이머 리셋 (애니메이션 길이가 정확히 판단된 후 카운트)
                attackTimer = 0f;
            }
            return;
        }

        // 공격 타이머 업데이트
        attackTimer += Time.deltaTime;

        // 데미지 적용 시점에 도달했고, 아직 데미지가 적용되지 않았으면
        if (!damageApplied && attackTimer >= damageApplyTime)
        {

            //TODO : 시너지 분기문 필요

            // 공격 상태로 변경
            Controller.RegisterTrigger(Trigger.TargetSelected);
            damageApplied = true;
        }

        // 공격 애니메이션이 끝났으면
        if (attackTimer >= attackAnimationLength)
        {
            // 타겟 유효성 및 범위 확인
            if (IsTargetValid() && IsTargetInRange())
            {
                // 공격 변수 초기화
                ResetAttack();

                // 애니메이션 체크 변수 초기화
                animLengthChecked = false;
                animCheckTimer = 0f;
            }
            else
            {
                // 타겟이 없거나 범위 밖이면 공격 종료 트리거
                Controller.RegisterTrigger(Trigger.AttackFinished);
            }
        }

        // 주기적으로 타겟 유효성 및 범위 체크
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            if (!IsTargetValid())
            {
                return;
            }

            // 범위 체크
            if (!IsTargetInRange())
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    // 가장 가까운 적을 찾는 메서드
    private void FindNearestTarget()
    {
        if (characterFSM == null || characterFSM.basicObject == null) return;

        // BasicObject.GetTarget() 메서드는 이미 가장 가까운 적을 반환함
        BasicObject nearestTarget = characterFSM.basicObject.GetNearestTarget();

        // 타겟 유효성 검사
        if (nearestTarget != null && (!nearestTarget.isActive || nearestTarget.GetStat(StatName.CurrentHp) <= 0))
        {
            nearestTarget = null;
        }

        // 타겟 설정
        characterFSM.CurrentTarget = nearestTarget;

        if (nearestTarget != null)
        {
            Debug.Log($"가장 가까운 적 타겟 설정: {nearestTarget.name}");
        }
        else
        {
            Debug.Log("유효한 타겟이 없습니다.");
        }
    }

    // 현재 애니메이션 길이 확인 메서드
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) return;

        // 현재 애니메이션 클립 정보 가져오기
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // 현재 재생중인 클립의 길이 가져오기
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
        }
    }

    // 공격 변수 초기화 메서드
    private void ResetAttack()
    {
        attackTimer = 0f;
        damageApplied = false;
    }

    // 타겟이 공격 범위 내에 있는지 확인하는 메서드
    private bool IsTargetInRange()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
            return false;

        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        return characterFSM.GetDistanceToTarget() <= attackRange;
    }

    // 타겟 유효성 체크 메서드
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;
        // 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            FindNearestTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 공격 종료
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            return true; // 새 타겟을 찾음
        }

        if (!target.isActive || target.GetStat(StatName.CurrentHp) <= 0)
        {
            // 타겟이 비활성화 상태이거나 체력이 0 이하면 새 타겟 찾기
            FindNearestTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟이 없으면 공격 종료
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // 새 타겟을 찾았지만 추적 상태로 전환
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        return true;
    }

    public override void OnExit()
    {
        Debug.Log("NearestTargetState 종료");
    }
}