using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public interface IStatSubscriber
{
    void OnStatChanged(StatSubject subject, StatStorage statChange);
}

public class StatStorage
{
    internal StatName statName;
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
    private Dictionary<StatSubject, List<IStatSubscriber>> subscribers = new Dictionary<StatSubject, List<IStatSubscriber>>();



    // �����ͺ��̽����� ���ֵ��� �⺻ ������ �о�ͼ� �ʱ�ȭ
    public void InitializeManager()
    {
        CleanUp();

        D_StatSubjectData.ForEachEntity(subjectData =>
        {
            var subject = subjectData.f_subjectType;

            // �ش� Subject�� Dictionary�� ������ ���� ����
            if (!baseStats.ContainsKey(subject))
            {
                baseStats[subject] = new List<StatStorage>();
            }

            // Subject�� ��� ���� �����͸� �о ����
            foreach (var statData in subjectData.f_subjectStats)
            {
                InitializeBaseStats(subject, statData.f_statName, statData.f_statValue, statData.f_valueMultiply);
            }
        });
    }


    // �⺻ ���� ����
    private void InitializeBaseStats(StatSubject subject, StatName statName, int value, float multiply)
    {
        var storage = new StatStorage
        {
            statName = statName,
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

        // ToList()�� ���纻�� ���� ��ȸ - ���߿� ����Ʈ�� ����Ǵ� �� ����
        foreach (var subscriber in subscribers[subject].ToList())
        {
            if (subscriber != null)
            {
                subscriber.OnStatChanged(subject, statChange);
            }
        }
    }

    // Subject�� statName�� �ش��ϴ� �⺻ ���� ��������
    public StatStorage GetSubjectStat(StatSubject subject, StatName statName)
    {
        if (!baseStats.ContainsKey(subject)) return null;
        return baseStats[subject].Find(s => s.statName == statName);
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
            if (!uniqueStats.ContainsKey(stat.statName))
            {
                uniqueStats[stat.statName] = new StatStorage
                {
                    statName = stat.statName,
                    value = stat.value,
                    multiply = stat.multiply
                };
            }
        }

        return uniqueStats.Values.ToList();
    }


    // ���� �߰�
    public void Subscribe(IStatSubscriber subscriber, StatSubject subject)
    {
        if (!subscribers.ContainsKey(subject))
        {
            subscribers[subject] = new List<IStatSubscriber>();
        }

        if (!subscribers[subject].Contains(subscriber))
        {
            subscribers[subject].Add(subscriber);

        }
    }

    // ���� ����
    public void Unsubscribe(IStatSubscriber subscriber, StatSubject subject)
    {
        if (subscribers.ContainsKey(subject))
        {
            subscribers[subject].Remove(subscriber);
        }
    }

    public List<IStatSubscriber> GetAllSubscribers(StatSubject subject)
    {
        if (subscribers.TryGetValue(subject, out var subscriberList))
        {
            return subscriberList;
        }
        return new List<IStatSubscriber>();
    }
    public void CleanUp()
    {
        baseStats.Clear();
        subscribers.Clear();
    }

}
