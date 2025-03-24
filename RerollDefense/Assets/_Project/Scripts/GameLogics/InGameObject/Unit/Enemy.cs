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
    
    [SerializeField] private EnemyType enemyType;//인스펙터에서 바인딩해주기
    [SerializeField] private Slider hpBar;  // Inspector에서 할당

    [SerializeField] private Canvas hpBarCanvas;  // Inspector에서 할당

    public LineRenderer pathRenderer;  // Inspector에서 할당

    private D_EnemyData enemyData;

    private bool isReach;
    private bool isActive;
    private Vector3 originalScale;

    public Action OnUpdateDistanceCheck;

    private void Update()
    {
        OnUpdateDistanceCheck?.Invoke();
    }
    public override void Initialize()
    {
        base.Initialize();
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        originalScale = transform.localScale;

        UpdateHpBar();

        InitializeLineRenderer();

    }

    private void InitializeLineRenderer()
    {
        pathRenderer.positionCount = 0;
        pathRenderer.startWidth = 0.03f;
        pathRenderer.endWidth = 0.03f;
        pathRenderer.sortingOrder = 1; // 경로가 타일맵 위에 그려지도록
    }

    public void InitializeEnemyInfo(D_EnemyData data)
    {
        isEnemy = true;

        enemyData = data;


        baseStats.Clear();
        currentStats.Clear();

        // StatSubject에 따른 스탯 합산
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

        // 현재 스탯 초기화
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                statName = baseStat.Value.statName,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }


        // currentHP를 maxHP로 초기화
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
        isReach = false;

        UpdateHpBar();
    }
    //이벤트 등록
    public void InitializeEvents(List<D_EventDummyData> events)
    {
        if (events == null || events.Count == 0) return;

        foreach (D_EventDummyData eventData in events)
        {
            // 이벤트 ID
            string eventId = eventData.Id.ToString();

            // 이벤트 객체 생성
            IEvent gameEvent = EventManager.Instance.CreateEventFromData(eventData);

            if (gameEvent != null)
            {
                // 이벤트 매니저에 이벤트 등록
                EventManager.Instance.RegisterEvent(eventId, gameEvent);

                // 적 객체에 이벤트 연결
                EventManager.Instance.AssignEventToObject(gameObject, eventData.f_eventTriggerType, eventId);
            }
        }
    }

    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (GetStat(StatName.CurrentHp) <= 0) return;  // 이미 죽었거나 죽는 중이면 스탯 변경 무시

        base.OnStatChanged(subject, statChange);
  
        // 체력 관련 스탯이 변경되었을 때
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            if (statChange.statName == StatName.CurrentHp)
            {
                // 데미지를 입었을 경우 깜빡이는 효과 적용
                DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                .Append(spriteRenderer.DOColor(Color.white, 0.1f))  // 0.1초 동안 원래 색으로
                .OnComplete(() =>
                {
                    // 깜빡임 효과가 끝난 후 체력이 0 이하인지 확인하고 죽음 처리
                    if (GetStat(StatName.CurrentHp) <= 0 && !isActive)
                    {
                        OnDead();
                    }
                });
            }

            // HP 바 업데이트
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

    // 상태 변경 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }


    public void OnReachEndTile()
    {
        //enemy의 공격력만큼 player의 hp감소 -> 스탯매니저 통해서 값 변경
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.CurrentHp,
            value = currentStats[StatName.ATK].value * -1 ,
            multiply = currentStats[StatName.ATK].multiply
        });

        isReach = true;
        OnDead();
    }

    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker의 공격력 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
                HitEffect();
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            OnDead();
        }
    }

    public void HitEffect()
    {

        // 기존 트윈이 실행 중이면 중단
        transform.DOKill(true);

        // 원래 크기로 초기화
        transform.localScale = originalScale;

        // 펀치 스케일 효과 적용
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 1)
            .SetEase(Ease.OutQuart);

        // 데미지를 입으면 빨간색으로 깜빡임
        if (spriteRenderer != null)
        {
            // 색상 변경 시퀀스
            DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1초 동안 원래 색으로
        }
    }

    public void OnDead()
    {
        //TODO : 나중에 수정 boss가 집에 안도착했을때 예외처리 필요함
        if (enemyType == EnemyType.Boss && !isReach)
        {
            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffectObject", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        // 이벤트 매니저를 통해 OnDeath 이벤트 트리거
        EventManager.Instance.TriggerEvent(gameObject, EventTriggerType.OnDeath, transform.position);

        isActive = false;
        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        PoolingManager.Instance.ReturnObject(gameObject);

        // 현재 웨이브에 적 감소 알림
        StageManager.Instance.NotifyEnemyDecrease();
    }

}
