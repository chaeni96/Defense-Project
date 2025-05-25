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
            Debug.Log("idle ������Ʈ ����");
            if (characterFSM == null) return;

            
            lastDetectTime = 0f;
        }
        
        public override void OnUpdate()
        {
            if(!characterFSM.basicObject.canChase) return;
            
            if (Time.time - lastDetectTime >= checkInterval)
            {
                lastDetectTime = Time.time;
                
                //idle �����϶� ���� �� �����ִ��� üũ
                //����������� ChaseTargetState�� �ѱ�
              CheckTarget();
            }
        }

        public override void OnExit()
        {
            lastDetectTime = 0f;
        }
        
        // Ÿ�� ������Ʈ �޼���
        private void CheckTarget()
        {
            if (characterFSM.basicObject != null)
            {
                // �� �����ִ��� üũ 
                var targetList = characterFSM.basicObject.GetActiveTargetList();
                if (targetList != null && targetList.Count > 0)
                {
                    //�߰� ���·� ����
                    Controller.RegisterTrigger(Trigger.DetectTarget);
                }
            }
        }

        
    }
    
}