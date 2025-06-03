using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheTargetDamageSkill : SkillBase
{
    [Header("Ÿ�� ��ų ����")]
    private float timer = 0f;                         // ���� �ð� Ÿ�̸�
    private BasicObject targetObject;                 // Ÿ�� ������Ʈ ����

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
    }

    public override void Fire(BasicObject target)
    {
        timer = 0f;
        targetObject = target;

        transform.position = targetObject.transform.position;

        // Ÿ���� �����ϸ� ��� ������ ����
        if (targetObject != null && targetObject.isEnemy != ownerObj.isEnemy)
        {
            ApplyDamageToTarget();
        }
        else
        {
            // Ÿ���� ������ ��ų ��� �ı�
            DestroySkill();
        }
    }

    private void Update()
    {
        if (ownerObj == null || targetObject == null)
        {
            DestroySkill();
            return;
        }

        // ���� �ð� üũ
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }
    }

    private void ApplyDamageToTarget()
    {
        if (targetObject != null)
        {
            // ������ ����
            targetObject.OnDamaged(damage);
            Debug.Log($"Ÿ�� ��ų ������ ����: {targetObject.name}, ������={damage}");
        }
    }

    public override void DestroySkill()
    {
        base.DestroySkill();
        ownerObj = null;
        targetObject = null;
    }
}
