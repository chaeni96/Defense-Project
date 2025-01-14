using System.Collections;
using System.Collections.Generic;
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

    internal Dictionary<StatSubject, List<StatStorage>> statDictionary;



    public void InitializeManager()
    {
        statDictionary = new Dictionary<StatSubject, List<StatStorage>>();

        //StatSubject���� ��ųʸ� �ʱ�ȭ
        foreach (StatSubject subject in System.Enum.GetValues(typeof(StatSubject)))
        {
            statDictionary[subject] = new List<StatStorage>();
        }
    }

    public void SetStat(StatSubject subject, StatName statName, int value, float multiply = 1f)
    {
        if (!statDictionary.ContainsKey(subject))
            statDictionary[subject] = new List<StatStorage>();

        statDictionary[subject].Add(new StatStorage
        {
            stat = statName,
            value = value,
            multiply = multiply
        });
    }

    public List<StatStorage> GetStats(StatSubject subject)
    {
        return statDictionary.TryGetValue(subject, out var stats) ? stats : new List<StatStorage>();
    }



}
