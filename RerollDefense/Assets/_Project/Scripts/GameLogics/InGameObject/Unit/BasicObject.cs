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


    // Dictionary�� �޾Ƽ� �ѹ��� ���� ����
    public void SetStatValues(Dictionary<StatName, int> statValues)
    {
        foreach (var pair in statValues)
        {
            SetStatValue(pair.Key, pair.Value);
        }
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


}
