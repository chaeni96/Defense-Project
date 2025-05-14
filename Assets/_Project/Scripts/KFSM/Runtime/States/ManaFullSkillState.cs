using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class ManaFullSkillState : StateBase
{
    [SerializeField] private string manaFullSkillAddressableKey; // 스킬 어드레서블 키, 평타로 스킬 쓰는거 (null이면 직접 데미지)


    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("마나 풀 스킬 진입");

        // CharacterFSMObject 확인
        if (characterFSM == null)
        {
            Controller?.RegisterTrigger(Trigger.DamageFinished);
            return;
        }

        // 타겟 확인
        if (characterFSM.CurrentTarget == null || !IsTargetValid())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // 마나 확인
        float currentMana = characterFSM.basicObject.GetStat(StatName.CurrentMana);
        float maxMana = characterFSM.basicObject.GetStat(StatName.MaxMana);
        Debug.Log($"{currentMana}");

        // 마나가 충분한지 확인
        if (currentMana < maxMana)
        {
            Debug.Log($"{currentMana}");
            // 마나가 부족하면 일반 공격 상태로 전환
            Debug.Log("마나가 부족하여 일반 공격으로 전환합니다.");
            Controller.RegisterTrigger(Trigger.TargetSelected);
            return;
        }

        characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(currentMana), 1f);

        // 마나가 충분하면 궁극기 사용
        ActiveManaFullSkill();

        // 즉시 공격 완료 트리거 발생
        Controller.RegisterTrigger(Trigger.DamageFinished);
    }

    // 스킬 발동 메서드
    private void ActiveManaFullSkill()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // 현재 타겟 위치와 방향 계산
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // 풀링 매니저에서 스킬 오브젝트 가져오기
        GameObject skillObj = PoolingManager.Instance.GetObject(manaFullSkillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // 스킬 초기화 및 발사
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"스킬 발사: {manaFullSkillAddressableKey}, 타겟: {characterFSM.CurrentTarget.name}");

                skill.Initialize(characterFSM.basicObject);
                skill.Fire(
                    characterFSM.basicObject,
                    currentTargetPosition,  // 타겟 위치
                    targetDirection,        // 타겟 방향
                    characterFSM.CurrentTarget  // 타겟 오브젝트
                );
            }
            else
            {
                Debug.LogError($"스킬 컴포넌트가 없습니다: {manaFullSkillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"스킬 오브젝트를 가져오는데 실패했습니다: {manaFullSkillAddressableKey}");
        }
    }

    // 타겟 유효성 체크 메서드
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        if (target == null)
            return false;

        // 활성화 상태이고 체력이 있는지 확인
        return target.isActive && target.GetStat(StatName.CurrentHp) > 0;
    }

    public override void OnExit()
    {
        Debug.Log("Attack_DamageState 종료");
    }
}
