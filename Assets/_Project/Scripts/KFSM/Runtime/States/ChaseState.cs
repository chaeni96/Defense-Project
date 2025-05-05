using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        private float lastTargetCheckTime;
        private float targetUpdateInterval = 0.1f;
        private Transform transform;
        private CharacterFSMObject characterFSM;

        public override void OnEnter()
        {
            Debug.Log("ChaseState 시작");
            lastTargetCheckTime = 0f;
            transform = Owner.transform;

            // Owner를 CharacterFSMObject로 캐스팅
            characterFSM = Owner as CharacterFSMObject;

            if (characterFSM != null)
            {
                // 타겟이 없으면 타겟 찾기
                if (characterFSM.CurrentTarget == null)
                {
                    characterFSM.UpdateTarget();

                    if (characterFSM.CurrentTarget == null)
                    {
                        // 타겟이 없으면 대기 상태로
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }
            else
            {
                // CharacterFSMObject가 아니면 처리
                Controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        public override void OnUpdate()
        {
            // CharacterFSMObject 확인
            if (characterFSM == null) return;

            // 정기적으로 타겟 상태 확인
            if (Time.time - lastTargetCheckTime > targetUpdateInterval)
            {
                lastTargetCheckTime = Time.time;

                // 타겟 있는지 확인
                var target = characterFSM.CurrentTarget;
                if (target == null || !target.gameObject.activeSelf)
                {
                    // 타겟이 죽었거나 사라졌으면 새 타겟 찾기
                    characterFSM.UpdateTarget();

                    //타겟이 아예없으면 idle로 
                    if (characterFSM.CurrentTarget == null)
                    {
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }

            // characterFSM.basicObject에서 스탯 가져오기 (CharacterFSMObject가 참조하는 basicObject 사용)
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            float moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed) * 2;

            // 타겟까지의 거리 계산
            float distanceToTarget = characterFSM.GetDistanceToTarget();

            // 공격 범위 안에 들어왔으면 공격 상태로 전환
            if (distanceToTarget <= attackRange)
            {
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }

            // 타겟 방향으로 이동
            Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos = new Vector2(characterFSM.CurrentTarget.transform.position.x, characterFSM.CurrentTarget.transform.position.y);
            Vector2 direction = (targetPos - currentPos).normalized;

            Vector3 newPosition = transform.position;
            newPosition.x += direction.x * moveSpeed * Time.deltaTime;
            newPosition.y += direction.y * moveSpeed * Time.deltaTime;
            transform.position = newPosition;
        }

        public override void OnExit()
        {
            Debug.Log("ChaseState 종료");
        }
    }
}