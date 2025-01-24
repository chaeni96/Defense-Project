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
    internal int value;         //스탯 수치
    internal float multiply;    //스탯 배율(1이 기본 배율)
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


    // StatSubject별 기본 스탯값 저장 (초기값 저장용)
    private Dictionary<StatSubject, List<StatStorage>> baseStats = new Dictionary<StatSubject, List<StatStorage>>();

    // StatSubject별 구독자(BasicObject) 목록
    private Dictionary<StatSubject, List<IStatSubscriber>> subscribers = new Dictionary<StatSubject, List<IStatSubscriber>>();



    // 데이터베이스에서 유닛들의 기본 스탯을 읽어와서 초기화
    public void InitializeManager()
    {
        CleanUp();

        D_StatSubjectData.ForEachEntity(subjectData =>
        {
            var subject = subjectData.f_subjectType;

            // 해당 Subject가 Dictionary에 없으면 새로 생성
            if (!baseStats.ContainsKey(subject))
            {
                baseStats[subject] = new List<StatStorage>();
            }

            // Subject의 모든 스탯 데이터를 읽어서 저장
            foreach (var statData in subjectData.f_subjectStats)
            {
                InitializeBaseStats(subject, statData.f_statName, statData.f_statValue, statData.f_valueMultiply);
            }
        });
    }


    // 기본 스탯 저장
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

    // 특정 Subject의 구독자들에게 스탯 변경 알림
    // 실제 스탯 변경은 각 BasicObject의 currentStats에서 이루어짐
    public void BroadcastStatChange(StatSubject subject, StatStorage statChange)
    {
        if (!subscribers.ContainsKey(subject)) return;

        // ToList()로 복사본을 만들어서 순회 - 도중에 리스트가 변경되는 것 방지
        foreach (var subscriber in subscribers[subject].ToList())
        {
            if (subscriber != null)
            {
                subscriber.OnStatChanged(subject, statChange);
            }
        }
    }

    // Subject의 statName에 해당하는 기본 스탯 가져오기
    public StatStorage GetSubjectStat(StatSubject subject, StatName statName)
    {
        if (!baseStats.ContainsKey(subject)) return null;
        return baseStats[subject].Find(s => s.statName == statName);
    }



    // Subject의 모든 기본 스탯 가져오기
    public List<StatStorage> GetAllStatsForSubject(StatSubject subject)
    {
        if (!baseStats.ContainsKey(subject))
        {
            return new List<StatStorage>();
        }

        // 중복된 StatSubject가 있을수있으므로 중복 제거
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


    // 구독 추가
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

    // 구독 제거
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
