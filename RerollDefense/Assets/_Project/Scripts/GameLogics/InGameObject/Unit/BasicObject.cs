using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour, IStatSubscriber
{

    //처음 Subject에서 가져온 기본 스탯
    protected Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //현재 적용중인 스탯값
    protected Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //구독중인 
    protected List<StatSubject> subjects = new List<StatSubject>();


    public virtual void Initialize()
    {
        foreach (var subject in subjects)
        {
            StatManager.Instance.Subscribe(this, subject);
        }
    }

    public void AddSubject(StatSubject subject)
    {
        if (!subjects.Contains(subject))
        {
            subjects.Add(subject);
            StatManager.Instance.Subscribe(this, subject);
        }
    }

    public virtual void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (!subjects.Contains(subject)) return;

        // currentStats 업데이트
        if (!currentStats.ContainsKey(statChange.stat))
        {
            currentStats[statChange.stat] = new StatStorage
            {
                stat = statChange.stat,
                value = baseStats.ContainsKey(statChange.stat) ? baseStats[statChange.stat].value : 0,
                multiply = baseStats.ContainsKey(statChange.stat) ? baseStats[statChange.stat].multiply : 1f
            };
        }

        var current = currentStats[statChange.stat];
        current.value += statChange.value;
        current.multiply *= statChange.multiply;
    }

    //유닛의 현재 특정 스탯의 값 반환
    public float GetStat(StatName statName)
    {
        if (currentStats.TryGetValue(statName, out var stat))
        {
            return stat.value * stat.multiply;
        }
        return 0f;
    }
    protected virtual void CleanUp()
    {

        foreach (var subject in subjects)
        {
            StatManager.Instance.Unsubscribe(this, subject);
        }

        currentStats.Clear();
        baseStats.Clear();
        subjects.Clear();
    }

}
