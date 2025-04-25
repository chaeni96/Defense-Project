using BansheeGz.BGDatabase;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour, IStatSubscriber
{
    public Animator animator;

    //ó�� Subject���� ������ �⺻ ����, �������̳� ���� ����� �����ϴ� ���ذ�
    public Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //���� ���ӿ��� ���Ǵ� ���� ���� ��, ������ ������� ��ȭ �����ϴ� ��
    public Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //�������� 
    public List<StatSubject> subjects = new List<StatSubject>();


    public bool isEnemy = false;


    //�ӽù��� state ����, ���߿� �� �����Ұ���
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

        // currentStats ������Ʈ
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

    //������ ���� Ư�� ������ �� ��ȯ
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
