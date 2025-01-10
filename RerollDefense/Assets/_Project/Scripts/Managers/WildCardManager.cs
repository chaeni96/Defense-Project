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
     

        //TODO : �ý��� �ν���, ��ų �ν��� �߰�
    }

    // ���� �ν��� ����
    private void ApplyStatBooster(D_StatBoosterData statBoosterData)
    {
        if (statBoosterData == null) return;

        var units = UnitManager.Instance.GetUnits();
        bool effectApplied = false;  // ȿ�� ����ƴ��� üũ

        foreach (var unit in units)
        {
            // Ÿ�� ����Ʈ�� �ִ� ������ ���
            if (statBoosterData.f_TargetUnitList.Contains(unit.unitData))
            {
                foreach (var boosterStat in statBoosterData.f_BoosterUnitStats)
                {
                    //���� ��ų ���� �о����
                    StatName statType = boosterStat.f_StatName;
                    int currentValue = unit.GetStat(statType);
                    int newValue = currentValue + boosterStat.f_IncreaseValue;
                    unit.SetStatValue(statType, newValue);
                }

                unit.ApplyEffect();
                effectApplied = true;  // �ּ� �ϳ��� ���ֿ� ȿ�� �����
            }
        }

        // ȿ���� ����� ������ �ϳ��� ���ٸ� -> �˸��˾����� ����൵�ɵ�
        if (!effectApplied)
        {
            Debug.LogWarning("��� ������ �������� ����");
        }
    }


    //TODO : �Ѹ����� ���� �߰����ִ°͵� �ʿ�


    public void CleanUp()
    {
        // �ʿ��� ���� �۾�
    }


}
