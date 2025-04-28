namespace Kylin.FSM
{
    using UnityEngine;

    [FSMContextFolder("Create/State/Move")]
    public class MoveForwardState : StateBase
    {
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float moveSpeed;

        private Transform transform;


        private float stopDistance = 0.1f;
        public override void OnEnter()
        {
            Debug.Log("MoveForwardState : State Enter!!");

            transform = Owner.transform;

        }

        public override void OnUpdate()
        {
            // Ÿ�� ���� ���
            Vector3 direction = (targetPosition - transform.position).normalized;

            // ���� �Ÿ� ���
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // �������� �����ߴ��� üũ
            if (distanceToTarget <= stopDistance)
            {
                // ������ ���� - moveFinished Ʈ���� �߻�
                Controller.RegisterTrigger(Trigger.MoveFinished);
                Debug.Log("������ ����, moveFinished Ʈ���� �߻�");
                return;
            }

            // ������ �̵�
            transform.position += transform.right * moveSpeed * Time.deltaTime;

        }

        public override void OnExit()
        {
        }
    }
}