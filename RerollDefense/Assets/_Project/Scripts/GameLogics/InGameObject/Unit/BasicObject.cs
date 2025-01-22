using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class BasicObject : MonoBehaviour, IStatSubscriber
{


    //ó�� Subject���� ������ �⺻ ����
    public Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //���� �������� ���Ȱ�
    public Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //�������� 
    public List<StatSubject> subjects = new List<StatSubject>();


    public bool isEnemy = false;
   
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
