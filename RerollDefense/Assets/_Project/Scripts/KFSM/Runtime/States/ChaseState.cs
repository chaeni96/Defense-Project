using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        private Transform target;
        private Transform transform;

        private float lastTargetCheckTime;
        private float targetUpdateInterval = 0.5f;
        private BasicObject basicObject;



        public override void OnEnter()
        {
            Debug.Log("ChaseState ����");
            lastTargetCheckTime = 0f;
            transform = Owner.transform;
            basicObject = Owner.GetComponent<BasicObject>();
            // Ÿ�� ã��
            FindTarget();
        }

        public override void OnUpdate()
        {
            // ���������� Ÿ�� ������Ʈ
            if (Time.time - lastTargetCheckTime > targetUpdateInterval)
            {
                FindTarget();
                lastTargetCheckTime = Time.time;
            }

            // Ÿ���� ������ ���(Idle) ���·� ��ȯ
            if (target == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }

            // GetStat���� ���� ������ �̵� �ӵ� �������� -> Enter���� �޾Ƴ��� ������ �ο�� �߰��� �����������־
            float attackRange = basicObject.GetStat(StatName.AttackRange);
            float moveSpeed = basicObject.GetStat(StatName.MoveSpeed) * 2;

            // Ÿ�ٱ����� �Ÿ� ���
            Vector2 currentPos = new Vector2(transform.position.x, transform.position.y);
            Vector2 targetPos = new Vector2(target.position.x, target.position.y);
            float distanceToTarget = Vector2.Distance(currentPos, targetPos);

            // ���� ���� �ȿ� �������� ���� ���·� ��ȯ
            if (distanceToTarget <= attackRange)
            {
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }

            // Ÿ�� �������� �̵�
            Vector2 direction = (targetPos - currentPos).normalized;

            Vector3 newPosition = transform.position;
            newPosition.x += direction.x * moveSpeed * Time.deltaTime;
            newPosition.y += direction.y * moveSpeed * Time.deltaTime;

            transform.position = newPosition;
        }

        public override void OnExit()
        {
            Debug.Log("ChaseState ����");

        }

        private void FindTarget()
        {
            if (basicObject == null) return;

            // Ÿ�� ã��
            target = basicObject.GetTarget();

            if (target == null)
            {
                Debug.Log("Ÿ���� ã�� �� ����");
            }
        }
    }
}