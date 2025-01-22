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

    //���� ����� ������ -> ���߿� Ư�� subject ��� ���� �����ϰų�, Ư�� ID ���� ���ŵ� ����ϱ� ����
    private Dictionary<StatSubject, List<BuffTimeBase>> activeBuffs = new Dictionary<StatSubject, List<BuffTimeBase>>();

  
    //WildCard�� ������ ��� ���� �����ϴ� �޼��� -> ȣ���ϸ� ���� ����
    public void ApplyBuff(D_BuffData buffData, StatSubject subject, BasicObject owner = null)
    {
        //���� Ÿ�Կ� �´� ���� ��ü ����
        var buff = CreateBuff(buffData.f_buffType);
        if (buff == null) return;


        if (!activeBuffs.ContainsKey(subject))
        {
            activeBuffs[subject] = new List<BuffTimeBase>();
        }

        //���� �ʱ�ȭ
        buff.Initialize(buffData);
        //���� ����
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

    //���� ���� ���� �޼����
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
