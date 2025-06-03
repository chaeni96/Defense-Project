using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    protected BasicObject ownerObj; //스킬을 발동한 주체

    //스킬 이펙트 사용하는 경우 이펙트의 duration 값 동일하게 입력 
    [SerializeField] protected float duration = 0f;
    [SerializeField] protected float damage = 0f; //스킬의 공격력
    [SerializeField] protected float damageMultiplier = 1f;
    public virtual void Initialize(BasicObject owner)
    {
        this.ownerObj = owner;

        // 스킬 매니저에 등록
        SkillManager.Instance.RegisterSkill(this);
    }
    public abstract void Fire(BasicObject target);

    public virtual void DestroySkill()
    {
        // 스킬 매니저에서 등록 해제
        SkillManager.Instance.UnregisterSkill(this);

        ownerObj = null;
        PoolingManager.Instance.ReturnObject(gameObject);
    }

}
