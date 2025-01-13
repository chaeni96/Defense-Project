using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatStorage
{
    internal StatName stat;
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

    internal Dictionary<StatSubject, List<StatStorage>> statDictionary;

}
