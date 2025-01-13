using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WildCardManager : MonoBehaviour
{
    private static WildCardManager _instance;

    public static WildCardManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<WildCardManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("WildCardManager");
                    _instance = singleton.AddComponent<WildCardManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // 와일드 카드내의 모든 효과 적용
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData == null) return;

        // 스탯 부스터 효과
        // StatBooster 효과 적용

        if(cardData.f_StatBoosterData != null)
        {
            foreach (var statBooster in cardData.f_StatBoosterData)
            {
                ApplyStatBooster(statBooster);
            }
        }
    }

    // 스탯 부스터 적용
    private void ApplyStatBooster(D_StatBoosterData statBoosterData)
    {
        if (statBoosterData == null) return;
        bool effectApplied = false;  // 효과 적용됐는지 체크


        // TargetUnitList가 비어있으면 시스템 스탯으로 처리
        if (statBoosterData.f_TargetUnitList == null)
        {
            foreach (var boosterStat in statBoosterData.f_BoosterStats)
            {
                ApplySystemEffect(boosterStat.f_StatName, boosterStat.f_IncreaseValue);
                effectApplied = true;
            }
        }
        else
        {
            // 유닛 스탯/스킬 처리
            //TODO : 한마리의 유닛 추가해주는것도 필요

            var units = UnitManager.Instance.GetUnits();
            foreach (var unit in units)
            {
                if (statBoosterData.f_TargetUnitList.Contains(unit.unitData))
                {
                    foreach (var boosterStat in statBoosterData.f_BoosterStats)
                    {
                        int currentValue = unit.GetStat(boosterStat.f_StatName);
                        unit.SetStatValue(boosterStat.f_StatName, currentValue + boosterStat.f_IncreaseValue);
                    }
                    unit.ApplyEffect();
                    effectApplied = true;
                }
            }
        }

        if (!effectApplied)
        {
            Debug.LogWarning("대상이 존재하지 않음");
        }

    }


    private void ApplySystemEffect(StatName statType, int value)
    {
        switch (statType)
        {
            // cost 획득 
            case StatName.CostAdded:
                GameManager.Instance.AddCost(value);
                break;
            case StatName.RerollCost:
                // 리롤 비용 관련 처리
                break;
        }
    }

    public void CleanUp()
    {
        // 필요한 정리 작업
    }


}
