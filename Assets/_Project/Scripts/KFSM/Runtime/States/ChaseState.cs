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
            Debug.Log("ChaseState ����");
            lastTargetCheckTime = 0f;
            transform = characterFSM.transform;

            // Owner�� CharacterFSMObject�� ĳ����
            if (characterFSM != null)
            {
                // Ÿ���� ������ Ÿ�� ã��
                if (characterFSM.CurrentTarget == null)
                {
                    characterFSM.UpdateTarget();

                    if (characterFSM.CurrentTarget == null)
                    {
                        // Ÿ���� ������ ��� ���·�
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }
            else
            {
                // CharacterFSMObject�� �ƴϸ� ó��
                Controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        public override void OnUpdate()
        {
            // CharacterFSMObject Ȯ��
            if (characterFSM == null) return;

            // ���������� Ÿ�� ���� Ȯ��
            if (Time.time - lastTargetCheckTime > targetUpdateInterval)
            {
                lastTargetCheckTime = Time.time;

                // Ÿ�� �ִ��� Ȯ��
                var target = characterFSM.CurrentTarget;
                if (target == null || !target.gameObject.activeSelf)
                {
                    // Ÿ���� �׾��ų� ��������� �� Ÿ�� ã��
                    characterFSM.UpdateTarget();

                    //Ÿ���� �ƿ������� idle�� 
                    if (characterFSM.CurrentTarget == null)
                    {
                        Controller.RegisterTrigger(Trigger.TargetMiss);
                        return;
                    }
                }
            }

            // characterFSM.basicObject���� ���� �������� (CharacterFSMObject�� �����ϴ� basicObject ���)
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            float moveSpeed = characterFSM.basicObject.GetStat(StatName.MoveSpeed) * 2;

            // Ÿ�ٱ����� �Ÿ� ���
            float distanceToTarget = characterFSM.GetDistanceToTarget();

            // ���� ���� �ȿ� �������� ���� ���·� ��ȯ
            if (distanceToTarget <= attackRange)
            {
                Controller.RegisterTrigger(Trigger.AttackRequested);
                return;
            }

            // Ÿ�� �������� �̵�
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
            Debug.Log("ChaseState ����");
        }
    }
}