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

    public UnitController owner; //스킬을 발동한 주체
    public virtual void Initialize(UnitController owner)
    {
        this.owner = owner;
    }
    public abstract void Fire(Vector3 targetPosition);

    public abstract void CleanUp();
}
