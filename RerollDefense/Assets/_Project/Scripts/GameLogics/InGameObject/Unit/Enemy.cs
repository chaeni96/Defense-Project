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

public class DistanceChecker
{
    public BuffTimeBase linkedTimeBaseBuff;
    public float maxDistance;

    public void InitiliazeDistanceChecker(Enemy targetObject)
    {
        targetObject.OnUpdateDistanceCheck += OnUpdate;
    }

    public void OnUpdate()
    {

    }


}

public class Enemy : BasicObject
{

    public SpriteRenderer spriteRenderer;
    public Collider2D enemyCollider;

    [SerializeField] private EnemyType enemyType;//�ν����Ϳ��� ���ε����ֱ�
    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    public LineRenderer pathRenderer;  // Inspector���� �Ҵ�

    private D_EnemyData enemyData;

    private bool isActive;

    public Action OnUpdateDistanceCheck;

    public UnitController attackTarget = null;
    public bool isAttackAnimationPlaying = false;
    private float lastAttackTime = 0f;

    public override void Initialize()
    {
        base.Initialize();
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;

        UpdateHpBar();

        isEnemy = true;
        // X �������� -1�� ���ؼ� �¿� ����
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

    }

    public override BasicObject GetTarget()
    {
        return UnitManager.Instance.GetNearestUnit(transform.position);
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

        isActive = true;

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
            if (GetStat(StatName.CurrentHp) <= 0 && !isActive)
            {
                OnDead();
            }

            // HP �� ������Ʈ
            UpdateHpBar();
        }
    }

    private void UpdateHpBar()
    {
        float currentHp = GetStat(StatName.CurrentHp);
        float maxHp = GetStat(StatName.MaxHP);

        if (hpBar != null && maxHp > 0)
        {
            hpBar.value = currentHp / maxHp;
        }
    }

    // ���� ���� �޼���
    public void SetActive(bool active)
    {
        isActive = active;
    }

    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker�� ���ݷ� 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
                //HitEffect();
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            OnDead();
        }
    }

    public void OnDead()
    {
        // �̺�Ʈ �Ŵ����� ���� OnDeath �̺�Ʈ Ʈ����
        EventManager.Instance.TriggerEvent(gameObject, EventTriggerType.OnDeath, transform.position);

        isActive = false;
        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        PoolingManager.Instance.ReturnObject(gameObject);

        // ���� ���̺꿡 �� ���� �˸�
        StageManager.Instance.NotifyEnemyDecrease();
    }

}
