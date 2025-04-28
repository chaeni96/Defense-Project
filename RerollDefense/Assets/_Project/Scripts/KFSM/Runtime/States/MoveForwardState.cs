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
            // 타겟 방향 계산
            Vector3 direction = (targetPosition - transform.position).normalized;

            // 남은 거리 계산
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

            // 목적지에 도착했는지 체크
            if (distanceToTarget <= stopDistance)
            {
                // 목적지 도착 - moveFinished 트리거 발생
                Controller.RegisterTrigger(Trigger.MoveFinished);
                Debug.Log("목적지 도착, moveFinished 트리거 발생");
                return;
            }

            // 앞으로 이동
            transform.position += transform.right * moveSpeed * Time.deltaTime;

        }

        public override void OnExit()
        {
        }
    }
}