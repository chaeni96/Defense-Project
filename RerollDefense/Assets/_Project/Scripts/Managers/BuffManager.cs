using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    }

    //현재 적용된 버프들 -> 나중에 특정 subject 모든 버프 제거하거나, 특정 ID 버프 제거등 사용하기 위함
    private Dictionary<StatSubject, List<BuffTimeBase>> activeBuffs = new Dictionary<StatSubject, List<BuffTimeBase>>();

  
    //WildCard나 아이템 등에서 버프 적용하는 메서드 -> 호출하면 버프 적용
    public void ApplyBuff(D_BuffData buffData, StatSubject subject, BasicObject owner = null)
    {
        //버프 타입에 맞는 버프 객체 생성
        var buff = CreateBuff(buffData.f_buffType);
        if (buff == null) return;


        if (!activeBuffs.ContainsKey(subject))
        {
            activeBuffs[subject] = new List<BuffTimeBase>();
        }

        //버프 초기화
        buff.Initialize(buffData);
        //버프 시작
        buff.StartBuff(subject);
        activeBuffs[subject].Add(buff);
    }

    private BuffTimeBase CreateBuff(BuffType type)
    {
        switch (type)
        {
            case BuffType.Temporal:
                return new TemporalBuff();
            case BuffType.Instant:
                return new InstantBuff();
            case BuffType.Range:
                return new RangeBuff();
            default:
                return null;
        }
    }

    //버프 삭제 관련 메서드들
    public void RemoveAllBuffsFromSubject(StatSubject subject)
    {
        if (activeBuffs.TryGetValue(subject, out var buffs))
        {
            foreach (var buff in buffs)
            {
                RemoveBuff(buff, subject);
            }
            activeBuffs.Remove(subject);
        }
    }

    private void RemoveBuff(BuffTimeBase buff, StatSubject subject)
    {
        if (buff is TemporalBuff tempBuff)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(tempBuff.GetBuffUID());
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(tempBuff.GetBuffUID());
        }
        else if (buff is InstantBuff permBuff)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(permBuff.GetBuffUID());
        }

        activeBuffs[subject].Remove(buff);
    }

    public void RemoveBuffById(int buffId, StatSubject subject)
    {
        if (activeBuffs.TryGetValue(subject, out var buffs))
        {
            var buff = buffs.FirstOrDefault(b => b.GetBuffUID() == buffId);
            if (buff != null)
            {
                RemoveBuff(buff, subject);
            }
        }
    }

    public void CleanUp()
    {
        foreach (var subject in activeBuffs.Keys.ToList())
        {
            RemoveAllBuffsFromSubject(subject);
        }
        activeBuffs.Clear();
    }

}
