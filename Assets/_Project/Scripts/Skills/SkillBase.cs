using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    protected BasicObject ownerObj; //��ų�� �ߵ��� ��ü

    //��ų ����Ʈ ����ϴ� ��� ����Ʈ�� duration �� �����ϰ� �Է� 
    [SerializeField] protected float duration = 0f;
    [SerializeField] protected float damage = 0f; //��ų�� ���ݷ�
    [SerializeField] protected float damageMultiplier = 1f;
    public virtual void Initialize(BasicObject owner)
    {
        this.ownerObj = owner;

        // ��ų �Ŵ����� ���
        SkillManager.Instance.RegisterSkill(this);
    }
    public abstract void Fire(BasicObject target);

    public virtual void DestroySkill()
    {
        // ��ų �Ŵ������� ��� ����
        SkillManager.Instance.UnregisterSkill(this);

        ownerObj = null;
        PoolingManager.Instance.ReturnObject(gameObject);
    }

}
