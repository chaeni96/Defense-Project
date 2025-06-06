using System.Collections;
using System.Collections.Generic;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Test")]
    public class TestAState : StateBase
    {
        [SerializeField] private float testTimer;
        private float elapsedTime = 0f;
        private bool timerCompleted = false;

        [Inject] protected StateController Controller;
        
        public override void OnEnter() {

            Debug.Log("A : A State Enter!!");
            elapsedTime = 0f;
            timerCompleted = false;
        }
        public override void OnUpdate() {
            if (!timerCompleted)
            {
                elapsedTime += Time.deltaTime;

                if (elapsedTime >= testTimer)
                {
                    Debug.Log("A : B Trigger Regist");
                    //Controller.RegisterTrigger(Trigger.TestBTrigger);
                    timerCompleted = true;
                }
            }
        }
        public override void OnExit() {

            Debug.Log("A : A State Exit!!");
        }
    }
}
