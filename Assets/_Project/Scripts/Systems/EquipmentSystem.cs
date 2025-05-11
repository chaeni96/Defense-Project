using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEquipmentSystem
{
    bool EquipItem(UnitController unit, D_ItemData item, int slotIndex = 0, string uniqueItemId = null);
    bool UnequipItem(UnitController unit, int slotIndex = 0);
    D_ItemData GetEquippedItem(UnitController unit, int slotIndex = 0);
    List<D_ItemData> GetAllEquippedItems(UnitController unit);
    void UnequipAllItems(UnitController unit);

    // 슬롯 정보 조회
    bool IsSlotAvailable(UnitController unit, int slotIndex);

    D_ItemData GetAndRemoveEquippedItem(UnitController unit, int slotIndex = 0);
}

public class EquipmentSystem : MonoBehaviour, IEquipmentSystem
{
    // 유닛 ID -> 슬롯 인덱스 -> (아이템 데이터 ID, 아이템 고유 ID) 매핑
    private Dictionary<string, Dictionary<int, (BGId dataId, string uniqueId)>> equippedItems =
        new Dictionary<string, Dictionary<int, (BGId, string)>>();

    // 아이템 고유 ID -> (유닛 ID, 슬롯 인덱스) 역참조 맵
    private Dictionary<string, (string unitId, int slotIndex)> itemLocationMap =
        new Dictionary<string, (string, int)>();

    private InventoryManager inventoryManager;

    public void Initialize(InventoryManager inventoryManager)
    {
        this.inventoryManager = inventoryManager;
    }

    public bool EquipItem(UnitController unit, D_ItemData item, int slotIndex = 0, string uniqueItemId = null)
    {
        if (unit == null || item == null || inventoryManager == null) return false;

        string unitId = unit.uniqueId;
        BGId itemDataId = item.Id;

        // 고유 ID가 없으면 생성
        if (string.IsNullOrEmpty(uniqueItemId))
        {
            uniqueItemId = System.Guid.NewGuid().ToString();
        }

        if (!equippedItems.ContainsKey(unitId))
        {
            equippedItems[unitId] = new Dictionary<int, (BGId, string)>();
        }

        // 슬롯에 이미 아이템이 있는지 확인
        if (equippedItems[unitId].ContainsKey(slotIndex))
        {
            // 기존 아이템 해제
            var (_, oldUniqueId) = equippedItems[unitId][slotIndex];
            // 고유 ID 기준으로 아이템 위치 정보 제거
            if (!string.IsNullOrEmpty(oldUniqueId) && itemLocationMap.ContainsKey(oldUniqueId))
            {
                itemLocationMap.Remove(oldUniqueId);
            }

            UnequipItem(unit, slotIndex);
        }

        // 이 아이템이 이미 다른 곳에 장착되어 있는지 확인
        if (itemLocationMap.ContainsKey(uniqueItemId))
        {
            var (oldUnitId, oldSlot) = itemLocationMap[uniqueItemId];

            if (oldUnitId == unitId && oldSlot == slotIndex)
            {
                // 동일한 위치에 재장착 시도하는 경우 - 무시
                return true;
            }

            // 다른 위치에 장착되어 있다면 해제
            UnitController oldUnit = FindUnitById(oldUnitId);
            if (oldUnit != null)
            {
                UnequipItem(oldUnit, oldSlot);
            }
        }

        // 새 아이템 장착
        equippedItems[unitId][slotIndex] = (itemDataId, uniqueItemId);
        itemLocationMap[uniqueItemId] = (unitId, slotIndex);

        // 유닛에 아이템 스탯 적용
        ApplyItemStatsToUnit(unit, item, true);

        return true;
    }

