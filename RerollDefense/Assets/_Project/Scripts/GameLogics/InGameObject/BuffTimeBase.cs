using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BuffTimeBase
{
    protected int buffUID;

    protected D_BuffData buffData;

    public int GetBuffUID() => buffUID;
    public D_BuffData GetBuffData() => buffData;

    public void Initialize(D_BuffData data)
    {
        buffData = data;
    }
    public abstract void StartBuff(StatSubject subject);
    public abstract void ApplyEffects(StatSubject subject);
    public abstract void RemoveEffects(StatSubject subject);

   

}



//일시적인 버프 
public class TemporaryBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{

    //마지막 효과 발동된 시간
    private float lastTickTime;

    //버프 적용 대상
    private StatSubject currentSubject;


    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        currentSubject = subject;

        // TimeTableManager에 버프 지속시간 등록하고 고유 ID 받아옴
        buffUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);

        // 버프 종료 시점 구독
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, buffUID);

        // 틱 간격이 있으면
        if (buffData.f_tickInterval > 0)
        {
            //시간 변화 구독 - 매 프레임 시간 체크용
            TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);
        }

        lastTickTime = Time.time;

        ApplyEffects(subject);
    }

    public override void ApplyEffects(StatSubject subject)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            // StatManager를 통해 변경사항 알림
            StatManager.Instance.BroadcastStatChange(subject, new StatStorage
            {
                stat = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject) { }


    // 매 프레임마다 호출되는 메서드

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID || buffData.f_tickInterval <= 0) return;


        // 마지막 효과 발동 후 틱 간격만큼 시간이 지났는지 체크
        if (Time.time - lastTickTime >= buffData.f_tickInterval)
        {
            // 현재 시간 기록
            lastTickTime = Time.time;

            ApplyEffects(currentSubject);  // 효과 다시 적용
        }
    }


    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID != buffUID) return;

        // 버프 종료 구독 해제
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        // 틱 간격이 있었다면 시간 변화 구독도 해제
        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);
        }

        buffData = null;
    }
}

//영구 버프 -> 한번 버프 주고 끝
public class InstantBuff : BuffTimeBase
{
    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;
        ApplyEffects(subject);
        buffData = null;
    }

    public override void ApplyEffects(StatSubject subject)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            //해당 Subject를 구독 중인 모든 BasicObject에게 알림
            StatManager.Instance.BroadcastStatChange(subject, new StatStorage
            {
                stat = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject) { }

}
