using System.Collections;
using System.Collections.Generic;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        private float lastTargetCheckTime;
        private float targetUpdateInterval = 0.1f;
        private Transform transform;
        [Inject] protected StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        public override void OnEnter()
        {
            Debug.Log("ChaseState ????");
            lastTargetCheckTime = 0f;
            transform = characterFSM.transform;

            // Owner?? CharacterFSMObject?? ĳ????
            if (characterFSM != null)
            {
                // ????? ?????? ??? ???
                if (characterFSM.CurrentTarget == null)
                {
                    characterFSM.UpdateTarget();

                    if (characterFSM.CurrentTarget == null)
                    {
                        // ????? ?????? ???????·?
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }
            else
            {
                // CharacterFSMObject?? ???? ???
                Controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        public override void OnUpdate()
        {
            // CharacterFSMObject ???
            if (characterFSM == null) return;

            // ?????????? ??? ???? ???
            if (Time.time - lastTargetCheckTime > targetUpdateInterval)
            {
                lastTargetCheckTime = Time.time;

                // ??? ????? ???
                var target = characterFSM.CurrentTarget;
                if (target == null || !target.gameObject.activeSelf)
                {
                    // ????? ?????? ???????????? ??? ???
                    characterFSM.UpdateTarget();

                    //????? ????????? idle?? 
                    if (characterFSM.CurrentTarget == null)
                    {
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }

            // characterFSM.basicObject???? ???? ???????? (CharacterFSMObject?? ??????? basicObject ????
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            float moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed) * 2;

            // ???????? ??? ????
            float distanceToTarget = characterFSM.GetDistanceToTarget();

            // ???? ???? ??? ???????? ???? ???·? ???
            if (distanceToTarget <= attackRange)
            {
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }

            // ??? ???????? ???
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
            Debug.Log("ChaseState ????");
        }
    }
}