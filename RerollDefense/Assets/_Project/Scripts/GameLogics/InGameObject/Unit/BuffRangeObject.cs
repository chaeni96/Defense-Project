using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ������ ������ üũ�ϰ� ȿ���� �����ϴ� ������Ʈ
public class BuffRangeObject : MonoBehaviour, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    [SerializeField] private CircleCollider2D myCollider;
    [SerializeField] private SpriteRenderer rangeIndicator;

    private BasicObject ownerObject;              // ���� ������
    private D_BuffData buffData;                  // ���� ������
    private HashSet<BasicObject> affectedObjects; // ���� ���� ���� �޴� ������Ʈ��
    private Dictionary<BasicObject, StatStorage> originalStats;  // �� ������Ʈ�� ���� ���� ����
    private int scheduleUID = -1;                 // TimeTableManager�� ���� ID
    private float elapsedTime = 0f;               // ��� �ð�

    private ContactFilter2D filter;               // �ݶ��̴� �浹 üũ�� ����
    private List<Collider2D> overlappedColliders; // ���� ���� �� �ݶ��̴���

    public void Initialize(D_BuffData buffData, float radius, BasicObject owner)
    {
        overlappedColliders = new List<Collider2D>();
        affectedObjects = new HashSet<BasicObject>();
        originalStats = new Dictionary<BasicObject, StatStorage>();

        this.buffData = buffData;
        ownerObject = owner;

        // �ݶ��̴� ũ�� �� ǥ�� ����
        myCollider.radius = radius;
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        }

        filter = new ContactFilter2D();
        filter.useTriggers = true;

        // TimeTableManager�� ����Ͽ� �ð� ����
        scheduleUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, scheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, scheduleUID);
    }

    public void OnChangeTime(int uid, float remainTime)
    {
        if (scheduleUID != uid) return;

        CheckRangeObjects();  // ���� �� ������Ʈ üũ

        // ƽ ������ �ִ� ������� �ֱ������� ȿ�� ����
        if (buffData.f_tickInterval > 0)
        {
            elapsedTime = buffData.f_duration - remainTime;
            if (Mathf.FloorToInt(elapsedTime / buffData.f_tickInterval) >
                Mathf.FloorToInt((elapsedTime - Time.deltaTime) / buffData.f_tickInterval))
            {
                // ��� ��󿡰� ���� ȿ�� ����
                foreach (var obj in affectedObjects)
                {
                    ApplyEffectToObject(obj);
                }
            }
        }
    }
    // ���� �� ������Ʈ �˻�
    private void CheckRangeObjects()
    {
        overlappedColliders.Clear();
        myCollider.OverlapCollider(filter, overlappedColliders);

        // ���� ������ ���� ������Ʈ üũ
        foreach (var collider in overlappedColliders)
        {
            var obj = collider.GetComponent<BasicObject>();

            if (obj != null && obj.isEnemy != ownerObject.isEnemy)
            {
                AddObject(obj);
            }
        }

        // ������ ��� ������Ʈ ó��
        foreach (var obj in affectedObjects.ToList())
        {
            if (!overlappedColliders.Any(c => c.GetComponent<BasicObject>() == obj))
            {
                RemoveObject(obj);
            }
        }
    }

    // ���ο� ������Ʈ�� ���� ����
    private void AddObject(BasicObject obj)
    {
        if (!affectedObjects.Contains(obj))
        {
            // Fixed Ÿ�� ������ ���� ���� ����
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
            ApplyEffectToObject(obj);  // ���� ȿ�� ����
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

    // ������Ʈ���� ���� ����
    private void RemoveObject(BasicObject obj)
    {
        if (affectedObjects.Contains(obj))
        {
            affectedObjects.Remove(obj);
            // Fixed Ÿ���� ȿ���� ����
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


    // ���� ���� �� ó��
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

        // ��� ������Ʈ�� ���� ����
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
