using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ITimeChangeSubscriber 
{
    abstract void OnChangeTime(int scheduleUID, float remainTime); //�����ð� 1�� �������� �����Ұ�. 
}
public interface IScheduleCompleteSubscriber
{
    abstract void OnCompleteSchedule(int scheduleUID);
}


public abstract class BuffTimeBase
{
    protected int buffUID;
    protected BasicObject targetObject;
    public abstract void StartBuff();
}
public class DebuffBleeding : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{

    public override void StartBuff()
    {
        buffUID = TimeTableManager.Instance.RegisterSchedule(5f);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, buffUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);

        // ���� �����̻� +1 ���༭ � �ɷ��ִ��� �߰�
        targetObject.SetStatValue(StatName.Bleed, targetObject.GetStat(StatName.Bleed) + 1);
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        //�����̶� 1�ʸ��� �������� ��
        targetObject.SetStatValue(StatName.Health, targetObject.GetStat(StatName.Health) - 1);
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        targetObject.SetStatValue(StatName.Bleed, targetObject.GetStat(StatName.Bleed) - 1);

        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);
        TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);

        targetObject = null;
    }
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
    public void AddTimeChangeTargetSubscriber(ITimeChangeSubscriber subscriber, int scheduleUID)
    {
        if (!targetTimeChangeSubscribers.ContainsKey(scheduleUID))
        {
            targetTimeChangeSubscribers.Add(scheduleUID, subscriber);
        }
    }
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
