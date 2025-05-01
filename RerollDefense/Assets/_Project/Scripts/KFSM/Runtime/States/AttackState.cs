using System;
using UnityEngine;
using Kylin.FSM;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    private float targetCheckInterval = 0.2f; // 타겟 및 범위 체크 주기
    private float lastTargetCheckTime = 0f;
    private CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("AttackState 시작");
        lastTargetCheckTime = 0f;

        // Owner를 CharacterFSMObject로 캐스팅
        characterFSM = Owner as CharacterFSMObject;

        if (characterFSM != null)
        {
            // 타겟이 없다면 찾기
            if (characterFSM.CurrentTarget == null)
            {
                characterFSM.UpdateTarget();

                // 타겟이 없으면 Chase 상태로 (Chase에서 다시 타겟 찾을 것)
                if (characterFSM.CurrentTarget == null)
                {
                    Controller.RegisterTrigger(Trigger.TargetMiss);
                    return;
                }
            }
        }
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject 확인
        if (characterFSM == null) return;

        // 주기적으로 타겟 상태와 범위 확인
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            // 타겟 유효한지 확인
            if (!IsTargetValid())
            {
                return; 
            }

            // 공격 범위 체크 (추가됨)
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            if (characterFSM.GetDistanceToTarget() > attackRange)
            {
                // 타겟이 공격 범위를 벗어났으면 추격 상태로
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    //타겟 유효 체크 메서드
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // 타겟이 없으면 새 타겟 찾기
        if (target == null)
        {
            characterFSM.UpdateTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 Idle 상태로
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            return true; // 새 타겟이 있음
        }

        // 타겟이 비활성화되었는지 확인 (죽었거나 풀에 반환됨)
        if (!target.gameObject.activeSelf)
        {
            // 타겟이 죽었으면 새 타겟 찾기
            characterFSM.UpdateTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // 새 타겟도 없으면 Idle 상태로
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

        return true; 
    }

    public override void OnExit()
    {
        Debug.Log("AttackState 종료");
    }
}