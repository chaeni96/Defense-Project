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

        InitializeBaseStats();
    }

    internal Dictionary<StatSubject, List<StatStorage>> statDictionary;


    //UnitData �о�ͼ� 
    private void InitializeBaseStats()
    {
        D_UnitData.ForEachEntity(unitData =>
        {
            var subject = unitData.f_StatSubject;
            if (!statDictionary.ContainsKey(subject))
            {
                statDictionary[subject] = new List<StatStorage>();
            }

            foreach (var stat in unitData.f_UnitsStat)
            {
                var statStorage = new StatStorage
                {
                    stat = stat.f_StatName,          
                    value = stat.f_StatValue,       
                    multiply = stat.f_ValueMultiply 
                };

                statDictionary[subject].Add(statStorage);
            }
        });
    }



}
