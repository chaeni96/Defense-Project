using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour
{

    public Dictionary<StatName, float> stats = new Dictionary<StatName, float>();

    public virtual void Initialize()
    {
    }


    // Dictionary로 받아서 한번에 스탯 설정
    public void SetStatValues(Dictionary<StatName, float> statValues)
    {
        foreach (var pair in statValues)
        {
            SetStatValue(pair.Key, pair.Value);
        }
    }

    public void SetStatValue(StatName type, float value)
    {
        // 새로운 스탯 타입이면 추가, 기존 스탯이면 업데이트
        if (stats.ContainsKey(type))
        {
            stats[type] = value;
        }
        else
        {
            stats.Add(type, value);
        }
    }

    public float GetStat(StatName type)
    {
        return stats.TryGetValue(type, out float value) ? value : 0;
    }


}
