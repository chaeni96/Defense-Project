using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        private Transform target;
        private Transform transform;

        private float lastTargetCheckTime;
        private float targetUpdateInterval = 0.5f;
        private BasicObject basicObject;



        public override void OnEnter()
        {
            Debug.Log("ChaseState 시작");
            lastTargetCheckTime = 0f;
            transform = Owner.transform;
            basicObject = Owner.GetComponent<BasicObject>();
            // 타겟 찾기
            FindTarget();
        }

        public override void OnUpdate()
        {
            // 정기적으로 타겟 업데이트
            if (Time.time - lastTargetCheckTime > targetUpdateInterval)
            {
                FindTarget();
                lastTargetCheckTime = Time.time;
            }

            // 타겟이 없으면 대기(Idle) 상태로 전환
            if (target == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }

            // GetStat으로 공격 범위와 이동 속도 가져오기 -> Enter에서 받아놔도 되지만 싸우는 중간에 버프받을수있어서
            float attackRange = basicObject.GetStat(StatName.AttackRange);
            float moveSpeed = basicObject.GetStat(StatName.MoveSpeed) * 2;

            // 타겟까지의 거리 계산
            Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos = new Vector2(target.position.x, target.position.y);
            float distanceToTarget = Vector2.Distance(currentPos, targetPos);

            // 공격 범위 안에 들어왔으면 공격 상태로 전환
            if (distanceToTarget <= attackRange)
            {
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }

            // 타겟 방향으로 이동
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

        private void FindTarget()
        {
            if (basicObject == null) return;

            // 타겟 찾기
            target = basicObject.GetTarget();

            if (target == null)
            {
                Debug.Log("타겟을 찾을 수 없음");
            }
        }
    }
}