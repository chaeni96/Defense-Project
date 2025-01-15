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

    public int GetStat(StatName type)
    {
        return stats.TryGetValue(type, out int value) ? value : 0;
    }

    //개별 스탯 변경시
    protected virtual void OnStatValueChanged(StatName type, float value)
    {
        // 스탯 변경 시 추가 동작이 필요한 경우 오버라이드
    }

    public virtual void OnSubjectStatChanged(StatSubject subject, StatName statName, float value)
    {
        // Subject 스탯 변경 시 호출됨
    }

}
