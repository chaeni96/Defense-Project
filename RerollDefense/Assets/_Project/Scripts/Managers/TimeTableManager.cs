using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITimeChangeSubscriber 
{
    //�ð� ��ȭ�� ������ ��ü�� ITimeChangeSubscriber�� �����ؾߵ�
    abstract void OnChangeTime(int scheduleUID, float remainTime); //�����ð� 1�� �������� �����Ұ�. 
}
public interface IScheduleCompleteSubscriber
{

    // ������ �ϷḦ ������ ��ü�� IScheduleCompleteSubscriber�� ����
    // �������� ������ ��������

    abstract void OnCompleteSchedule(int scheduleUID);
}


public class TimeSchedule
{
    public int UID;             //����ũ ���̵�
    public double endTime;      //100������ : 100�� 1��
    public double currentTime;  //100������ : 100�� 1��


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
        // 1�� -> 100������ ��ȯ���ֱ� ���� *100
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
                // ������ �Ϸ�
                NotifyScheduleComplete(schedule.UID);
                registeredSchedules.RemoveAt(i);
            }
        }
    }

    //������ ����ϸ� UID ��ȯ����
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

    // ������ ��� �Լ� (�ʿ信 ���� �����ε�)
    public void AddTimeChangeSubscriber(ITimeChangeSubscriber subscriber)
    {
        if (!timeChangeSubscribers.Contains(subscriber))
        {
            timeChangeSubscribers.Add(subscriber);
        }
    }

    // Ư�� �����ٸ� ����
    public void AddTimeChangeTargetSubscriber(ITimeChangeSubscriber subscriber, int scheduleUID)
    {
        if (!targetTimeChangeSubscribers.ContainsKey(scheduleUID))
        {
            targetTimeChangeSubscribers.Add(scheduleUID, subscriber);
        }
    }

    // ���� ����
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
        // ���� �����ڵ鿡�� �˸�

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
