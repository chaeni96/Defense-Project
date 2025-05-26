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
        private IDetectService detectService;
        [SerializeField] private float retargetInterval = 1f; // 재탐색 주기

        
        [Inject] protected StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        
        private Transform transform;
        private float lastRetargetTime;
        
        
        public override void OnEnter()
        {
            Debug.Log("ChaseState 상태 진입");
            transform = characterFSM.transform;
            
            lastRetargetTime = 0f;
            
          

            
        }

        public override void OnUpdate()
        {
           
            if (characterFSM == null) return;
            
            // 진입 시 타겟 탐색
            if (detectService != null)
            {
                var targetList = characterFSM.basicObject.GetActiveTargetList();
                var temp = detectService.DetectTarget(transform, targetList, !characterFSM.isEnemy);
                
                if (temp != null)
                {
                    characterFSM.CurrentTarget = temp;
                    
                    Debug.Log($"타겟 찾음 {temp} : {temp.GetStat(StatName.MaxHP)}");
                }
            }
            
            var target = characterFSM.CurrentTarget;
            
            
            // 타겟이 없거나 유효하지 않으면 재탐색
            if (target == null || !IsValidTarget(target))
            {
                if (detectService != null)
                {
                    var targetList = characterFSM.basicObject.GetActiveTargetList();
                    target = detectService.DetectTarget(transform, targetList, !characterFSM.isEnemy);
                    
                    if (target != null)
                    {
                        characterFSM.CurrentTarget = target;
                    }
                    else
                    {
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
                else
                {
                    Controller.RegisterTrigger(Trigger.TargetMiss);
                    return;
                }
            }
            
            // 주기적 재탐색
            if (detectService != null && Time.time - lastRetargetTime > retargetInterval)
            {
                lastRetargetTime = Time.time;
                
                var targetList = characterFSM.basicObject.GetActiveTargetList();
                var newTarget = detectService.DetectTarget(transform, targetList, !characterFSM.isEnemy);
                
                if (newTarget != null)
                {
                    characterFSM.CurrentTarget = newTarget;
                    target = newTarget;
                }
            }
            
            // 스탯 가져오기
            float moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed);
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            
            // 타겟까지의 거리 계산
            float distance = Vector2.Distance(transform.position, target.transform.position);
            
            // 공격 범위 내에 있으면 공격 요청
            if (distance <= attackRange)
            {
                characterFSM.CurrentTarget = target;
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }
            
            // 타겟을 향해 이동
            Vector2 direction = (target.transform.position - transform.position).normalized;
            transform.position += (Vector3)direction * (moveSpeed * Time.deltaTime);
          
        }

        public override void OnExit()
        {
        }
        
        private bool IsValidTarget(BasicObject target)
        {
            return target != null && 
                   target.GetStat(StatName.CurrentHp) > 0 && 
                   target.gameObject.activeSelf;
        }
    }
}