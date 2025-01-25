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

    public UnitController owner; //��ų�� �ߵ��� ��ü
    public virtual void Initialize(UnitController owner)
    {
        this.owner = owner;
    }
    public abstract void Fire(Vector3 targetPosition);

    public abstract void CleanUp();
}
