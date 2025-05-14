using BGDatabaseEnum;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class Enemy : BasicObject
{
    private D_EnemyData enemyData;

    public Action OnUpdateDistanceCheck;
    public override void Initialize()
    {

        base.Initialize();
       

        isEnemy = true;
        // X �������� -1�� ���ؼ� �¿� ����
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

    }

    public override BasicObject GetNearestTarget()
    {
        return UnitManager.Instance.GetNearestUnit(transform.position);
    }

    public override List<BasicObject> GetTargetList()
    {
        // Convert List<UnitController> to List<BasicObject>
        List<UnitController> unitControllers = UnitManager.Instance.GetAllUnits();
        List<BasicObject> basicObjects = new List<BasicObject>(unitControllers);
        return basicObjects;
    }

    public void InitializeEnemyInfo(D_EnemyData data)
    {
        enemyData = data;

        baseStats.Clear();
        currentStats.Clear();

        // StatSubject�� ���� ���� �ջ�
        foreach (var subject in enemyData.f_statSubject)
        {
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);

            foreach (var stat in subjectStats)
            {
                if (!baseStats.ContainsKey(stat.statName))
                {
                    baseStats[stat.statName] = new StatStorage
                    {
                        statName = stat.statName,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    baseStats[stat.statName].value += stat.value;
                    baseStats[stat.statName].multiply *= stat.multiply;
                }
            }

            AddSubject(subject);
        }

        // ���� ���� �ʱ�ȭ
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                statName = baseStat.Value.statName,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }
        // currentHP�� maxHP�� �ʱ�ȭ
        if (!currentStats.ContainsKey(StatName.CurrentHp))
        {
            var maxHp = GetStat(StatName.MaxHP);
            currentStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }
        UpdateHpBar();
    }
    //�̺�Ʈ ���
    public void InitializeEvents(List<D_EventDummyData> events)
    {
        if (events == null || events.Count == 0) return;

        foreach (D_EventDummyData eventData in events)
        {
            // �̺�Ʈ ID
            string eventId = eventData.Id.ToString();

            // �̺�Ʈ ��ü ����
            IEvent gameEvent = EventManager.Instance.CreateEventFromData(eventData);

            if (gameEvent != null)
            {
                // �̺�Ʈ �Ŵ����� �̺�Ʈ ���
                EventManager.Instance.RegisterEvent(eventId, gameEvent);

                // �� ��ü�� �̺�Ʈ ����
                EventManager.Instance.AssignEventToObject(gameObject, eventData.f_eventTriggerType, eventId);
            }
        }
    }

    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (GetStat(StatName.CurrentHp) <= 0) return;  // �̹� �׾��ų� �״� ���̸� ���� ���� ����

        base.OnStatChanged(subject, statChange);
  
        // ü�� ���� ������ ����Ǿ��� ��
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            // HP �� ������Ʈ
            UpdateHpBar();
        }
    }

   

    // ���� ���� �޼���
    public void SetActive(bool active)
    {
        isActive = active;
    }

 

    public override void OnDead()
    {
        if (isDead == false) return;

        // �̺�Ʈ �Ŵ����� ���� OnDeath �̺�Ʈ Ʈ����
        EventManager.Instance.TriggerEvent(gameObject, EventTriggerType.OnDeath, transform.position);

        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(this);
        EnemyManager.Instance.NotifyEnemyDead();
        StageManager.Instance.UpdateEnemyCount(EnemyManager.Instance.GetActiveEnemyCount());

    }

}
