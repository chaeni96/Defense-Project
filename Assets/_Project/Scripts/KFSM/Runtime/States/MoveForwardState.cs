using Kylin.LWDI;

namespace Kylin.FSM
{
    using UnityEngine;

    [FSMContextFolder("Create/State/Move")]
    public class MoveForwardState : StateBase
    {
        [SerializeField] private float unitTargetPos;
        [SerializeField] private float enemyTargetPos;
        [SerializeField] private float stopDistance = 0.1f;
        [SerializeField] private float moveSpeed;
        private Transform transform;
        private Vector3 targetPosition;
        private bool checkInit;
        [Inject] protected StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        public override void OnEnter()
        {

            Debug.Log("MoveForwardState : State Enter!!");
            transform = characterFSM.transform;

            if (characterFSM == null) return;

            checkInit = false;

        }

        public override void OnUpdate()
        {
            if (transform == null || characterFSM == null) return;

            if(!checkInit)
            {
                float killZoneX;

                if (characterFSM.isEnemy)
                {
                    killZoneX = enemyTargetPos;
                }
                else
                {
                    killZoneX = unitTargetPos;
                }

                targetPosition = new Vector3(killZoneX, transform.position.y, transform.position.z);

                checkInit = true;
            }

            float distanceToTarget = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(targetPosition.x, targetPosition.y)
            );

            if (distanceToTarget <= stopDistance)
            {
                Controller.RegisterTrigger(Trigger.DetectTarget);
                return;
            }

            Vector3 direction;

            if (characterFSM.isEnemy)
            {
                direction = Vector3.left;
            }
            else
            {
                direction = Vector3.right;
            }

            Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;

            newPosition.y = transform.position.y;

            transform.position = newPosition;

        }

        public override void OnExit()
        {
            Debug.Log("MoveForwardState : State Exit!!");
        }
    }
}