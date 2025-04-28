using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    public class UnitFSMObject : FSMObjectBase
    {

        // �ִϸ��̼� �̺�Ʈ�� ȣ��� �޼���
        public void OnAttackHit()
        {
            // ���� Ÿ�� �Ǵ� ���� ���� �� ������ ������ ����
            var basicObject = GetComponent<BasicObject>();
            if (basicObject != null)
            {
                Transform target = basicObject.GetTarget();
                if (target != null)
                {
                    var enemyObj = target.GetComponent<Enemy>();
                    if (enemyObj != null)
                    {
                        // ������ ��� �� ����
                        float damage = basicObject.GetStat(StatName.ATK);
                        enemyObj.onDamaged(basicObject, damage);
                    }
                }
            }
        }
    }
}
