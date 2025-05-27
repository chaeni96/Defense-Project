using System.Collections;
using System.Collections.Generic;
using _Project.Scripts.KFSM.Runtime.Services;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        [Inject] private NearestEnemyDetectService detectService;
        [Inject] protected StateController controller;
        [Inject] protected CharacterFSMObject characterFSM;
        
        private Transform transform;
        
        [SerializeField] private float priorityUpdateInterval = 0.167f; // 10틱마다
        [SerializeField] private float retargetInterval = 0.5f; // 재타겟 주기
        
        private float lastPriorityUpdateTime;
        private float lastRetargetTime;
        
        public override void OnEnter()
        {
            Debug.Log("ChaseState 상태 진입");
            transform = characterFSM.transform;
            
            lastPriorityUpdateTime = 0f;
            lastRetargetTime = 0f;
            
            // 진입 시 즉시 우선도 업데이트
            detectService.UpdateTargetPriority(characterFSM);
            
            var initialTarget = detectService.DetectTarget(characterFSM);
            if (initialTarget != null)
            {
                characterFSM.CurrentTarget = initialTarget;
                Debug.Log($"초기 타겟 설정: {initialTarget.name}, HP: {initialTarget.GetStat(StatName.CurrentHp)}");
            }
            else
            {
                Debug.Log("초기 타겟을 찾을 수 없음");
                controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        public override void OnUpdate()
        {
            // 주기적으로 우선도 업데이트
            if (Time.time - lastPriorityUpdateTime > priorityUpdateInterval)
            {
                detectService.UpdateTargetPriority(characterFSM);
                lastPriorityUpdateTime = Time.time;
            }
            
            // 주기적으로 타겟 재선택
            if (Time.time - lastRetargetTime > retargetInterval || characterFSM.CurrentTarget == null)
            {
                var newTarget = detectService.DetectTarget(characterFSM);
                
                if (newTarget != null)
                {
                    characterFSM.CurrentTarget = newTarget;
                }
                else
                {
                    controller.RegisterTrigger(Trigger.TargetMiss);
                    return;
                }
                
                lastRetargetTime = Time.time;
            }
            
            var target = characterFSM.CurrentTarget;
            
            // 타겟 유효성 체크
            if (target == null || target.GetStat(StatName.CurrentHp) <= 0)
            {
                controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
            
            // 공격 범위 체크
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            float distance = Vector2.Distance(transform.position, target.transform.position);
            
            if (distance <= attackRange)
            {
                controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }
            
            // 타겟을 향해 이동
            float moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed);
            Vector2 direction = (target.transform.position - transform.position).normalized;
            transform.position += (Vector3)direction * (moveSpeed * Time.deltaTime);

        }

        public override void OnExit()
        {
        }
        
       
    }
}