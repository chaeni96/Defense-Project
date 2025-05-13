using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetBasedAOE : TheAOE
{

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        // Ÿ�� ��ġ ���
        Vector3 finalPosition;

        finalPosition = targetPos;

        // ���� ��ġ�� AOE ��ġ
        transform.position = finalPosition;

        // �⺻ Fire �޼��� ȣ��
        base.Fire(user, targetPos, targetDirection, target);
    }

}
