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

    // ���� ���� ��ȸ
    bool IsSlotAvailable(UnitController unit, int slotIndex);
}

public class EquipmentSystem : MonoBehaviour, IEquipmentSystem
{
    //TODO : ��ųʸ� �ϳ��� �ᵵ �ȴٸ� �ϳ��� ���� 
    // ���� ID -> ���� �ε��� -> ������ ID ���� => �� ������ � ���Կ� � ������ �����ϰ� �ִ��� ����, ���Ը��� �ϳ��� ������ �����Ҽ��հԲ�
    private Dictionary<BGId, Dictionary<int, BGId>> equippedItems = new Dictionary<BGId, Dictionary<int, BGId>>();
    // ������ ID -> (���� ID, ���� �ε���) ������ �� => Ư�� ������ Id�� � ������ � ���Կ� �����Ǿ��ִ��� �ľ��ϱ� ����
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

        // �̹� �ش� ���Կ� �������� �����Ǿ� �ִ��� Ȯ��
        if (equippedItems[unitId].ContainsKey(slotIndex))
        {
            // ���� ������ ����
            BGId oldItemId = equippedItems[unitId][slotIndex];
            UnequipItem(unit, slotIndex);
        }

        // �������� �̹� �ٸ� ���� �����Ǿ� �ִ��� Ȯ��
        if (itemLocationMap.ContainsKey(itemId))
        {
            // ���� ���� ����
            var (oldUnitId, oldSlot) = itemLocationMap[itemId];

            if (oldUnitId == unitId && oldSlot == slotIndex)
            {
                // ������ ��ġ�� ������ �õ��ϴ� ��� - �ƹ��͵� ���� ����
                return true;
            }

            // �ٸ� ��ġ�� �����Ǿ� �ִٸ� ����
            UnitController oldUnit = FindUnitById(oldUnitId);
            if (oldUnit != null)
            {
                UnequipItem(oldUnit, oldSlot);
            }
        }

        // �� ������ ����
        equippedItems[unitId][slotIndex] = itemId;
        itemLocationMap[itemId] = (unitId, slotIndex);

        // ���ֿ� ������ ���� ����
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

        // ������ ������ ID ��������
        BGId itemId = equippedItems[unitId][slotIndex];

        // �ش� ������ ������ ��������
        D_ItemData itemData = inventoryManager.GetItemDataById(itemId);

        if (itemData != null)
        {
            // ���ֿ��� ������ ���� ����
            ApplyItemStatsToUnit(unit, itemData, false);

            // �������� �κ��丮�� ��ȯ
            inventoryManager.ReturnItemToInventory(itemId, itemData);

            // ������ ������ ���� ��Ȱ��ȭ
            unit.UnequipItemSlot();
        }

        // ���� ��Ͽ��� ����
        equippedItems[unitId].Remove(slotIndex);

        // ������ �ʿ����� ����
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

        // ������ ������ �������� �´��� Ȯ��
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

        // �� ���Կ� ���� ��ȸ
        foreach (var slotPair in new Dictionary<int, BGId>(equippedItems[unitId]))
        {
            UnequipItem(unit, slotPair.Key);
        }

        // ��� �������� �����Ǿ����Ƿ� ���� ��Ʈ�� ����
        equippedItems.Remove(unitId);

        // ������ �ʿ��� �� ������ �׸�� ����
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
            // ������ �������, ������ 1ĭ�� ����ؼ� ��� 1�� ������ StatName.UnitInventoryCount�� �ص���
            //return slotIndex >= 0 && slotIndex < unit.GetStat(StatName.UnitInventoryCount);
            return slotIndex >= 0 && slotIndex < 1;
        }

        // ������ �̹� ��� ��
        return false;
    }

    // ������ ������ ���ֿ� ����/����
    private void ApplyItemStatsToUnit(UnitController unit, D_ItemData item, bool isEquipping)
    {
        if (unit == null || item == null) return;

        // �������� ���� ���� ����
        foreach (var stat in item.f_stats)
        {
            // ���� �� ���� �߰�, ���� �� ���� ����
            int statValue = isEquipping ? stat.f_statValue : -stat.f_statValue;
            float statMultiply = isEquipping ? stat.f_valueMultiply : 1f / stat.f_valueMultiply;

            // ���ֿ� ���� ���� ����
            unit.ModifyStat(stat.f_statName, statValue, statMultiply);
        }
    }

    // ���� ID�� UnitController ã��
    private UnitController FindUnitById(BGId unitId)
    {
        // UnitManager�� ���� ���� ã��
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