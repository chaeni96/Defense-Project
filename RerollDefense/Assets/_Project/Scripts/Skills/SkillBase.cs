using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SkillBase : MonoBehaviour
{
    /// <summary>
    /// targetposition�� �޾Ƽ� ��ų�� �ߵ���Ű�� ����
    /// </summary>
    /// <param name="targetPosition"></param>
    public abstract void Fire(Vector3 targetPosition);
}
