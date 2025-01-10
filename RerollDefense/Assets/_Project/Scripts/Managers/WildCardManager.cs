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
     

        //TODO : 시스템 부스터, 스킬 부스터 추가
    }

    // 스탯 부스터 적용
    private void ApplyStatBooster(D_StatBoosterData statBoosterData)
    {
        if (statBoosterData == null) return;

        var units = UnitManager.Instance.GetUnits();
        bool effectApplied = false;  // 효과 적용됐는지 체크

        foreach (var unit in units)
        {
            // 타겟 리스트에 있는 유닛인 경우
            if (statBoosterData.f_TargetUnitList.Contains(unit.unitData))
            {
                foreach (var boosterStat in statBoosterData.f_BoosterUnitStats)
                {
                    //적용 시킬 스탯 읽어오기
                    StatName statType = boosterStat.f_StatName;
                    int currentValue = unit.GetStat(statType);
                    int newValue = currentValue + boosterStat.f_IncreaseValue;
                    unit.SetStatValue(statType, newValue);
                }

                unit.ApplyEffect();
                effectApplied = true;  // 최소 하나의 유닛에 효과 적용됨
            }
        }

        // 효과가 적용된 유닛이 하나도 없다면 -> 알림팝업으로 띄워줘도될듯
        if (!effectApplied)
        {
            Debug.LogWarning("대상 유닛이 존재하지 않음");
        }
    }


    //TODO : 한마리의 유닛 추가해주는것도 필요


    public void CleanUp()
    {
        // 필요한 정리 작업
    }


}
