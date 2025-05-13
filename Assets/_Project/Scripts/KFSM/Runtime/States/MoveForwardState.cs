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
        //private CharacterFSMObject characterFSM;
        private Vector3 targetPosition;
        private bool checkInit;
        [Inject] protected StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        public override void OnEnter()
        {

            Debug.Log("MoveForwardState : State Enter!!");

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
                    // ���ʹ̴� ���ʿ� �ִ� ų������ �̵�
                    killZoneX = enemyTargetPos;
                }
                else
                {
                    // ������ �����ʿ� �ִ� ų������ �̵�
                    killZoneX = unitTargetPos;
                }

                // Ÿ�� ��ġ ����
                targetPosition = new Vector3(killZoneX, transform.position.y, transform.position.z);

                checkInit = true;
            }

            // ��ǥ ���������� �Ÿ� ���
            float distanceToTarget = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(targetPosition.x, targetPosition.y)
            );

            // ��ǥ ������ �����ߴ��� Ȯ��
            if (distanceToTarget <= stopDistance)
            {
                // ų���� �����ϸ� Chase Ʈ���� �߻�
                Controller.RegisterTrigger(Trigger.ChaseTarget);
                return;
            }

            // ��ǥ ������ ���� �̵�
            Vector3 direction;

            if (characterFSM.isEnemy)
            {
                // ���ʹ̴� �������� �̵�
                direction = Vector3.left;
            }
            else
            {
                // ������ ���������� �̵�
                direction = Vector3.right;
            }

            // �̵� ó��
            Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;

            // Y���� ���� �� ����
            newPosition.y = transform.position.y;

            // ��ġ ������Ʈ
            transform.position = newPosition;

        }

        public override void OnExit()
        {
            Debug.Log("MoveForwardState : State Exit!!");
        }
    }
}