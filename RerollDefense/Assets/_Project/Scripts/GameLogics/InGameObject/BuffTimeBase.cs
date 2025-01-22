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



//�Ͻ����� ���� - �ð� ������� �������� ��ȭ�� �ִ� ����
public class TemporalBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    //���� ���� ���
    private StatSubject currentSubject;
    private float elapsedTime = 0f;  // ��� �ð�


    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        currentSubject = subject;

        // TimeTableManager�� ���� ���ӽð� ����ϰ� ���� ID �޾ƿ�
        buffUID = TimeTableManager.Instance.RegisterSchedule(buffData.f_duration);

        // ���� ���� ���� ����
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, buffUID);

        // ƽ ������ ������
        if (buffData.f_tickInterval > 0)
        {
            //�ð� ��ȭ ���� - �� ������ �ð� üũ��
            TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);
        }

    }

    public override void ApplyEffects(StatSubject subject)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            // StatManager�� ���� ������� �˸�
            StatManager.Instance.BroadcastStatChange(subject, new StatStorage
            {
                statName = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject) { }


    // �� �����Ӹ��� ȣ��Ǵ� �޼���

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID || buffData.f_tickInterval <= 0) return;

        // ��ü ���ӽð����� ���� �ð��� ���� ��� �ð� ���
        elapsedTime = buffData.f_duration - remainTime;

        // ��� �ð��� tick interval�� ���� ���� ������ �� ������ ȿ�� ����
        if (Mathf.FloorToInt(elapsedTime / buffData.f_tickInterval) >
            Mathf.FloorToInt((elapsedTime - Time.deltaTime) / buffData.f_tickInterval))
        {
            ApplyEffects(currentSubject);
        }
    }


    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID != buffUID) return;

        // ���� ���� ���� ����
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        // ƽ ������ �־��ٸ� �ð� ��ȭ ������ ����
        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);
        }

        buffData = null;
    }
}

//���� ���� -> �ѹ� ���� �ְ� ��
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
            //�ش� Subject�� ���� ���� ��� BasicObject���� �˸�
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

//TODO : �����ؾߵ� -> �ݶ��̴� üũ�� �߾ȵ�.
public class RangeBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{
    private StatSubject casterSubject;  // ������ ������ ������Ʈ, ���� ���� ����� ��ü
    private BuffRangeObject buffRangeObject;
    private HashSet<BasicObject> objectsInRange = new HashSet<BasicObject>();
    private float buffRange = 1f;
    private StatStorage buffEffect;

    //���� ���� ����
    private Dictionary<BasicObject, StatStorage> originalStats = new Dictionary<BasicObject, StatStorage>();

    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        casterSubject = subject;

        // ���� ȿ�� �̸� ���
        var effect = buffData.f_buffEffects.First();
        buffEffect = new StatStorage
        {
            statName = effect.f_statName,
            value = effect.f_value,
            multiply = effect.f_valueMultiply
        };

        // ���� ������ ������Ʈ ��� ��������
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

        // �ð� ��ȭ ����(ƽ ���͹��� �����ϰ� �׻� ����)
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, buffUID);
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID != buffUID) return;

        // BuffRangeObject�� ���� �ݶ��̴� ���� üũ
        if (buffRangeObject != null)
        {
            buffRangeObject.CheckRangeObjects();
        }

        // ƽ ������ �ִ� ��쿡�� �ֱ��� ȿ�� ����
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

        // ���� ���� �� ��� ������Ʈ�� ���� ����
        foreach (var obj in objectsInRange.ToList())
        {
            RemoveObjectInRange(obj);
        }

        // ���� ����
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

    // BuffRangeObject���� ���� �� ������Ʈ ������ ���� �޼���
    public void AddObjectInRange(BasicObject obj)
    {
        if (obj != null && !objectsInRange.Contains(obj))
        {
            // ���� ���� ����
            var currentStat = obj.currentStats[buffEffect.statName];
            originalStats[obj] = new StatStorage
            {
                statName = currentStat.statName,
                value = currentStat.value,
                multiply = currentStat.multiply
            };

            // ȿ�� ���� (ù ��° Subject����)
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
                // ���� �����ص� �������� �״�� ����
                obj.OnStatChanged(obj.subjects[0], originalStat);
                originalStats.Remove(obj);
            }
        }
    }
}

