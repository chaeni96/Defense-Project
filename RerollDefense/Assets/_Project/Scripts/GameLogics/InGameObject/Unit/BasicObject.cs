using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour
{

    public Dictionary<StatName, int> stats = new Dictionary<StatName, int>();

    public virtual void Initialize()
    {
    }


    public void SetStatValue(StatName type, int value)
    {
        // ���ο� ���� Ÿ���̸� �߰�, ���� �����̸� ������Ʈ
        if (stats.ContainsKey(type))
        {
            stats[type] = value;
        }
        else
        {
            stats.Add(type, value);
        }



    }

    public int GetStat(StatName type)
    {
        return stats.TryGetValue(type, out int value) ? value : 0;
    }

    //���� ���� �����
    protected virtual void OnStatValueChanged(StatName type, float value)
    {
        // ���� ���� �� �߰� ������ �ʿ��� ��� �������̵�
    }

    public virtual void OnSubjectStatChanged(StatSubject subject, StatName statName, float value)
    {
        // Subject ���� ���� �� ȣ���
    }

}
