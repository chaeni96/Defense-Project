using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffManager : MonoBehaviour
{
    public static BuffManager _instance;

    public static BuffManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuffManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("BuffManager");
                    _instance = singleton.AddComponent<BuffManager>();
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

        CacheBuffData();
    }

    private Dictionary<string, D_BuffData> buffDataCache = new Dictionary<string, D_BuffData>();
    private Dictionary<BasicObject, List<BuffTimeBase>> activeBuffs = new Dictionary<BasicObject, List<BuffTimeBase>>();


    private void CacheBuffData()
    {
        // BGDatabase에서 모든 버프 데이터를 가져와 캐싱
        D_BuffData.ForEachEntity(buffData =>
        {
            buffDataCache[buffData.f_name] = buffData;
        });
    }


    //BasicObject가 아님...
    public void ApplyBuff(string buffName, BasicObject target)
    {
        if (!buffDataCache.TryGetValue(buffName, out D_BuffData buffData))
            return;

        var buff = CreateBuff(buffData.f_buffType);
        if (buff != null)
        {
            if (!activeBuffs.ContainsKey(target))
            {
                activeBuffs[target] = new List<BuffTimeBase>();
            }

            buff.Initialize(target, buffData);
            buff.StartBuff();
            activeBuffs[target].Add(buff);
        }
    }

    private BuffTimeBase CreateBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.Temporary:
                return new TemporaryBuff();
            case BuffType.Permanent:
                return new PermanentBuff();
            case BuffType.AreaBased:
                return new AreaBuff();
            default:
                return null;
        }
    }

    public void RemoveAllBuffsFromTarget(BasicObject target)
    {
        if (activeBuffs.TryGetValue(target, out var buffs))
        {
            foreach (var buff in buffs)
            {
                //버프 비워주기
            }
            activeBuffs.Remove(target);
        }
    }
}
