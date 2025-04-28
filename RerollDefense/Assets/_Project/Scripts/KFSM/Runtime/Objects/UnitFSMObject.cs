using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Kylin.FSM
{
    public class UnitFSMObject : FSMObjectBase
    {

        // 애니메이션 이벤트로 호출될 메서드
        public void OnAttackHit()
        {
            // 현재 타겟 또는 공격 범위 내 적에게 데미지 적용
            var basicObject = GetComponent<BasicObject>();
            if (basicObject != null)
            {
                Transform target = basicObject.GetTarget();
                if (target != null)
                {
                    var enemyObj = target.GetComponent<Enemy>();
                    if (enemyObj != null)
                    {
                        // 데미지 계산 및 적용
                        float damage = basicObject.GetStat(StatName.ATK);
                        enemyObj.onDamaged(basicObject, damage);
                    }
                }
            }
        }
    }
}
