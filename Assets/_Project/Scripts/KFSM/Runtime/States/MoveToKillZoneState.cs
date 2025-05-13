using Kylin.LWDI;

namespace Kylin.FSM
{
    using UnityEngine;

    [FSMContextFolder("Create/State/Move")]
    public class MoveToKillZoneState : StateBase
    {
        [SerializeField] private float unitTargetPos;
        [SerializeField] private float enemyTargetPos;
        [SerializeField] private float stopDistance = 0.1f;
        private Transform transform;
        //private CharacterFSMObject characterFSM;
        private Vector3 targetPosition;
        private float moveSpeed;
        private bool isEnemy; // ���ʹ����� Ȯ���ϴ� �÷���

        [Inject] protected StateController Controller;
        [Inject] protected CharacterFSMObject characterFSM;
        
        public override void OnEnter()
        {
            Debug.Log("MoveForwardState : State Enter!!");
            transform = characterFSM.transform;
            if (characterFSM == null) return;

            // �̵� �ӵ� �������� -> ������ �������൵��
            moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed);

            // ���ʹ� ���� Ȯ��
            isEnemy = characterFSM.basicObject.isEnemy;

         
            float killZoneX;

            if (isEnemy)
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

        }

        public override void OnUpdate()
        {
            if (transform == null || characterFSM == null) return;

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

            if (isEnemy)
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