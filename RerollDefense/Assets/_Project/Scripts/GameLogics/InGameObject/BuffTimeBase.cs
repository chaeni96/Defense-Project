using System.Collections;
using System.Collections.Generic;
using System.Linq;
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



//일시적인 버프 - 시간 기반으로 지속적인 변화를 주는 버프
public class TemporalBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    //버프 적용 대상
    private StatSubject currentSubject;
    private float elapsedTime = 0f;  // 경과 시간


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

    }

    public override void ApplyEffects(StatSubject subject)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            // StatManager를 통해 변경사항 알림
            StatManager.Instance.BroadcastStatChange(subject, new StatStorage
            {
                statName = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject) 
    {

        // 버프 종료 구독 해제
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        // 틱 간격이 있었다면 시간 변화 구독도 해제
        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);
        }

        buffData = null;
    }


    // 매 프레임마다 호출되는 메서드

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID || buffData.f_tickInterval <= 0) return;

        // 전체 지속시간에서 남은 시간을 빼서 경과 시간 계산
        elapsedTime = buffData.f_duration - remainTime;

        // 경과 시간을 tick interval로 나눈 값이 정수가 될 때마다 효과 적용
        if (Mathf.FloorToInt(elapsedTime / buffData.f_tickInterval) >
            Mathf.FloorToInt((elapsedTime - Time.deltaTime) / buffData.f_tickInterval))
        {
            ApplyEffects(currentSubject);
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
                statName = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject)
    {
        // 스케줄 완료 구독 해제
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        buffData = null;

    }

}

//범위(공간) 버프 생성하고 관리하는 클래스
//실제 버프를 받아 스탯이 변경되는거는 BuffRangeObject 콜라이더 안에들어오는 오브젝트들임
public class RangeBuff : BuffTimeBase
{
    private StatSubject casterSubject;  // 버프를 시전하는 주체 -> BuffRangeObject를 생성하는 주체
    private List<BuffRangeObject> activeRangeObjects = new List<BuffRangeObject>();  // 현재 활성화된 모든 범위 버프 오브젝트
    private float buffRange = 1f;  // 버프 범위
    
    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        casterSubject = subject;

        // 버프 시전할 오브젝트 모두 가져오기
        var subscribers = StatManager.Instance.GetAllSubscribers(casterSubject);

        foreach (var sub in subscribers)
        {
            var owner = sub as BasicObject;

            if (owner != null)
            {
                CreateBuffRange(owner);  // 각 오브젝트마다 범위 버프 생성
            }
        }
      
    }

    // 실제 범위 버프 오브젝트 생성
    private void CreateBuffRange(BasicObject owner)
    {
        var buffRangeObj = PoolingManager.Instance.GetObject("BuffRangeObject", owner.transform.position);
        var rangeObject = buffRangeObj.GetComponent<BuffRangeObject>();

        if (rangeObject != null)
        {
            rangeObject.Initialize(buffData, buffRange, owner);
            activeRangeObjects.Add(rangeObject);
        }
    }

    // RangeBuff 자체는 직접적인 효과 적용하지 않음
    public override void ApplyEffects(StatSubject subject) { }

    // 버프 종료 시 모든 범위 버프 오브젝트 제거
    public override void RemoveEffects(StatSubject subject)
    {
        foreach (var rangeObject in activeRangeObjects)
        {
            if (rangeObject != null)
            {
                rangeObject.CleanUp();
                PoolingManager.Instance.ReturnObject(rangeObject.gameObject);
            }
        }
        activeRangeObjects.Clear();
    }
}

