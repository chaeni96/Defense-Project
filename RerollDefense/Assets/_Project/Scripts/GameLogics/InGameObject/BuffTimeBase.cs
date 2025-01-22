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

    public override void RemoveEffects(StatSubject subject) { }


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

    public override void RemoveEffects(StatSubject subject) { }

}

//TODO : 수정해야됨 -> 콜라이더 체크가 잘안됨.
public class RangeBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    private StatSubject casterSubject;  // 버프를 시전할 오브젝트, 공간 버프 생기는 주체
    private BuffRangeObject buffRangeObject;
    private HashSet<BasicObject> objectsInRange = new HashSet<BasicObject>();
    private float buffRange = 1f;
    private StatStorage buffEffect;

    //원래 스탯 저장
    private Dictionary<BasicObject, StatStorage> originalStats = new Dictionary<BasicObject, StatStorage>();

    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        casterSubject = subject;

        // 버프 효과 미리 계산
        var effect = buffData.f_buffEffects.First();
        buffEffect = new StatStorage
        {
            statName = effect.f_statName,
            value = effect.f_value,
            multiply = effect.f_valueMultiply
        };

        // 버프 시전할 오브젝트 모두 가져오기
        var subscribers = StatManager.Instance.GetAllSubscribers(casterSubject);

        foreach (var sub in subscribers)
        {
            var owner = sub as BasicObject;

            if (owner != null)
            {
                CreateRangeCollider(owner.transform.position, owner);
            }
        }

        buffUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, buffUID);

        // 시간 변화 구독(틱 인터벌과 무관하게 항상 구독)
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID) return;

        // BuffRangeObject를 통해 콜라이더 범위 체크
        if (buffRangeObject != null)
        {
            buffRangeObject.CheckRangeObjects();
        }

        // 틱 간격이 있는 경우에만 주기적 효과 적용
        if (buffData.f_tickInterval > 0)
        {
            float elapsedTime = buffData.f_duration - remainTime;
            if (Mathf.FloorToInt(elapsedTime / buffData.f_tickInterval) >
                Mathf.FloorToInt((elapsedTime - Time.deltaTime) / buffData.f_tickInterval))
            {
                ApplyEffects(casterSubject);
            }
        }
    }

    public override void ApplyEffects(StatSubject subject)
    {
        if (buffData.f_tickInterval <= 0) return;

        foreach (var obj in objectsInRange.ToList())
        {
            if (obj != null)
            {
                obj.OnStatChanged(obj.subjects[0], buffEffect);
            }
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID != buffUID) return;

        // 버프 종료 시 모든 오브젝트의 스탯 복원
        foreach (var obj in objectsInRange.ToList())
        {
            RemoveObjectInRange(obj);
        }

        // 구독 해제
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);
        TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);

        if (buffRangeObject != null)
        {
            PoolingManager.Instance.ReturnObject(buffRangeObject.gameObject);
            buffRangeObject = null;
        }

        objectsInRange.Clear();
        originalStats.Clear();
        buffData = null;
    }

    public override void RemoveEffects(StatSubject subject) 
    {
        OnCompleteSchedule(buffUID);
    }

    private void CreateRangeCollider(Vector3 position, BasicObject owner)
    {
        GameObject buffRangeObj = PoolingManager.Instance.GetObject("BuffRangeObject", position);


        buffRangeObject = buffRangeObj.GetComponent<BuffRangeObject>();
        buffRangeObject.Initialize(this, buffRange, owner);
    }

    // BuffRangeObject에서 범위 내 오브젝트 추적을 위한 메서드
    public void AddObjectInRange(BasicObject obj)
    {
        if (obj != null && !objectsInRange.Contains(obj))
        {
            // 현재 스탯 저장
            var currentStat = obj.currentStats[buffEffect.statName];
            originalStats[obj] = new StatStorage
            {
                statName = currentStat.statName,
                value = currentStat.value,
                multiply = currentStat.multiply
            };

            // 효과 적용 (첫 번째 Subject에만)
            obj.OnStatChanged(obj.subjects[0], buffEffect);
            objectsInRange.Add(obj);
        }
    }

    public void RemoveObjectInRange(BasicObject obj)
    {
        if (obj != null && objectsInRange.Contains(obj))
        {
            objectsInRange.Remove(obj);

            if (originalStats.TryGetValue(obj, out var originalStat))
            {
                // 원래 저장해둔 스탯으로 그대로 복원
                obj.OnStatChanged(obj.subjects[0], originalStat);
                originalStats.Remove(obj);
            }
        }
    }
}

