using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatStorage
{
    internal StatName stat;
    internal int value;         //���� ��ġ
    internal float multiply;    //���� ����(1�� �⺻ ����)
}

public class StatManager : MonoBehaviour
{
    public static StatManager _instance;

    public static StatManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StatManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("StatManager");
                    _instance = singleton.AddComponent<StatManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }


    // StatSubject�� �⺻ ���Ȱ� ���� (�ʱⰪ �����)
    private Dictionary<StatSubject, List<StatStorage>> baseStats = new Dictionary<StatSubject, List<StatStorage>>();

    // StatSubject�� ������(BasicObject) ���
    private Dictionary<StatSubject, List<BasicObject>> subscribers = new Dictionary<StatSubject, List<BasicObject>>();



    // �����ͺ��̽����� ���ֵ��� �⺻ ������ �о�ͼ� �ʱ�ȭ
    public void InitializeManager()
    {
        CleanUp();

        D_UnitData.ForEachEntity(unitData =>
        {
            foreach (var subject in unitData.f_StatSubject)
            {
                if (!baseStats.ContainsKey(subject))
                {
                    baseStats[subject] = new List<StatStorage>();
                }

                foreach (var stat in unitData.f_UnitsStat)
                {
                    InitializeBaseStats(subject, stat.f_StatName, stat.f_StatValue, stat.f_ValueMultiply);
                }
            }
        });
    }


    // �⺻ ���� ����
    private void InitializeBaseStats(StatSubject subject, StatName statName, int value, float multiply)
    {
        var storage = new StatStorage
        {
            stat = statName,
            value = value,
            multiply = multiply
        };
        
         baseStats[subject].Add(storage);
        
    }

    // Ư�� Subject�� �����ڵ鿡�� ���� ���� �˸�
    // ���� ���� ������ �� BasicObject�� currentStats���� �̷����
    public void BroadcastStatChange(StatSubject subject, StatStorage statChange)
    {
        if (!subscribers.ContainsKey(subject)) return;

        foreach (var subscriber in subscribers[subject])
        {
            subscriber.OnStatChanged(subject, statChange);
        }
    }

    // Subject�� statName�� �ش��ϴ� �⺻ ���� ��������
    public StatStorage GetSubjectStat(StatSubject subject, StatName statName)
    {
        if (!baseStats.ContainsKey(subject)) return null;
        return baseStats[subject].Find(s => s.stat == statName);
    }



    // Subject�� ��� �⺻ ���� ��������
    public List<StatStorage> GetAllStatsForSubject(StatSubject subject)
    {
        if (!baseStats.ContainsKey(subject))
        {
            return new List<StatStorage>();
        }

        // �ߺ��� StatSubject�� �����������Ƿ� �ߺ� ����
        var uniqueStats = new Dictionary<StatName, StatStorage>();
        foreach (var stat in baseStats[subject])
        {
            if (!uniqueStats.ContainsKey(stat.stat))
            {
                uniqueStats[stat.stat] = new StatStorage
                {
                    stat = stat.stat,
                    value = stat.value,
                    multiply = stat.multiply
                };
            }
        }

        return uniqueStats.Values.ToList();
    }


    // ���� �߰�
    public void Subscribe(BasicObject subscriber, StatSubject subject)
    {
        if (!subscribers.ContainsKey(subject))
        {
            subscribers[subject] = new List<BasicObject>();
        }

        if (!subscribers[subject].Contains(subscriber))
        {
            subscribers[subject].Add(subscriber);

        }
    }

    // ���� ����
    public void Unsubscribe(BasicObject subscriber, StatSubject subject)
    {
        if (subscribers.ContainsKey(subject))
        {
            subscribers[subject].Remove(subscriber);
        }
    }


    public void CleanUp()
    {
        baseStats.Clear();
        subscribers.Clear();
    }

}
