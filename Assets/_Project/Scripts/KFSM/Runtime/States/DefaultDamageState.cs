using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class DefaultDamageState : StateBase
{
    [SerializeField] private string defaultSkillAddressableKey; // 스킬 어드레서블 키, 평타로 스킬 쓰는거 (null이면 직접 데미지)
    [SerializeField] private string manaFullSkillAddressableKey; // 마나 풀로 채웠을때 쓰는 스킬 어드레서블 키


    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("Attack_DamageState 진입");

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

        //TODO: 분기문 필요 마나 다 차면 스킬 사용하는것과 아닌것으로

        // skillAddressableKey가 null이면 직접 데미지, 아니면 스킬 사용
        if (string.IsNullOrEmpty(defaultSkillAddressableKey))
        {
            ApplyDamage();
        }
        else
        {
            ActiveDefaultSkill();
        }

        // 즉시 공격 완료 트리거 발생
        Controller.RegisterTrigger(Trigger.DamageFinished);
    }

    // 직접 데미지 적용 메서드
    private void ApplyDamage()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // 데미지 계산 (캐릭터의 공격력 가져오기)
        float damage = characterFSM.basicObject.GetStat(StatName.ATK);

        // 타겟에게 데미지 적용
        characterFSM.CurrentTarget.OnDamaged(characterFSM.basicObject, damage);

        Debug.Log($"직접 데미지 적용: 대상={characterFSM.CurrentTarget.name}, 데미지={damage}");
    }

    // 스킬 발동 메서드
    private void ActiveDefaultSkill()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // 현재 타겟 위치와 방향 계산
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // 풀링 매니저에서 스킬 오브젝트 가져오기
        GameObject skillObj = PoolingManager.Instance.GetObject(defaultSkillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // 스킬 초기화 및 발사
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"스킬 발사: {defaultSkillAddressableKey}, 타겟: {characterFSM.CurrentTarget.name}");

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
                Debug.LogError($"스킬 컴포넌트가 없습니다: {defaultSkillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"스킬 오브젝트를 가져오는데 실패했습니다: {defaultSkillAddressableKey}");
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
