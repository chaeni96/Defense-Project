using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class SynergySkillState : StateBase
{
    [SerializeField] private string skillAddressableKey; // 스킬 어드레서블 키

    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("ActiveState 진입");

        // CharacterFSMObject 확인
        if (characterFSM == null)
        {
            //Controller?.RegisterTrigger(Trigger.SynergySkillFinished);
            return;
        }

        // 타겟 확인
        if (characterFSM.CurrentTarget == null || !IsTargetValid())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // 스킬 사용
        CastSkill();

        // 즉시 스킬 완료 트리거 발생
        //Controller.RegisterTrigger(Trigger.SynergySkillFinished);
    }

    // 스킬 발동 메서드
    private void CastSkill()
    {
        if (string.IsNullOrEmpty(skillAddressableKey))
        {
            Debug.LogError("스킬 어드레서블 키가 설정되지 않았습니다.");
            return;
        }

        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // 현재 타겟 위치와 방향 계산
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // 풀링 매니저에서 스킬 오브젝트 가져오기
        GameObject skillObj = PoolingManager.Instance.GetObject(skillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // 스킬 초기화 및 발사
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"스킬 발사: {skillAddressableKey}, 타겟: {characterFSM.CurrentTarget.name}");

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
                Debug.LogError($"스킬 컴포넌트가 없습니다: {skillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"스킬 오브젝트를 가져오는데 실패했습니다: {skillAddressableKey}");
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
        Debug.Log("SkillCastState 종료");
    }
}
