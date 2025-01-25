using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 실제로 범위를 체크하고 효과를 적용하는 오브젝트
public class BuffRangeObject : MonoBehaviour, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    [SerializeField] private CircleCollider2D myCollider;
    [SerializeField] private SpriteRenderer rangeIndicator;

    private BasicObject ownerObject;              // 버프 소유자
    private D_BuffData buffData;                  // 버프 데이터
    private HashSet<BasicObject> affectedObjects; // 현재 버프 영향 받는 오브젝트들
    private Dictionary<BasicObject, StatStorage> originalStats;  // 각 오브젝트의 원래 스탯 저장
    private int scheduleUID = -1;                 // TimeTableManager용 고유 ID
    private float elapsedTime = 0f;               // 경과 시간

    private ContactFilter2D filter;               // 콜라이더 충돌 체크용 필터
    private List<Collider2D> overlappedColliders; // 현재 범위 내 콜라이더들

    public void Initialize(D_BuffData buffData, float radius, BasicObject owner)
    {
        overlappedColliders = new List<Collider2D>();
        affectedObjects = new HashSet<BasicObject>();
        originalStats = new Dictionary<BasicObject, StatStorage>();

        this.buffData = buffData;
        ownerObject = owner;

        // 콜라이더 크기 및 표시 설정
        myCollider.radius = radius;
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        }

        filter = new ContactFilter2D();
        filter.useTriggers = true;

        // TimeTableManager에 등록하여 시간 관리
        scheduleUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, scheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, scheduleUID);
    }

    public void OnChangeTime(int uid, float remainTime)
    {
        if (scheduleUID != uid) return;

        CheckRangeObjects();  // 범위 내 오브젝트 체크

        // 틱 간격이 있는 버프라면 주기적으로 효과 적용
        if (buffData.f_tickInterval > 0)
        {
            elapsedTime = buffData.f_duration - remainTime;
            if (Mathf.FloorToInt(elapsedTime / buffData.f_tickInterval) >
                Mathf.FloorToInt((elapsedTime - Time.deltaTime) / buffData.f_tickInterval))
            {
                // 모든 대상에게 버프 효과 적용
                foreach (var obj in affectedObjects)
                {
                    ApplyEffectToObject(obj);
                }
            }
        }
    }
    // 범위 내 오브젝트 검사
    private void CheckRangeObjects()
    {
        overlappedColliders.Clear();
        myCollider.OverlapCollider(filter, overlappedColliders);

        // 새로 범위에 들어온 오브젝트 체크
        foreach (var collider in overlappedColliders)
        {
            var obj = collider.GetComponent<BasicObject>();

            if (obj != null && obj.isEnemy != ownerObject.isEnemy)
            {
                AddObject(obj);
            }
        }

        // 범위를 벗어난 오브젝트 처리
        foreach (var obj in affectedObjects.ToList())
        {
            if (!overlappedColliders.Any(c => c.GetComponent<BasicObject>() == obj))
            {
                RemoveObject(obj);
            }
        }
    }

    // 새로운 오브젝트에 버프 적용
    private void AddObject(BasicObject obj)
    {
        if (!affectedObjects.Contains(obj))
        {
            // Fixed 타입 버프의 원래 스탯 저장
            foreach (var effect in buffData.f_buffEffects)
            {
                if (effect.f_buffTickType == BuffTickType.Fixed && obj.currentStats.TryGetValue(effect.f_statName, out var currentStat))
                {
                    originalStats[obj] = new StatStorage
                    {
                        statName = effect.f_statName,
                        value = currentStat.value,
                        multiply = currentStat.multiply
                    };
                }
            }

            affectedObjects.Add(obj);
            ApplyEffectToObject(obj);  // 버프 효과 적용
        }
    }

    private void ApplyEffectToObject(BasicObject obj)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            obj.OnStatChanged(obj.subjects[0], new StatStorage
            {
                statName = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    // 오브젝트에서 버프 제거
    private void RemoveObject(BasicObject obj)
    {
        if (affectedObjects.Contains(obj))
        {
            affectedObjects.Remove(obj);
            // Fixed 타입의 효과만 복원
            foreach (var effect in buffData.f_buffEffects)
            {
                if (effect.f_buffTickType == BuffTickType.Fixed &&
                   originalStats.TryGetValue(obj, out var originalStat))
                {
                    obj.OnStatChanged(obj.subjects[0], originalStat);
                    originalStats.Remove(obj);
                }
            }
        }
    }


    // 버프 종료 시 처리
    public void OnCompleteSchedule(int uid)
    {
        if (scheduleUID != uid) return;

        CleanUp();
        PoolingManager.Instance.ReturnObject(gameObject);
    }


    public void CleanUp()
    {
        if (scheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(scheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(scheduleUID);
        }

        // 모든 오브젝트의 스탯 복원
        foreach (var obj in affectedObjects.ToList())
        {
            RemoveObject(obj);
        }

        affectedObjects.Clear();
        originalStats.Clear();
        overlappedColliders.Clear();

        ownerObject = null;
        buffData = null;
        scheduleUID = -1;
    }


}
