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

        // 기존 데이터 정리
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
        // 이벤트 발생
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

    // 가장 가까운 유닛 찾기
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
            // 먼저 기존 타일 데이터 가져오기
            var existingTileData = TileMapManager.Instance.GetTileData(unit.tilePosition);

            if (existingTileData != null)
            {
                // 기존 타일 데이터의 placedUnit 초기화
                existingTileData.isAvailable = true;
                existingTileData.placedUnit = null;

                // 타일 데이터 업데이트
                TileMapManager.Instance.SetTileData(existingTileData);
            }

            unit.isActive = false;
            units.Remove(unit);
            PoolingManager.Instance.ReturnObject(unit.gameObject);
        }
    }


    // 게임 상태 변경 시 호출할 메서드
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

        // 유닛 리스트 정리
        if (units != null)
        {
            units.Clear();
        }
    }

    

}
