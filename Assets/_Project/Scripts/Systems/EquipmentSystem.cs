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

    // ���� ���� ��ȸ
    bool IsSlotAvailable(UnitController unit, int slotIndex);

    D_ItemData GetAndRemoveEquippedItem(UnitController unit, int slotIndex = 0);
}

public class EquipmentSystem : MonoBehaviour, IEquipmentSystem
{
    // ���� ID -> ���� �ε��� -> (������ ������ ID, ������ ���� ID) ����
    private Dictionary<string, Dictionary<int, (BGId dataId, string uniqueId)>> equippedItems =
        new Dictionary<string, Dictionary<int, (BGId, string)>>();

    // ������ ���� ID -> (���� ID, ���� �ε���) ������ ��
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

        // ���� ID�� ������ ����
        if (string.IsNullOrEmpty(uniqueItemId))
        {
            uniqueItemId = System.Guid.NewGuid().ToString();
        }

        if (!equippedItems.ContainsKey(unitId))
        {
            equippedItems[unitId] = new Dictionary<int, (BGId, string)>();
        }

        // ���Կ� �̹� �������� �ִ��� Ȯ��
        if (equippedItems[unitId].ContainsKey(slotIndex))
        {
            // ���� ������ ����
            var (_, oldUniqueId) = equippedItems[unitId][slotIndex];
            // ���� ID �������� ������ ��ġ ���� ����
            if (!string.IsNullOrEmpty(oldUniqueId) && itemLocationMap.ContainsKey(oldUniqueId))
            {
                itemLocationMap.Remove(oldUniqueId);
            }

            UnequipItem(unit, slotIndex);
        }

        // �� �������� �̹� �ٸ� ���� �����Ǿ� �ִ��� Ȯ��
        if (itemLocationMap.ContainsKey(uniqueItemId))
        {
            var (oldUnitId, oldSlot) = itemLocationMap[uniqueItemId];

            if (oldUnitId == unitId && oldSlot == slotIndex)
            {
                // ������ ��ġ�� ������ �õ��ϴ� ��� - ����
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
        equippedItems[unitId][slotIndex] = (itemDataId, uniqueItemId);
        itemLocationMap[uniqueItemId] = (unitId, slotIndex);

        // ���ֿ� ������ ���� ����
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

        // ������ ������ ���� ��������
        var (itemDataId, uniqueItemId) = equippedItems[unitId][slotIndex];

        // �ش� ������ ������ ��������
        D_ItemData itemData = inventoryManager.GetItemDataById(itemDataId);

        if (itemData != null)
        {
            // ���ֿ��� ������ ���� ����
            ApplyItemStatsToUnit(unit, itemData, false);

            // �������� �κ��丮�� ��ȯ
            inventoryManager.ReturnItemToInventory(itemDataId, itemData);

            // ������ ������ ���� ��Ȱ��ȭ
            unit.UnequipItemSlot();
        }

        // ���� ��Ͽ��� ����
        equippedItems[unitId].Remove(slotIndex);

        // ������ �ʿ����� ����
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

        // �� ���Կ� ���� ��ȸ (���纻�� ��ȸ�ؾ� ���� ����)
        var slotsCopy = new Dictionary<int, (BGId, string)>(equippedItems[unitId]);
        foreach (var slotPair in slotsCopy)
        {
            UnequipItem(unit, slotPair.Key);
        }

        // ��� �������� �����Ǿ����Ƿ� ���� ��Ʈ�� ����
        equippedItems.Remove(unitId);

        // ������ �ʿ��� �� ������ �׸�� ����
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

    public D_ItemData GetAndRemoveEquippedItem(UnitController unit, int slotIndex = 0)
    {
        if (unit == null || inventoryManager == null) return null;

        string unitId = unit.uniqueId;

        if (!equippedItems.ContainsKey(unitId) || !equippedItems[unitId].ContainsKey(slotIndex))
        {
            return null;
        }

        // ������ ������ ���� ��������
        var (itemDataId, uniqueItemId) = equippedItems[unitId][slotIndex];

        // �ش� ������ ������ ��������
        D_ItemData itemData = inventoryManager.GetItemDataById(itemDataId);

        if (itemData != null)
        {
            // ���ֿ��� ������ ���� ����
            ApplyItemStatsToUnit(unit, itemData, false);

            // ������ ������ ���� ��Ȱ��ȭ
            unit.UnequipItemSlot();
        }

        // ���� ��Ͽ��� ����
        equippedItems[unitId].Remove(slotIndex);

        // ������ �ʿ����� ����
        if (!string.IsNullOrEmpty(uniqueItemId) && itemLocationMap.ContainsKey(uniqueItemId))
        {
            itemLocationMap.Remove(uniqueItemId);
        }

        return itemData;
    }

    // ���� ID�� UnitController ã��
    private UnitController FindUnitById(string unitId)
    {
        // UnitManager�� ���� ���� ã��
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