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



//�Ͻ����� ���� 
public class TemporaryBuff : BuffTimeBase, IScheduleCompleteSubscriber, ITimeChangeSubscriber
{

    //������ ȿ�� �ߵ��� �ð�
    private float lastTickTime;

    //���� ���� ���
    private StatSubject currentSubject;


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

        lastTickTime = Time.time;

        ApplyEffects(subject);
    }

    public override void ApplyEffects(StatSubject subject)
    {
        foreach (var effect in buffData.f_buffEffects)
        {
            // StatManager�� ���� ������� �˸�
            StatManager.Instance.BroadcastStatChange(subject, new StatStorage
            {
                stat = effect.f_statName,
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


        // ������ ȿ�� �ߵ� �� ƽ ���ݸ�ŭ �ð��� �������� üũ
        if (Time.time - lastTickTime >= buffData.f_tickInterval)
        {
            // ���� �ð� ���
            lastTickTime = Time.time;

            ApplyEffects(currentSubject);  // ȿ�� �ٽ� ����
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
                stat = effect.f_statName,
                value = effect.f_value,
                multiply = effect.f_valueMultiply
            });
        }
    }

    public override void RemoveEffects(StatSubject subject) { }

}
