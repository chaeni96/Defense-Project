using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEquipmentSystem
{
    bool EquipItem(UnitController unit, D_ItemData item, int slotIndex = 0);
    bool UnequipItem(UnitController unit, int slotIndex = 0);
    bool UnequipItemById(UnitController unit, BGId itemId);
    D_ItemData GetEquippedItem(UnitController unit, int slotIndex = 0);
    List<D_ItemData> GetAllEquippedItems(UnitController unit);
    void UnequipAllItems(UnitController unit);

    // 슬롯 정보 조회
    bool IsSlotAvailable(UnitController unit, int slotIndex);
}

public class EquipmentSystem : MonoBehaviour, IEquipmentSystem
{
    //TODO : 딕셔너리 하나만 써도 된다면 하나만 쓰기 
    // 유닛 ID -> 슬롯 인덱스 -> 아이템 ID 매핑 => 각 유닛이 어떤 슬롯에 어떤 아이템 장착하고 있는지 저장, 슬롯마다 하나의 아이템 장착할수잇게끔
    private Dictionary<BGId, Dictionary<int, BGId>> equippedItems = new Dictionary<BGId, Dictionary<int, BGId>>();
    // 아이템 ID -> (유닛 ID, 슬롯 인덱스) 역참조 맵 => 특정 아이템 Id가 어떤 유닛의 어떤 슬롯에 장착되어있는지 파악하기 위함
    private Dictionary<BGId, (BGId unitId, int slotIndex)> itemLocationMap = new Dictionary<BGId, (BGId, int)>();

   

    private InventoryManager inventoryManager;

    public void Initialize(InventoryManager inventoryManager)
    {
        this.inventoryManager = inventoryManager;
    }

    public bool EquipItem(UnitController unit, D_ItemData item, int slotIndex = 0)
    {
        if (unit == null || item == null || inventoryManager == null) return false;

        BGId unitId = unit.unitData.Id;
        BGId itemId = item.Id;

        if (!equippedItems.ContainsKey(unitId))
        {
            equippedItems[unitId] = new Dictionary<int, BGId>();
        }

        // 이미 해당 슬롯에 아이템이 장착되어 있는지 확인
        if (equippedItems[unitId].ContainsKey(slotIndex))
        {
            // 기존 아이템 해제
            BGId oldItemId = equippedItems[unitId][slotIndex];
            UnequipItem(unit, slotIndex);
        }

        // 아이템이 이미 다른 곳에 장착되어 있는지 확인
        if (itemLocationMap.ContainsKey(itemId))
        {
            // 기존 장착 해제
            var (oldUnitId, oldSlot) = itemLocationMap[itemId];

            if (oldUnitId == unitId && oldSlot == slotIndex)
            {
                // 동일한 위치에 재장착 시도하는 경우 - 아무것도 하지 않음
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
        equippedItems[unitId][slotIndex] = itemId;
        itemLocationMap[itemId] = (unitId, slotIndex);

        // 유닛에 아이템 스탯 적용
        ApplyItemStatsToUnit(unit, item, true);

        return true;
    }

    public bool UnequipItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return false;

        BGId unitId = unit.unitData.Id;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return false;
        }

        // 장착된 아이템 ID 가져오기
        BGId itemId = equippedItems[unitId][slotIndex];

        // 해당 아이템 데이터 가져오기
        D_ItemData itemData = inventoryManager.GetItemDataById(itemId);

        if (itemData != null)
        {
            // 유닛에서 아이템 스탯 제거
            ApplyItemStatsToUnit(unit, itemData, false);

            // 아이템을 인벤토리로 반환
            inventoryManager.ReturnItemToInventory(itemId, itemData);

            // 유닛의 아이템 슬롯 비활성화
            unit.UnequipItemSlot();
        }

        // 장착 목록에서 제거
        equippedItems[unitId].Remove(slotIndex);

        // 역참조 맵에서도 제거
        if (itemLocationMap.ContainsKey(itemId))
        {
            itemLocationMap.Remove(itemId);
        }

        return true;
    }

    public bool UnequipItemById(UnitController unit, BGId itemId)
    {
        if (unit == null || itemId == null || !itemLocationMap.ContainsKey(itemId)) return false;

        var (unitId, slotIndex) = itemLocationMap[itemId];

        // 제공된 유닛의 아이템이 맞는지 확인
        if (unitId != unit.unitData.Id) return false;

        return UnequipItem(unit, slotIndex);
    }

    public D_ItemData GetEquippedItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return null;

        BGId unitId = unit.unitData.Id;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return null;
        }

        BGId itemId = equippedItems[unitId][slotIndex];
        return inventoryManager.GetItemDataById(itemId);
    }

    public List<D_ItemData> GetAllEquippedItems(UnitController unit)
    {
        List<D_ItemData> result = new List<D_ItemData>();

        if (unit == null || inventoryManager == null) return result;

        BGId unitId = unit.unitData.Id;

        if (!equippedItems.ContainsKey(unitId)) return result;

        foreach (var slotPair in equippedItems[unitId])
        {
            D_ItemData itemData = inventoryManager.GetItemDataById(slotPair.Value);
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

        BGId unitId = unit.unitData.Id;

        if (!equippedItems.ContainsKey(unitId)) return;

        // 각 슬롯에 대해 순회
        foreach (var slotPair in new Dictionary<int, BGId>(equippedItems[unitId]))
        {
            UnequipItem(unit, slotPair.Key);
        }

        // 모든 아이템이 해제되었으므로 유닛 엔트리 제거
        equippedItems.Remove(unitId);

        // 역참조 맵에서 이 유닛의 항목들 제거
        List<BGId> itemsToRemove = new List<BGId>();
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

        BGId unitId = unit.unitData.Id;

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

    // 유닛 ID로 UnitController 찾기
    private UnitController FindUnitById(BGId unitId)
    {
        // UnitManager를 통해 유닛 찾기
        foreach (var unit in UnitManager.Instance.GetAllUnits())
        {
            if (unit != null && unit.unitData != null && unit.unitData.Id == unitId)
            {
                return unit;
            }
        }

        return null;
    }
}