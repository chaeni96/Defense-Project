using BansheeGz.BGDatabase;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour, IStatSubscriber
{
    public Animator animator;

    //처음 Subject에서 가져온 기본 스탯, 레벨업이나 버프 적용시 참조하는 기준값
    public Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //실제 게임에서 사용되는 현재 스탯 값, 버프나 디버프로 변화 적용하는 값
    public Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //구독중인 
    public List<StatSubject> subjects = new List<StatSubject>();


    public bool isEnemy = false;


    //임시방편 state 변경, 나중에 다 삭제할거임
    public State currentState;
    public void ChangeState<T>(T state) where T : State
    {
        currentState?.ExitState(this);
        currentState = state;
        currentState?.EnterState(this);
    }


    public virtual void Initialize()
    {
        foreach (var subject in subjects)
        {
            StatManager.Instance.Subscribe(this, subject);
        }
    }

    public virtual void Update()
    {
        if (Time.timeScale != 0)
        {
            currentState?.UpdateState(this);
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
        if (!currentStats.ContainsKey(statChange.statName))
        {
            currentStats[statChange.statName] = new StatStorage
            {
                statName = statChange.statName,
                value = baseStats.ContainsKey(statChange.statName) ? baseStats[statChange.statName].value : 0,
                multiply = baseStats.ContainsKey(statChange.statName) ? baseStats[statChange.statName].multiply : 1f
            };
        }

        currentStats[statChange.statName].value += statChange.value;
        currentStats[statChange.statName].multiply *= statChange.multiply;
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
