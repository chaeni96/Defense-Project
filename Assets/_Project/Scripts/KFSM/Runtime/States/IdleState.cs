using Kylin.LWDI;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Idle")]
    public class IdleState : StateBase
    {
        [SerializeField] private float checkInterval = 0.3f;
        
        [Inject] private StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        
        private float lastDetectTime;
        
        public override void OnEnter()
        {        
            Debug.Log("idle 스테이트 진입");
            if (characterFSM == null) return;

            
            lastDetectTime = 0f;
        }
        
        public override void OnUpdate()
        {
            if(!characterFSM.basicObject.canChase) return;
            
            if (Time.time - lastDetectTime >= checkInterval)
            {
                lastDetectTime = Time.time;
                
                //idle 상태일때 상대방 적 남아있는지 체크
                //남아있을경우 ChaseTargetState로 넘김
              CheckTarget();
            }
        }

        public override void OnExit()
        {
            lastDetectTime = 0f;
        }
        
        // 타겟 업데이트 메서드
        private void CheckTarget()
        {
            if (characterFSM.basicObject != null)
            {
                // 적 남아있는지 체크 
                var targetList = characterFSM.basicObject.GetActiveTargetList();
                if (targetList != null && targetList.Count > 0)
                {
                    //추격 상태로 변경
                    Controller.RegisterTrigger(Trigger.DetectTarget);
                }
            }
        }

        
    }
    
}