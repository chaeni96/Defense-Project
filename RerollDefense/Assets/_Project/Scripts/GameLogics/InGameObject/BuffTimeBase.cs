using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuffTimeBase
{
    protected int buffUID;
    protected BasicObject targetObject; //버프 받는 타겟 

    protected D_BuffData buffData;
    protected Dictionary<StatName, float> originalStats = new Dictionary<StatName, float>();

    public void Initialize(BasicObject target, D_BuffData data)
    {
        targetObject = target;
        buffData = data;
    }
    public abstract void StartBuff();


    protected virtual void ApplyEffects()
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            float currentValue = targetObject.GetStat(effect.f_statName);
            if (effect.f_tickType != BuffTickType.Periodic)
            {
                originalStats[effect.f_statName] = currentValue;
            }

            targetObject.SetStatValue(effect.f_statName, (int)effect.f_value);
        }
    }

    protected virtual void RemoveEffects()
    {
        foreach (var stat in originalStats)
        {
            targetObject.SetStatValue(stat.Key, (int)stat.Value);
        }
    }
}



//일시적인 버프 
public class TemporaryBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    private float lastTickTime;

    public override void StartBuff()
    {
        if (buffData == null || targetObject == null) return;

        buffUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, buffUID);

        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);
            lastTickTime = Time.time;
        }

        ApplyEffects();
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID != buffUID) return;

        RemoveEffects();
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);
        }

        targetObject = null;
        buffData = null;
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID || buffData.f_tickInterval <= 0) return;

        if (Time.time - lastTickTime >= buffData.f_tickInterval)
        {
            lastTickTime = Time.time;

            foreach (var effect in buffData.f_buffEffects)
            {
                if (effect.f_tickType == BuffTickType.Periodic)
                {
                    float currentValue = targetObject.GetStat(effect.f_statName);
                

                    targetObject.SetStatValue(effect.f_statName, (int)effect.f_value);
                }
            }
        }
    }
}

//영구 버프
public class PermanentBuff : BuffTimeBase
{
    public override void StartBuff()
    {
        if (buffData == null || targetObject == null) return;
        ApplyEffects();
    }
}

//TODO : 범위 기반 버프 -> 범위 기반이어도 일시적으로 버프를 줄수있음, IScheduleCompleteSubscriber, ITimeChangeSubscriber 구독 필요할수도.
public class AreaBuff : BuffTimeBase
{
    private float radius;
    private LayerMask targetLayer;

    public override void StartBuff()
    {
        if (buffData == null || targetObject == null) return;

        // 영역 효과 구현
        Collider2D[] colliders = Physics2D.OverlapCircleAll(targetObject.transform.position, radius, targetLayer);
        foreach (var collider in colliders)
        {
            var target = collider.GetComponent<BasicObject>();
            if (target != null)
            {
                ApplyEffects();
            }
        }
    }
}