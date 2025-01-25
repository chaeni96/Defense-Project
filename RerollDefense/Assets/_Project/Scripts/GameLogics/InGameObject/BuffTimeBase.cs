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

    public override void RemoveEffects(StatSubject subject) 
    {

        // ���� ���� ���� ����
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        // ƽ ������ �־��ٸ� �ð� ��ȭ ������ ����
        if (buffData.f_tickInterval > 0)
        {
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(buffUID);
        }

        buffData = null;
    }


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

    public override void RemoveEffects(StatSubject subject)
    {
        // ������ �Ϸ� ���� ����
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(buffUID);

        buffData = null;

    }

}

//����(����) ���� �����ϰ� �����ϴ� Ŭ����
//���� ������ �޾� ������ ����Ǵ°Ŵ� BuffRangeObject �ݶ��̴� �ȿ������� ������Ʈ����
public class RangeBuff : BuffTimeBase
{
    private StatSubject casterSubject;  // ������ �����ϴ� ��ü -> BuffRangeObject�� �����ϴ� ��ü
    private List<BuffRangeObject> activeRangeObjects = new List<BuffRangeObject>();  // ���� Ȱ��ȭ�� ��� ���� ���� ������Ʈ
    private float buffRange = 1f;  // ���� ����
    
    public override void StartBuff(StatSubject subject)
    {
        if (buffData == null) return;

        casterSubject = subject;

        // ���� ������ ������Ʈ ��� ��������
        var subscribers = StatManager.Instance.GetAllSubscribers(casterSubject);

        foreach (var sub in subscribers)
        {
            var owner = sub as BasicObject;

            if (owner != null)
            {
                CreateBuffRange(owner);  // �� ������Ʈ���� ���� ���� ����
            }
        }
      
    }

    // ���� ���� ���� ������Ʈ ����
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

    // RangeBuff ��ü�� �������� ȿ�� �������� ����
    public override void ApplyEffects(StatSubject subject) { }

    // ���� ���� �� ��� ���� ���� ������Ʈ ����
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

