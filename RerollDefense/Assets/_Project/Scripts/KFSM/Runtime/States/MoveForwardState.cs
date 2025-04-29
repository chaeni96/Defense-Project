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
        private CharacterFSMObject characterFSM;
        private Vector3 targetPosition;

        public override void OnEnter()
        {
            Debug.Log("MoveForwardState : State Enter!!");
            transform = Owner.transform;
            characterFSM = Owner as CharacterFSMObject;

            if (characterFSM == null) return;


          
            float killZoneX;

            if (characterFSM.isEnemy)
            {
                // 에너미는 왼쪽에 있는 킬존으로 이동
                killZoneX = enemyTargetPos;
            }
            else
            {
                // 유닛은 오른쪽에 있는 킬존으로 이동
                killZoneX = unitTargetPos;
            }

            // 타겟 위치 설정
            targetPosition = new Vector3(killZoneX, transform.position.y, transform.position.z);

        }

        public override void OnUpdate()
        {
            if (transform == null || characterFSM == null) return;

            // 목표 지점까지의 거리 계산
            float distanceToTarget = Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(targetPosition.x, targetPosition.y)
            );

            // 목표 지점에 도달했는지 확인
            if (distanceToTarget <= stopDistance)
            {
                // 킬존에 도착하면 Chase 트리거 발생
                Controller.RegisterTrigger(Trigger.ChaseTarget);
                return;
            }

            // 목표 지점을 향해 이동
            Vector3 direction;

            if (characterFSM.isEnemy)
            {
                // 에너미는 왼쪽으로 이동
                direction = Vector3.left;
            }
            else
            {
                // 유닛은 오른쪽으로 이동
                direction = Vector3.right;
            }

            // 이동 처리
            Vector3 newPosition = transform.position + direction * moveSpeed * Time.deltaTime;

            // Y값은 원래 값 유지
            newPosition.y = transform.position.y;

            // 위치 업데이트
            transform.position = newPosition;

        }

        public override void OnExit()
        {
            Debug.Log("MoveForwardState : State Exit!!");
        }
    }
}