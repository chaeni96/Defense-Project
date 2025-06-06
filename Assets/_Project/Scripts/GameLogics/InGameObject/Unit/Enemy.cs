using BGDatabaseEnum;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BansheeGz.BGDatabase;
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
        // X 스케일을 -1배 곱해서 좌우 반전
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

    }

    public override BasicObject GetNearestTarget()
    {
        return UnitManager.Instance.GetNearestUnit(transform.position);
    }

    public override List<BasicObject> GetActiveTargetList()
    {
        // Convert List<UnitController> to List<BasicObject>
        List<UnitController> unitControllers = UnitManager.Instance.GetAllUnits();
        // 체력이 0보다 크고 활성화된 유닛만 필터링
        List<BasicObject> basicObjects = unitControllers
            .Where(unit => unit != null && 
                           unit.GetStat(StatName.CurrentHp) > 0 && 
                           unit.isActive)
            .Cast<BasicObject>()
            .ToList();
        return basicObjects;
    }

    public void InitializeEnemyInfo(D_EnemyData data)
    {
        enemyData = data;

        //fsmID 설정
        fsmObj.UpdateFSM(enemyData.f_fsmID);
        // 애니메이터 컨트롤러 설정
        SetAnimatorController(enemyData.f_animControllType);
        
        // 외형 설정 로직 추가
        if (appearanceProvider != null && enemyData.f_appearanceData != null)
        {
            // 기존에 unitAppearance가 null이면 새로 생성
            if (appearanceProvider.unitAppearance == null)
            {
                appearanceProvider.unitAppearance = new BGEntityGo();
            }

            // Entity 속성을 통해 외형 데이터를 설정
            appearanceProvider.unitAppearance.Entity = enemyData.f_appearanceData;

            // 외형 로드
            appearanceProvider.LoadAppearance();
        }
        
        
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
            // HP 바 업데이트
            UpdateHpBar();
        }
    }

   

    // 상태 변경 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }

 

    public override void OnDead()
    {
        if (isDead == false) return;

        // 이벤트 매니저를 통해 OnDeath 이벤트 트리거
        EventManager.Instance.TriggerEvent(gameObject, EventTriggerType.OnDeath, transform.position);

        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(this);
        EnemyManager.Instance.NotifyEnemyDead();
        StageManager.Instance.UpdateEnemyCount(EnemyManager.Instance.GetActiveEnemyCount());

    }

}
