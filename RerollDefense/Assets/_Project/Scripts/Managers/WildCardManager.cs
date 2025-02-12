using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ������ �� ���� ������ ���� Ŭ����
public class ScheduleBuffApplier : IScheduleCompleteSubscriber
{
    private D_BuffData buffData;
    private string buffDescription;

    public ScheduleBuffApplier(D_BuffData buffData, string description)
    {
        this.buffData = buffData;
        this.buffDescription = description; 
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject, buffDescription);
    }
}


public class WildCardManager : MonoBehaviour
{
    private static WildCardManager _instance;

    public static WildCardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WildCardManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("WildCardManager");
                    _instance = singleton.AddComponent<WildCardManager>();
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
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ���ϵ� ī�峻�� ��� ȿ�� ����
  
    // ���� �ν��� ����
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null) return;

        foreach (var buffData in cardData.f_BuffData)
        {
            // �����̰� 0�̸� ��� ����
            if (buffData.f_startDelayTime <= 0)
            {
                // ������ ��� Subject�� ���� ����
                BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject, cardData.f_Description);
            }
            else
            {
                // �����̰� ������ TimeTableManager�� ���� ����
                int scheduleUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_startDelayTime);
                TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(new ScheduleBuffApplier(buffData, cardData.f_Description), scheduleUID);
            }      
        }
    }


  
    public void CleanUp()
    {
        // �ʿ��� ���� �۾�
    }


}