    public bool UnequipItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return false;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return false;
        }

        // 장착된 아이템 정보 가져오기
        var (itemDataId, uniqueItemId) = equippedItems[unitId][slotIndex];

        // 해당 아이템 데이터 가져오기
        D_ItemData itemData = inventoryManager.GetItemDataById(itemDataId);

        if (itemData != null)
        {
            // 유닛에서 아이템 스탯 제거
            ApplyItemStatsToUnit(unit, itemData, false);

            // 아이템을 인벤토리로 반환
            inventoryManager.ReturnItemToInventory(itemDataId, itemData);

            // 유닛의 아이템 슬롯 비활성화
            unit.UnequipItemSlot();
        }

        // 장착 목록에서 제거
        equippedItems[unitId].Remove(slotIndex);

        // 역참조 맵에서도 제거
        if (!string.IsNullOrEmpty(uniqueItemId) && itemLocationMap.ContainsKey(uniqueItemId))
        {
            itemLocationMap.Remove(uniqueItemId);
        }

        return true;
    }

    public D_ItemData GetEquippedItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return null;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return null;
        }

        BGId itemDataId = equippedItems[unitId][slotIndex].dataId;
        return inventoryManager.GetItemDataById(itemDataId);
    }

    public List<D_ItemData> GetAllEquippedItems(UnitController unit)
    {
        List<D_ItemData> result = new List<D_ItemData>();

        if (unit == null || inventoryManager == null) return result;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId)) return result;

        foreach (var slotPair in equippedItems[unitId])
        {
            D_ItemData itemData = inventoryManager.GetItemDataById(slotPair.Value.dataId);
            if (itemData != null)
            {
                result.Add(itemData);
            }
        }

        return result;
    }

    public void UnequipAllItems(UnitController unit)
    {
        if (unit == null) return;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId)) return;

        // 각 슬롯에 대해 순회 (복사본을 순회해야 수정 가능)
        var slotsCopy = new Dictionary<int, (BGId, string)>(equippedItems[unitId]);
        foreach (var slotPair in slotsCopy)
        {
            UnequipItem(unit, slotPair.Key);
        }

        // 모든 아이템이 해제되었으므로 유닛 엔트리 제거
        equippedItems.Remove(unitId);

        // 역참조 맵에서 이 유닛의 항목들 제거
        List<string> itemsToRemove = new List<string>();
        foreach (var pair in itemLocationMap)
        {
            if (pair.Value.unitId == unitId)
            {
                itemsToRemove.Add(pair.Key);
            }
        }

        foreach (var itemId in itemsToRemove)
        {
            itemLocationMap.Remove(itemId);
        }
    }

    public bool IsSlotAvailable(UnitController unit, int slotIndex)
    {
        if (unit == null) return false;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            // 슬롯이 비어있음, 지금은 1칸만 사용해서 상수 1로 하지만 StatName.UnitInventoryCount로 해도됨
            //return slotIndex >= 0 && slotIndex < unit.GetStat(StatName.UnitInventoryCount);
            return slotIndex >= 0 && slotIndex < 1;
        }

        // 슬롯이 이미 사용 중
        return false;
    }

    // 아이템 스탯을 유닛에 적용/제거
    private void ApplyItemStatsToUnit(UnitController unit, D_ItemData item, bool isEquipping)
    {
        if (unit == null || item == null) return;

        // 아이템의 스탯 변경 적용
        foreach (var stat in item.f_stats)
        {
            // 장착 시 스탯 추가, 해제 시 스탯 제거
            int statValue = isEquipping ? stat.f_statValue : -stat.f_statValue;
            float statMultiply = isEquipping ? stat.f_valueMultiply : 1f / stat.f_valueMultiply;

            // 유닛에 직접 스탯 적용
            unit.ModifyStat(stat.f_statName, statValue, statMultiply);
        }
    }

    public D_ItemData GetAndRemoveEquippedItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return null;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return null;
        }

        // 장착된 아이템 정보 가져오기
        var (itemDataId, uniqueItemId) = equippedItems[unitId][slotIndex];

        // 해당 아이템 데이터 가져오기
        D_ItemData itemData = inventoryManager.GetItemDataById(itemDataId);

        if (itemData != null)
        {
            // 유닛에서 아이템 스탯 제거
            ApplyItemStatsToUnit(unit, itemData, false);

            // 유닛의 아이템 슬롯 비활성화
            unit.UnequipItemSlot();
        }

        // 장착 목록에서 제거
        equippedItems[unitId].Remove(slotIndex);

        // 역참조 맵에서도 제거
        if (!string.IsNullOrEmpty(uniqueItemId) && itemLocationMap.ContainsKey(uniqueItemId))
        {
            itemLocationMap.Remove(uniqueItemId);
        }

        return itemData;
    }

    // 유닛 ID로 UnitController 찾기
    private UnitController FindUnitById(string unitId)
    {
        // UnitManager를 통해 유닛 찾기
        foreach (var unit in UnitManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.uniqueId == unitId)
            {
                return unit;
            }
        }

        return null;
    }
}