using System;
using UnityEngine;
using Kylin.FSM;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    private float targetCheckInterval = 0.2f; // 타겟 및 범위 체크 주기
    private float lastTargetCheckTime = 0f;
    private CharacterFSMObject characterFSM;

    // 공격 애니메이션 관련 타이머 변수
    private float attackAnimationLength = 1.0f; // 기본값
    private float damageApplyTime = 0.5f; // 기본값
    private float attackTimer = 0f; // 공격 타이머
    private bool damageApplied = false; // 데미지 적용 여부

    // 애니메이션 정보 가져오기 관련 변수
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f; // 애니메이션 정보 가져오기 전 대기 시간
    private float animCheckTimer = 0f; // 애니메이션 체크 타이머

    // 애니메이션 관련 상수
    private const float DAMAGE_TIMING_RATIO = 0.5f; // 애니메이션의 40% 지점에서 데미지 적용
    private const int LAYER_INDEX = 0; // 기본 애니메이션 레이어

    public override void OnEnter()
    {
        Debug.Log("AttackState 시작");
        lastTargetCheckTime = 0f;

        // Owner를 CharacterFSMObject로 캐스팅
        characterFSM = Owner as CharacterFSMObject;
        if (characterFSM == null) return;

        // 타겟이 없다면 찾기
        if (characterFSM.CurrentTarget == null)
        {
            characterFSM.UpdateTarget();
            // 타겟이 없으면 Chase 상태로 
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }


        // 공격 타이머 및 상태 초기화
        ResetAttack();

        // 애니메이션 정보 가져오기 초기화
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject 확인
        if (characterFSM == null) return;

        // 애니메이션 정보를 아직 가져오지 않았다면
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // 일정 시간 후 애니메이션 정보 가져오기
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // 타이머 리셋 (애니메이션 정보를 정확히 가져온 시점부터 카운트)
                attackTimer = 0f;
            }
            return;
        }

        // 공격 타이머 업데이트
        attackTimer += Time.deltaTime;

        // 데미지 적용 시점에 도달했고 아직 데미지를 적용하지 않았다면
        if (!damageApplied && attackTimer >= damageApplyTime)
        {
            // 데미지 적용
            ApplyDamage();
            damageApplied = true;
        }

        // 공격 애니메이션이 끝났다면
        if (attackTimer >= attackAnimationLength)
        {
            // 타겟 유효성 및 거리 확인
            if (IsTargetValid() && IsTargetInRange())
            {

                // 공격 상태 초기화 
                ResetAttack();

                // 애니메이션 체크 상태 초기화
                animLengthChecked = false;
                animCheckTimer = 0f;
            }
            else
            {
                // 타겟이 없거나 범위 밖이면 다른 상태로 전환
                Controller.RegisterTrigger(Trigger.AttackFinished);
            }
        }

        // 주기적으로 타겟 상태와 범위 확인
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            // 타겟 유효한지 확인
            if (!IsTargetValid())
            {
                return;
            }

            // 공격 범위 체크
            if (!IsTargetInRange())
            {
                // 타겟이 공격 범위를 벗어났으면 추격 상태로
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
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
            // 현재 재생 중인 클립의 길이 가져오기
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;

            Debug.Log($"현재 공격 애니메이션 길이:{clipInfo[0].clip.name} {attackAnimationLength:F2}초, 데미지 적용 시점: {damageApplyTime:F2}초");
        }
        else
        {
            // 클립 정보를 못 찾은 경우 - 기본값 사용
            attackAnimationLength = 1.0f; // 기본값
            damageApplyTime = 0.4f; // 기본값
            Debug.LogWarning("현재 재생 중인 애니메이션 클립을 찾지 못했습니다. 기본값 사용");
        }
    }

    // 데미지 적용 메서드
    private void ApplyDamage()
    {
        if (characterFSM != null && characterFSM.CurrentTarget != null)
        {
            if (characterFSM.CurrentTarget.isEnemy)
            {
                var enemyObj = characterFSM.CurrentTarget.GetComponent<Enemy>();
                if (enemyObj != null)
                {
                    // 데미지 계산 및 적용
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    enemyObj.onDamaged(characterFSM.basicObject, damage);
                }
            }
            else
            {
                var unitObj = characterFSM.CurrentTarget.GetComponent<UnitController>();
                if (unitObj != null)
                {
                    // 데미지 계산 및 적용
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    unitObj.onDamaged(characterFSM.basicObject, damage);
                }
            }
        }
    }

    // 공격 상태 초기화 메서드
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

    // 타겟 유효 체크 메서드
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;
        // 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 상태 전환
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            Controller.RegisterTrigger(Trigger.AttackRequested);

            return true; // 새 타겟이 있음
        }

        // 타겟이 비활성화되었는지 확인 (죽었거나 풀에 반환됨)
        if (!target.gameObject.activeSelf)
        {
            // 타겟이 죽었으면 새 타겟 찾기
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 상태 전환
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // 새 타겟이 있으면 Chase 상태로
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        // 타겟의 체력 확인 (추가됨)
        if (target.GetStat(StatName.CurrentHp) <= 0)
        {
            // 타겟이 죽었으면 새 타겟 찾기
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 상태 전환
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // 새 타겟이 있지만 현재 타겟과 다르면 Chase 상태로
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
        Debug.Log("AttackState 종료");
    }
}