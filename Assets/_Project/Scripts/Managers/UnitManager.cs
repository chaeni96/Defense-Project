using BGDatabaseEnum;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitManager : MonoBehaviour
{
    private static UnitManager _instance;

    private List<UnitController> units = new List<UnitController>();

    public event System.Action OnUnitDeath;

    public static UnitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("UnitManager");
                    _instance = singleton.AddComponent<UnitManager>();
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

    public void InitializeManager()
    {

        // ���� ������ ����
        CleanUp();

        units = new List<UnitController>();

    }

    public List<UnitController> GetAllUnits() => units;

    public int GetActiveUnitCount()
    {
        return units.Count(unit => unit.isActive);
    }

    public void NotifyUnitDead(UnitController unit)
    {
        // �̺�Ʈ �߻�
        OnUnitDeath?.Invoke();
    }

    public void RegisterUnit(UnitController unit)
    {
        if (!units.Contains(unit))
        {
            unit.isActive = true;
            units.Add(unit);
        }
    }

    // ���� ����� ���� ã��
    public BasicObject GetNearestUnit(Vector2 position)
    {
        BasicObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (var unit in units)
        {
            if (unit == null || !unit.gameObject.activeInHierarchy) continue;

            float distance = Vector2.Distance(
                position,
                new Vector2(unit.transform.position.x, unit.transform.position.y)
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = unit;
            }
        }

        return nearest;
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (units.Contains(unit))
        {
            // ���� ���� Ÿ�� ������ ��������
            var existingTileData = TileMapManager.Instance.GetTileData(unit.tilePosition);

            if (existingTileData != null)
            {
                // ���� Ÿ�� �������� placedUnit �ʱ�ȭ
                existingTileData.isAvailable = true;
                existingTileData.placedUnit = null;

                // Ÿ�� ������ ������Ʈ
                TileMapManager.Instance.SetTileData(existingTileData);
            }

            unit.isActive = false;
            units.Remove(unit);
            PoolingManager.Instance.ReturnObject(unit.gameObject);
        }
    }


    // ���� ���� ���� �� ȣ���� �޼���
    public void SetUnitsActive(bool active)
    {
        foreach (var unit in units)
        {
            unit.SetActive(active);
        }
    }

    
    public void CleanUp()
    {
        for (int i = units.Count - 1; i >= 0; i--)
        {
            var unit = units[i];

            if (unit != null)
            {
                PoolingManager.Instance.ReturnObject(unit.gameObject);
            }
        }

        // ���� ����Ʈ ����
        if (units != null)
        {
            units.Clear();
        }
    }

    

}
