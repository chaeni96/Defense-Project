using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    public class CharacterFSMObject : FSMObjectBase
    {
        //Ÿ�� ���� �ʵ�

        public BasicObject CurrentTarget;
        public BasicObject basicObject;//�� ���¸ӽ��� ����ִ� ������Ʈ(����, ���ʹ�)

        protected override void Initialized()
        {
            base.Initialized();

            // basicObject�� ������ ��������
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();

            isEnemy = basicObject.isEnemy;

            // �ʱ� Ÿ�� ����
            UpdateTarget();


            isFinished = false;
        }

        // Ÿ�� ������Ʈ �޼���
        public virtual void UpdateTarget()
        {
            if (basicObject == null)
                basicObject = GetComponent<BasicObject>();

            if (basicObject != null)
            {
                // ���� Ÿ�� ����
                var oldTarget = CurrentTarget;

                // �� Ÿ�� ã��
                var newTarget = basicObject.GetNearestTarget();

                if (newTarget != null)
                {
                    // Ȱ��ȭ ���� Ȯ��
                    if (!newTarget.gameObject.activeSelf)
                    {
                        newTarget = null;
                    }
                    else
                    {
                        if (newTarget != null)
                        {
                            if (newTarget.GetStat(StatName.CurrentHp) <= 0)
                            {
                                newTarget = null;
                            }
                        }
                    }
                }
                CurrentTarget = newTarget;
            }
        }

        // Ÿ�� ��ȿ�� �˻�
        public virtual bool IsTargetValid()
        {
            if (CurrentTarget == null)
                return false;

            return CurrentTarget.gameObject.activeSelf;
        }

        // Ÿ�ٰ��� �Ÿ� ���
        public virtual float GetDistanceToTarget()
        {
            if (CurrentTarget == null)
                return float.MaxValue;

            return Vector2.Distance(
                new Vector2(transform.position.x, transform.position.y),
                new Vector2(CurrentTarget.transform.position.x, CurrentTarget.transform.position.y)
            );
        }


        public void FinishBattleWinAnimation()
        {
            isFinished = true;
        }
    }
}
