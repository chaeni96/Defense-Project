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

    private int currentItemCount = 0;

    // �κ��丮 ������ ��� (������ ID�� ����)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // ������ ������ ĳ�� (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();


    // �κ��丮 ���� �̺�Ʈ
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    public event System.Action<int> OnItemCountUpdate;

    // �κ��丮 �ε� �� ĳ�� �ʱ�ȭ
    public void LoadInventory()
    {
        // �κ��丮 ������ �ʱ�ȭ
        inventoryItemCounts.Clear();
        itemDataCache.Clear();

        // �κ��丮 ���� �̺�Ʈ �߻�
        OnInventoryChanged?.Invoke();

        OnItemCountUpdate?.Invoke(currentItemCount);

    }

    // ������ �߰�
    public bool AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return false;

        // �κ��丮 �ִ� ���� Ȯ��
        int maxInventoryCount = Mathf.FloorToInt(GameManager.Instance.GetSystemStat(StatName.InventoryCount));

        // ���� ������ �ִ� �������� ũ�ų� ������ �߰� �Ұ�
        if (currentItemCount >= maxInventoryCount)
        {
            Debug.Log($"�κ��丮�� ���� á���ϴ�. �ִ� ����: {maxInventoryCount}");
            // ���⼭ �κ��丮 ������ �˸� UI�� ǥ���� �� �ֽ��ϴ�
            return false;
        }

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

        // ���� ������ ���� ����
        currentItemCount++;

        // �κ��丮 ���� �̺�Ʈ �߻�
        OnInventoryChanged?.Invoke();
        // ������ ���� ������Ʈ �̺�Ʈ �߻�
        OnItemCountUpdate?.Invoke(currentItemCount);

        Debug.Log($"������ '{itemData.f_name}'��(��) �κ��丮�� �߰��Ǿ����ϴ�. ���� ����: {inventoryItemCounts[itemId]}, �� ������ ����: {currentItemCount}/{maxInventoryCount}");

        return true;
    }

    // ������ �߰� (ItemData�κ��� ID ��������)
    public bool AddItem(D_ItemData itemData)
    {
        if (itemData == null) return false;

        // �������� BGId ��������
        BGId itemId = itemData.Id;

        // ������ �߰�
        return AddItem(itemId, itemData);
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
            // �κ��丮�� �߰� (���� ���� Ȯ��)
            bool success = AddItem(item);

            // �κ��丮 ���� á�� ��� ó�� 
            if (!success)
            {
                //TODO : �κ��丮 ���� á�� �� UI �����
                Debug.Log("�κ��丮�� ���� á���ϴ�!");
                
                
            }
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
    public bool RemoveItem(BGId itemId)
    {
        if (itemId == null) return false;

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

            // ���� ������ ���� ����
            currentItemCount--;

            // �κ��丮 ���� �̺�Ʈ �߻�
            OnInventoryChanged?.Invoke();
            // ������ ���� ������Ʈ �̺�Ʈ �߻�
            OnItemCountUpdate?.Invoke(currentItemCount);

            int maxInventoryCount = Mathf.FloorToInt(GameManager.Instance.GetSystemStat(StatName.InventoryCount));
            Debug.Log($"������ ID '{itemId}'�� �κ��丮���� ���ŵǾ����ϴ�. �� ������ ����: {currentItemCount}/{maxInventoryCount}");

            return true;
        }

        return false;
    }

    public void ReturnItemToInventory(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // �������� �κ��丮�� �߰�
        AddItem(itemId, itemData);

    }

}
