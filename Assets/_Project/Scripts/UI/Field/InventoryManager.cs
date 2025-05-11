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

    // �κ��丮 ������ ��� (������ ID�� ����)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // ������ ������ ĳ�� (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();


    // �κ��丮 ���� �̺�Ʈ
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    // ������ ���� �Ϸ� �̺�Ʈ
    public delegate void ItemCollectedHandler(D_ItemData item);
    public event ItemCollectedHandler OnItemCollected;

    // �κ��丮 �ε� �� ĳ�� �ʱ�ȭ
    public void LoadInventory()
    {
        // �κ��丮 ������ �ʱ�ȭ
        inventoryItemCounts.Clear();
        itemDataCache.Clear();

        // �κ��丮 ���� �̺�Ʈ �߻�
        OnInventoryChanged?.Invoke();
    }

    // ������ �߰�
    public void AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // ������ ������ ĳ�ÿ� �߰� �Ǵ� ������Ʈ
        itemDataCache[itemId] = itemData;

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
    }

    // ������ �߰� (ItemData�κ��� ID ��������)
    public void AddItem(D_ItemData itemData)
    {
        if (itemData == null) return;

        // �������� BGId ��������
        BGId itemId = itemData.Id;

        // ������ �߰�
        AddItem(itemId, itemData);
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
        foreach (var pair in inventoryItemCounts)
        {
            if (itemDataCache.TryGetValue(pair.Key, out D_ItemData itemData))
            {
                // �� �����۸��� ������ŭ ���� ����
                for (int i = 0; i < pair.Value; i++)
                {
                    GameObject slotObj = Instantiate(emptySlotPrefab, slotParent);
                    SlotItemObject slotScript = slotObj.GetComponent<SlotItemObject>();

                    if (slotScript != null)
                    {
                        slotScript.InitializeSlot(pair.Key, itemData);
                    }
                }
            }
        }
    }

    // �κ��丮���� ������ ����
    public void RemoveItem(BGId itemId)
    {
        if (itemId == null) return;

        if (inventoryItemCounts.ContainsKey(itemId))
        {
            if (inventoryItemCounts[itemId] > 1)
            {
                inventoryItemCounts[itemId]--;
            }
            else
            {
                inventoryItemCounts.Remove(itemId);
            }

            // �κ��丮 ���� �̺�Ʈ �߻�
            OnInventoryChanged?.Invoke();

            Debug.Log($"������ ID '{itemId}'�� �κ��丮���� ���ŵǾ����ϴ�.");
        }
    }

    public void ReturnItemToInventory(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // �������� �κ��丮�� �߰�
        AddItem(itemId, itemData);

    }

}
