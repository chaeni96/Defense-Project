using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    /// <summary>
    /// targetposition을 받아서 스킬을 발동시키는 역할
    /// </summary>
    /// <param name="targetPosition"></param>
    /// 

    public BasicObject owner; //스킬을 발동한 주체
    public SoundPlayer soundEffect;

    protected float attackDamage = 0; //스킬의 공격력
    
    public virtual void Initialize(BasicObject owner)
    {
        this.owner = owner;
    }
    public abstract void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null);

    public abstract void DestroySkill();

}
