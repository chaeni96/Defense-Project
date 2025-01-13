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

    // ���ϵ� ī�峻�� ��� ȿ�� ����
    public void ApplyWildCardEffect(D_WildCardData cardData)
    {
        if (cardData == null) return;

        // ���� �ν��� ȿ��
        // StatBooster ȿ�� ����

        if(cardData.f_StatBoosterData != null)
        {
            foreach (var statBooster in cardData.f_StatBoosterData)
            {
                ApplyStatBooster(statBooster);
            }
        }
    }

    // ���� �ν��� ����
    private void ApplyStatBooster(D_StatBoosterData statBoosterData)
    {
        if (statBoosterData == null) return;
        bool effectApplied = false;  // ȿ�� ����ƴ��� üũ


        // TargetUnitList�� ��������� �ý��� �������� ó��
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
            // ���� ����/��ų ó��
            //TODO : �Ѹ����� ���� �߰����ִ°͵� �ʿ�

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
            Debug.LogWarning("����� �������� ����");
        }

    }


    private void ApplySystemEffect(StatName statType, int value)
    {
        switch (statType)
        {
            // cost ȹ�� 
            case StatName.CostAdded:
                GameManager.Instance.AddCost(value);
                break;
            case StatName.RerollCost:
                // ���� ��� ���� ó��
                break;
        }
    }

    public void CleanUp()
    {
        // �ʿ��� ���� �۾�
    }


}
