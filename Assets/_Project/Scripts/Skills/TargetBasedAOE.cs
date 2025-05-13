using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBasedAOE : TheAOE
{

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        // 타겟 위치 계산
        Vector3 finalPosition;

        finalPosition = targetPos;

        // 계산된 위치로 AOE 배치
        transform.position = finalPosition;

        // 기본 Fire 메서드 호출
        base.Fire(user, targetPos, targetDirection, target);
    }

}
