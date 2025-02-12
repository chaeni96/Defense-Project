using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 딜레이 후 버프 적용을 위한 클래스
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

    // 와일드 카드내의 모든 효과 적용
  
    // 스탯 부스터 적용
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null) return;

        foreach (var buffData in cardData.f_BuffData)
        {
            // 딜레이가 0이면 즉시 적용
            if (buffData.f_startDelayTime <= 0)
            {
                // 버프의 대상 Subject에 따라 적용
                BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject, cardData.f_Description);
            }
            else
            {
                // 딜레이가 있으면 TimeTableManager를 통해 예약
                int scheduleUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_startDelayTime);
                TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(new ScheduleBuffApplier(buffData, cardData.f_Description), scheduleUID);
            }      
        }
    }


  
    public void CleanUp()
    {
        // 필요한 정리 작업
    }


}
