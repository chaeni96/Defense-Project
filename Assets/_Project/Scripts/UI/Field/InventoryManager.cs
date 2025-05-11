using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager _instance;

    public static InventoryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<InventoryManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("InventoryManager");
                    _instance = singleton.AddComponent<InventoryManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    // �߰�: ��� �ý��� ����
    private IEquipmentSystem equipmentSystem;

    // �κ��丮 ������ ��� (������ ������ ID�� ����)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // ������ ������ ĳ�� (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();

    // ������ ������ ID -> �ν��Ͻ� ���� ID ���
    private Dictionary<BGId, List<string>> inventoryItemIds = new Dictionary<BGId, List<string>>();

    // �ν��Ͻ� ���� ID -> ������ ������
    private Dictionary<string, D_ItemData> itemInstanceCache = new Dictionary<string, D_ItemData>();

    // �κ��丮 ���� �̺�Ʈ
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    // ������ ���� �Ϸ� �̺�Ʈ
    public delegate void ItemCollectedHandler(D_ItemData item);
    public event ItemCollectedHandler OnItemCollected;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);

            // ��� �ý��� ���� �� �ʱ�ȭ
            GameObject equipSysObj = new GameObject("EquipmentSystem");
            equipSysObj.transform.SetParent(transform);
            EquipmentSystem equipSys = equipSysObj.AddComponent<EquipmentSystem>();
            equipSys.Initialize(this);
            equipmentSystem = equipSys;
        }
    }

    // �κ��丮 �ε� �� ĳ�� �ʱ�ȭ
    public void LoadInventory()
    {
        // �κ��丮 ������ �ʱ�ȭ
        inventoryItemCounts.Clear();
        itemDataCache.Clear();
        inventoryItemIds.Clear();
        itemInstanceCache.Clear();

        // �κ��丮 ���� �̺�Ʈ �߻�
        OnInventoryChanged?.Invoke();
    }

    // ������ �߰� (���� ID ���� �� ��ȯ)
    public string AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return null;

        // ���� ID ����
        string uniqueId = System.Guid.NewGuid().ToString();

        // ĳ�ÿ� �߰�
        itemDataCache[itemId] = itemData;
        itemInstanceCache[uniqueId] = itemData;

        // ID ����
        if (!inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId] = new List<string>();
        }
        inventoryItemIds[itemId].Add(uniqueId);

        // �κ��丮�� ������ �߰� (���� ����)
        if (inventoryItemCounts.ContainsKey(itemId))
        {
            inventoryItemCounts[itemId]++;
        }
        else
        {
            inventoryItemCounts[itemId] = 1;
        }

        // �κ��丮 ���� �̺�Ʈ �߻�
        OnInventoryChanged?.Invoke();

        Debug.Log($"������ '{itemData.f_name}'��(��) �κ��丮�� �߰��Ǿ����ϴ�. ���� ����: {inventoryItemCounts[itemId]}");

        return uniqueId;
    }

    // ������ �߰� (ItemData�κ��� ID ��������)
    public string AddItem(D_ItemData itemData)
    {
        if (itemData == null) return null;

        // �������� BGId ��������
        BGId itemId = itemData.Id;

        // ������ �߰�
        return AddItem(itemId, itemData);
    }

    // ���� ID�� ������ �߰� (��� ���� �� ���)
    public void AddItemWithUniqueId(BGId itemId, D_ItemData itemData, string uniqueId)
    {
        if (itemId == null || itemData == null || string.IsNullOrEmpty(uniqueId)) return;

        // �̹� �����ϴ� ���� ID���� Ȯ��
        if (itemInstanceCache.ContainsKey(uniqueId))
        {
            Debug.LogWarning($"�̹� �����ϴ� ���� ID: {uniqueId}");
            return;
        }

        // ĳ�ÿ� �߰�
        itemDataCache[itemId] = itemData;
        itemInstanceCache[uniqueId] = itemData;

        // ID ����
        if (!inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId] = new List<string>();
        }
        inventoryItemIds[itemId].Add(uniqueId);

        // ���� ����
        if (inventoryItemCounts.ContainsKey(itemId))
        {
            inventoryItemCounts[itemId]++;
        }
        else
        {
            inventoryItemCounts[itemId] = 1;
        }

        OnInventoryChanged?.Invoke();
    }

    // ������ ������ ��������
    public D_ItemData GetItemDataById(BGId itemId)
    {
        if (itemId == null) return null;

        if (itemDataCache.TryGetValue(itemId, out D_ItemData itemData))
        {
            return itemData;
        }

        return null;
    }

    // ���� ID�� ������ ��������
    public D_ItemData GetItemDataByUniqueId(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return null;

        if (itemInstanceCache.TryGetValue(uniqueId, out D_ItemData itemData))
        {
            return itemData;
        }

        return null;
    }

    // ��� �ý��� ��������
    public IEquipmentSystem GetEquipmentSystem()
    {
        return equipmentSystem;
    }

    // �ʵ� ������ ���� �Ϸ� ó�� (FieldDropItemObject���� ȣ��)
    public void OnFieldItemCollected(D_ItemData item)
    {
        if (item != null)
        {
            // �κ��丮�� �߰�
            AddItem(item);

            // ������ ���� �̺�Ʈ �߻�
            OnItemCollected?.Invoke(item);
        }
    }

    // UI ���� �޼��� (FullWindowInGameDlg���� ȣ��)
    public void RefreshInventoryUI(Transform slotParent, GameObject emptySlotPrefab)
    {
        if (slotParent == null || emptySlotPrefab == null) return;

        // ��� �ڽ� ������Ʈ ����
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        // �κ��丮 �����ۿ� ���� ���� ����
        foreach (var pair in inventoryItemIds)
        {
            BGId itemId = pair.Key;
            List<string> uniqueIds = pair.Value;

            if (itemDataCache.TryGetValue(itemId, out D_ItemData itemData))
            {
                // �� �ν��Ͻ����� ���� ����
                foreach (string uniqueId in uniqueIds)
                {
                    GameObject slotObj = Instantiate(emptySlotPrefab, slotParent);
                    SlotItemObject slotScript = slotObj.GetComponent<SlotItemObject>();

                    if (slotScript != null)
                    {
                        slotScript.InitializeSlot(itemId, itemData, uniqueId);
                    }
                }
            }
        }
    }

    // BGId�� ������ ���� (���� ���, ȣȯ���� ���� ����)
    public void RemoveItem(BGId itemId)
    {
        if (itemId == null) return;

        if (inventoryItemCounts.ContainsKey(itemId) && inventoryItemIds.ContainsKey(itemId) && inventoryItemIds[itemId].Count > 0)
        {
            // ù ��° �ν��Ͻ� ID ��������
            string uniqueId = inventoryItemIds[itemId][0];
            // �ش� �ν��Ͻ� ����
            RemoveItemByUniqueId(uniqueId);
        }
    }

    // ���� ID�� ������ ����
    public void RemoveItemByUniqueId(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId) || !itemInstanceCache.ContainsKey(uniqueId)) return;

        D_ItemData itemData = itemInstanceCache[uniqueId];
        BGId itemId = itemData.Id;

        // ��Ͽ��� ����
        if (inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId].Remove(uniqueId);

            // ���� ����
            if (inventoryItemCounts.ContainsKey(itemId) && inventoryItemCounts[itemId] > 0)
            {
                inventoryItemCounts[itemId]--;

                if (inventoryItemCounts[itemId] == 0)
                {
                    inventoryItemCounts.Remove(itemId);
                    inventoryItemIds.Remove(itemId);
                }
            }
        }

        // ĳ�ÿ��� ����
        itemInstanceCache.Remove(uniqueId);

        OnInventoryChanged?.Invoke();

        Debug.Log($"������ ID '{uniqueId}'�� �κ��丮���� ���ŵǾ����ϴ�.");
    }

    // �������� �κ��丮�� ��ȯ
    public void ReturnItemToInventory(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // �������� �κ��丮�� �߰�
        AddItem(itemId, itemData);
    }
}