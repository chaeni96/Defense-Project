using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 딜레이 후 버프 적용을 위한 클래스
public class ScheduleBuffApplier : IScheduleCompleteSubscriber
{
    private D_BuffData buffData;
    private string description;

    public ScheduleBuffApplier(D_BuffData buffData, string description)
    {
        this.buffData = buffData;
        this.description = description;

    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject, description);
    }
}


public class WildCardManager : MonoBehaviour
{
    private static WildCardManager _instance;

    public event Action OnWildCardSelected;  //와일드카드 선택 이벤트

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

    // 스탯 부스터 적용
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData.f_BuffData == null) return;

        string description = cardData.f_Description; // 와일드카드 설명 가져오기

        foreach (var buffData in cardData.f_BuffData)
        {
            // 딜레이가 0이면 즉시 적용
            if (buffData.f_startDelayTime <= 0)
            {
                // 버프의 대상 Subject에 따라 적용
                BuffManager.Instance.ApplyBuff(buffData, buffData.f_targetSubject, description);
            }
            else
            {
                // 딜레이가 있으면 TimeTableManager를 통해 예약
                ScheduleBuffApplier applier = new ScheduleBuffApplier(buffData, description);
                int scheduleUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_startDelayTime);
                TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(applier, scheduleUID);
            }      
        }

        OnWildCardSelected?.Invoke();
    }


  
    public void CleanUp()
    {
        // 필요한 정리 작업
    }


}
