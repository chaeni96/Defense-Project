using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    /// <summary>
    /// targetposition�� �޾Ƽ� ��ų�� �ߵ���Ű�� ����
    /// </summary>
    /// <param name="targetPosition"></param>
    /// 

    public BasicObject owner; //��ų�� �ߵ��� ��ü
    public SoundPlayer soundEffect;

    protected float attackDamage = 0; //��ų�� ���ݷ�
    
    public virtual void Initialize(BasicObject owner)
    {
        this.owner = owner;
    }
    public abstract void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null);

    public abstract void DestroySkill();

}
