using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITimeChangeSubscriber 
{
    //시간 변화를 구독할 객체는 ITimeChangeSubscriber를 구현해야됨
    abstract void OnChangeTime(int scheduleUID, float remainTime); //남은시간 1초 간격으로 전달할것. 
}
public interface IScheduleCompleteSubscriber
{

    // 스케줄 완료를 구독할 객체는 IScheduleCompleteSubscriber를 구현
    // 스케줄이 끝나면 통지받음

    abstract void OnCompleteSchedule(int scheduleUID);
}


public class TimeSchedule
{
    public int UID;             //유니크 아이디
    public double endTime;      //100배율임 : 100이 1초
    public double currentTime;  //100배율임 : 100이 1초


    public int lastNotifiedSecond = -1;
}
public class TimeTableManager : MonoBehaviour
{
    public static TimeTableManager _instance;

    public static TimeTableManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TimeTableManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("TimeTableManager");
                    _instance = singleton.AddComponent<TimeTableManager>();
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

    private List<TimeSchedule> registeredSchedules = new ();
    private List<ITimeChangeSubscriber> timeChangeSubscribers = new();
    private Dictionary<int, ITimeChangeSubscriber> targetTimeChangeSubscribers = new Dictionary<int, ITimeChangeSubscriber>();

    private List<IScheduleCompleteSubscriber> scheduleCompleteSubscribers = new();
    private Dictionary<int, IScheduleCompleteSubscriber> targetScheduleCompleteSubscribers = new Dictionary<int, IScheduleCompleteSubscriber>();

    private int uidCounter = 0;
    static readonly int SCHEDULE_UID_MAX = 9999999;
    private void Update()
    {
        // 1초 -> 100단위로 변환해주기 위해 *100
        double deltaTimeInHundred = Time.deltaTime * 100.0;

        for (int i = registeredSchedules.Count - 1; i >= 0; i--)
        {
            var schedule = registeredSchedules[i];
            schedule.currentTime += deltaTimeInHundred;

            if (schedule.currentTime < schedule.endTime)
            {
                double remainTime = (schedule.endTime - schedule.currentTime) / 100.0;
                int remainSecond = Mathf.CeilToInt((float)remainTime);

                if (schedule.lastNotifiedSecond != remainSecond)
                {
                    schedule.lastNotifiedSecond = remainSecond;
                    NotifyTimeChange(schedule.UID, remainSecond);
                }
            }
            else
            {
                // 스케줄 완료
                NotifyScheduleComplete(schedule.UID);
                registeredSchedules.RemoveAt(i);
            }
        }
    }

    //스케줄 등록하면 UID 반환받음
    public int RegisterSchedule(float endTimeInSecond)
    {
        int newUID = GetUniqueScheduleID();
        TimeSchedule newSchedule = new TimeSchedule
        {
            UID = newUID,
            endTime = endTimeInSecond * 100.0f,
            currentTime = 0.0f
        };
        registeredSchedules.Add(newSchedule);
        return newUID;
    }

    private int GetUniqueScheduleID()
    {
        uidCounter++;

        if (uidCounter >= SCHEDULE_UID_MAX) uidCounter = 0;
        return uidCounter;
    }
    public TimeSchedule GetSchedule(int scheduleUID)
    {
        return registeredSchedules.Find(s => s.UID == scheduleUID);
    }

    // 구독자 등록 함수 (필요에 따라 오버로드)
    public void AddTimeChangeSubscriber(ITimeChangeSubscriber subscriber)
    {
        if (!timeChangeSubscribers.Contains(subscriber))
        {
            timeChangeSubscribers.Add(subscriber);
        }
    }

    // 특정 스케줄만 구독
    public void AddTimeChangeTargetSubscriber(ITimeChangeSubscriber subscriber, int scheduleUID)
    {
        if (!targetTimeChangeSubscribers.ContainsKey(scheduleUID))
        {
            targetTimeChangeSubscribers.Add(scheduleUID, subscriber);
        }
    }

    // 구독 해제
    public void RemoveTimeChangeSubscriber(ITimeChangeSubscriber subscriber)
    {
        if (timeChangeSubscribers.Contains(subscriber))
        {
            timeChangeSubscribers.Remove(subscriber);
        }
    }
    public void RemoveTimeChangeTargetSubscriber(int scheduleUID)
    {
        if (!targetTimeChangeSubscribers.ContainsKey(scheduleUID))
        {
            targetTimeChangeSubscribers.Remove(scheduleUID);
        }
    }

    public void AddScheduleCompleteSubscriber(IScheduleCompleteSubscriber subscriber)
    {
        if (!scheduleCompleteSubscribers.Contains(subscriber))
        {
            scheduleCompleteSubscribers.Add(subscriber);
        }
    }
    public void AddScheduleCompleteTargetSubscriber(IScheduleCompleteSubscriber subscriber, int scheduleUID)
    {
        if (!targetScheduleCompleteSubscribers.ContainsKey(scheduleUID))
        {
            targetScheduleCompleteSubscribers.Add(scheduleUID, subscriber);
        }
    }
    public void RemoveScheduleCompleteSubscriber(IScheduleCompleteSubscriber subscriber)
    {
        if (scheduleCompleteSubscribers.Contains(subscriber))
        {
            scheduleCompleteSubscribers.Remove(subscriber);
        }
    }
    public void RemoveScheduleCompleteTargetSubscriber(int scheduleUID)
    {
        if (!targetScheduleCompleteSubscribers.ContainsKey(scheduleUID))
        {
            targetScheduleCompleteSubscribers.Remove(scheduleUID);
        }
    }
    private void NotifyTimeChange(int scheduleUID, float remainTime)
    {
        // 전역 구독자들에게 알림

        for (int i = 0; i < timeChangeSubscribers.Count; i++)
        {
            timeChangeSubscribers[i].OnChangeTime(scheduleUID, remainTime);
        }

        if (targetTimeChangeSubscribers.ContainsKey(scheduleUID))
        {
            targetTimeChangeSubscribers[scheduleUID].OnChangeTime(scheduleUID, remainTime);
        }
    }
    private void NotifyScheduleComplete(int scheduleUID)
    {
        for (int i = 0; i < scheduleCompleteSubscribers.Count; i++)
        {
            scheduleCompleteSubscribers[i].OnCompleteSchedule(scheduleUID);
        }
        if (targetScheduleCompleteSubscribers.ContainsKey(scheduleUID))
        {
            targetScheduleCompleteSubscribers[scheduleUID].OnCompleteSchedule(scheduleUID);
        }
    }

}
