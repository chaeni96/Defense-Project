using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheTargetDamageSkill : SkillBase
{
    [Header("Ÿ�� ��ų ����")]
    [SerializeField] private float damage = 1f;        // �⺻ ������
    [SerializeField] private float duration = 0.5f;    // ��ų ���� �ð�
    private float timer = 0f;                         // ���� �ð� Ÿ�̸�
    private BasicObject targetObject;                 // Ÿ�� ������Ʈ ����

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        timer = 0f;
        targetObject = target;

        transform.position = targetPos;

        // Ÿ���� �����ϸ� ��� ������ ����
        if (targetObject != null && targetObject.isEnemy != owner.isEnemy)
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
        if (owner == null || targetObject == null)
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
            targetObject.OnDamaged(owner, damage);
            Debug.Log($"Ÿ�� ��ų ������ ����: {targetObject.name}, ������={damage}");
        }
    }

    public override void DestroySkill()
    {
        base.DestroySkill();
        owner = null;
        targetObject = null;
    }
}
