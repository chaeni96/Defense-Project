using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    [SerializeField] private string skillAddressableKey; // 기본 스킬 어드레서블 키
    [SerializeField] private string manaFullSkillAddressableKey; // 마나풀 스킬 어드레서블 키

    private float targetCheckInterval = 0.2f; // 타겟 체크 간격
    private float lastTargetCheckTime = 0f;

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
        lastTargetCheckTime = 0f;

        // 캐릭터 FSM 존재 확인
        if (characterFSM == null) return;

        // 필요시 타겟 찾기
        if (characterFSM.CurrentTarget == null)
        {
            characterFSM.UpdateTarget();
            // 타겟이 없으면 추격 상태로 전환
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }

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
            // 마나 확인
            int currentMana = (int)characterFSM.basicObject.GetStat(StatName.CurrentMana);
            float maxMana = characterFSM.basicObject.GetStat(StatName.MaxMana);

            // 마나가 가득 찼고 마나풀 스킬이 있는 경우 마나풀 스킬 사용
            if (currentMana >= maxMana && !string.IsNullOrEmpty(manaFullSkillAddressableKey))
            {
                Debug.Log("마나가 가득 찼습니다! 마나풀 스킬을 사용합니다.");
                FireSkill(manaFullSkillAddressableKey);
                // 마나 소모
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -currentMana, 1f); // 마나 모두 소모
            }
            // 그렇지 않으면 일반 공격/스킬 사용
            else
            {
                if (string.IsNullOrWhiteSpace(skillAddressableKey))
                {
                    // 직접 데미지 적용
                    ApplyDamage();
                }
                else
                {
                    // 기본 스킬 사용
                    if (characterFSM.CurrentTarget != null)
                    {
                        FireSkill(skillAddressableKey);
                    }
                }

                // 공격 시 소량의 마나 획득
                int manaGain = 10; // 필요에 따라 이 값 조정
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, manaGain, 1f);
            }

            damageApplied = true;
        }

        // 공격 애니메이션 완료 여부 체크
        if (attackTimer >= attackAnimationLength)
        {
            // 타겟이 여전히 유효하고 사거리 내에 있는지 확인
            if (IsTargetValid() && IsTargetInRange())
            {
                // 다음 공격을 위한 초기화
                ResetAttack();
                animLengthChecked = false;
                animCheckTimer = 0f;
            }
            else
            {
                // 타겟이 더 이상 유효하지 않거나 사거리 밖에 있으면 공격 상태 종료
                Controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        // 주기적으로 타겟 유효성 및 사거리 체크
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            if (!IsTargetValid())
            {
                return;
            }

            // 타겟이 사거리 내에 있는지 체크
            if (!IsTargetInRange())
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    // 타겟에게 스킬 발사 메소드
    private void FireSkill(string skillKey)
    {
        if (characterFSM.CurrentTarget == null) return;

        // 타겟 위치와 방향 가져오기
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // 풀에서 스킬 오브젝트 가져오기
        GameObject skillObj = PoolingManager.Instance.GetObject(skillKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // 투사체 초기화 및 발사
        if (skillObj != null)
        {
            SkillBase projectile = skillObj.GetComponent<SkillBase>();
            if (projectile != null)
            {
                projectile.Initialize(characterFSM.basicObject);
                projectile.Fire(
                    characterFSM.basicObject,
                    currentTargetPosition,
                    targetDirection,
                    characterFSM.CurrentTarget
                );

                Debug.Log($"스킬 발사: {skillKey}, 타겟: {characterFSM.CurrentTarget.name}");
            }
            else
            {
                Debug.LogError($"스킬 컴포넌트가 없습니다: {skillKey}");
            }
        }
        else
        {
            Debug.LogError($"스킬 오브젝트를 가져오는데 실패했습니다: {skillKey}");
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

    // 타겟에게 직접 데미지 적용
    private void ApplyDamage()
    {
        if (characterFSM != null && characterFSM.CurrentTarget != null)
        {
            if (characterFSM.CurrentTarget.isEnemy)
            {
                var enemyObj = characterFSM.CurrentTarget.GetComponent<Enemy>();
                if (enemyObj != null)
                {
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    enemyObj.OnDamaged(characterFSM.basicObject, damage);
                    Debug.Log($"직접 데미지 적용: {characterFSM.CurrentTarget.name}, 데미지={damage}");
                }
            }
            else
            {
                var unitObj = characterFSM.CurrentTarget.GetComponent<UnitController>();
                if (unitObj != null)
                {
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    unitObj.OnDamaged(characterFSM.basicObject, damage);
                    Debug.Log($"직접 데미지 적용: {characterFSM.CurrentTarget.name}, 데미지={damage}");
                }
            }
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
        return characterFSM.GetDistanceToTarget() <= attackRange;
    }

    // 타겟 유효성 체크
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // 현재 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            characterFSM.UpdateTarget();
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
            characterFSM.UpdateTarget();
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