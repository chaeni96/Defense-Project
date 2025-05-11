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

    // 추가: 장비 시스템 참조
    private IEquipmentSystem equipmentSystem;

    // 인벤토리 아이템 목록 (아이템 데이터 ID와 수량)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // 아이템 데이터 캐시 (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();

    // 아이템 데이터 ID -> 인스턴스 고유 ID 목록
    private Dictionary<BGId, List<string>> inventoryItemIds = new Dictionary<BGId, List<string>>();

    // 인스턴스 고유 ID -> 아이템 데이터
    private Dictionary<string, D_ItemData> itemInstanceCache = new Dictionary<string, D_ItemData>();

    // 인벤토리 변경 이벤트
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    // 아이템 수집 완료 이벤트
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

            // 장비 시스템 생성 및 초기화
            GameObject equipSysObj = new GameObject("EquipmentSystem");
            equipSysObj.transform.SetParent(transform);
            EquipmentSystem equipSys = equipSysObj.AddComponent<EquipmentSystem>();
            equipSys.Initialize(this);
            equipmentSystem = equipSys;
        }
    }

    // 인벤토리 로드 및 캐시 초기화
    public void LoadInventory()
    {
        // 인벤토리 데이터 초기화
        inventoryItemCounts.Clear();
        itemDataCache.Clear();
        inventoryItemIds.Clear();
        itemInstanceCache.Clear();

        // 인벤토리 변경 이벤트 발생
        OnInventoryChanged?.Invoke();
    }

    // 아이템 추가 (고유 ID 생성 및 반환)
    public string AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return null;

        // 고유 ID 생성
        string uniqueId = System.Guid.NewGuid().ToString();

        // 캐시에 추가
        itemDataCache[itemId] = itemData;
        itemInstanceCache[uniqueId] = itemData;

        // ID 맵핑
        if (!inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId] = new List<string>();
        }
        inventoryItemIds[itemId].Add(uniqueId);

        // 인벤토리에 아이템 추가 (수량 증가)
        if (inventoryItemCounts.ContainsKey(itemId))
        {
            inventoryItemCounts[itemId]++;
        }
        else
        {
            inventoryItemCounts[itemId] = 1;
        }

        // 인벤토리 변경 이벤트 발생
        OnInventoryChanged?.Invoke();

        Debug.Log($"아이템 '{itemData.f_name}'이(가) 인벤토리에 추가되었습니다. 현재 수량: {inventoryItemCounts[itemId]}");

        return uniqueId;
    }

    // 아이템 추가 (ItemData로부터 ID 가져오기)
    public string AddItem(D_ItemData itemData)
    {
        if (itemData == null) return null;

        // 아이템의 BGId 가져오기
        BGId itemId = itemData.Id;

        // 아이템 추가
        return AddItem(itemId, itemData);
    }

    // 고유 ID로 아이템 추가 (장비 해제 시 사용)
    public void AddItemWithUniqueId(BGId itemId, D_ItemData itemData, string uniqueId)
    {
        if (itemId == null || itemData == null || string.IsNullOrEmpty(uniqueId)) return;

        // 이미 존재하는 고유 ID인지 확인
        if (itemInstanceCache.ContainsKey(uniqueId))
        {
            Debug.LogWarning($"이미 존재하는 고유 ID: {uniqueId}");
            return;
        }

        // 캐시에 추가
        itemDataCache[itemId] = itemData;
        itemInstanceCache[uniqueId] = itemData;

        // ID 맵핑
        if (!inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId] = new List<string>();
        }
        inventoryItemIds[itemId].Add(uniqueId);

        // 수량 관리
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

    // 아이템 데이터 가져오기
    public D_ItemData GetItemDataById(BGId itemId)
    {
        if (itemId == null) return null;

        if (itemDataCache.TryGetValue(itemId, out D_ItemData itemData))
        {
            return itemData;
        }

        return null;
    }

    // 고유 ID로 아이템 가져오기
    public D_ItemData GetItemDataByUniqueId(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId)) return null;

        if (itemInstanceCache.TryGetValue(uniqueId, out D_ItemData itemData))
        {
            return itemData;
        }

        return null;
    }

    // 장비 시스템 가져오기
    public IEquipmentSystem GetEquipmentSystem()
    {
        return equipmentSystem;
    }

    // 필드 아이템 수집 완료 처리 (FieldDropItemObject에서 호출)
    public void OnFieldItemCollected(D_ItemData item)
    {
        if (item != null)
        {
            // 인벤토리에 추가
            AddItem(item);

            // 아이템 수집 이벤트 발생
            OnItemCollected?.Invoke(item);
        }
    }

    // UI 갱신 메서드 (FullWindowInGameDlg에서 호출)
    public void RefreshInventoryUI(Transform slotParent, GameObject emptySlotPrefab)
    {
        if (slotParent == null || emptySlotPrefab == null) return;

        // 모든 자식 오브젝트 제거
        foreach (Transform child in slotParent)
        {
            Destroy(child.gameObject);
        }

        // 인벤토리 아이템에 따라 슬롯 생성
        foreach (var pair in inventoryItemIds)
        {
            BGId itemId = pair.Key;
            List<string> uniqueIds = pair.Value;

            if (itemDataCache.TryGetValue(itemId, out D_ItemData itemData))
            {
                // 각 인스턴스별로 슬롯 생성
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

    // BGId로 아이템 제거 (이전 방식, 호환성을 위해 유지)
    public void RemoveItem(BGId itemId)
    {
        if (itemId == null) return;

        if (inventoryItemCounts.ContainsKey(itemId) && inventoryItemIds.ContainsKey(itemId) && inventoryItemIds[itemId].Count > 0)
        {
            // 첫 번째 인스턴스 ID 가져오기
            string uniqueId = inventoryItemIds[itemId][0];
            // 해당 인스턴스 제거
            RemoveItemByUniqueId(uniqueId);
        }
    }

    // 고유 ID로 아이템 제거
    public void RemoveItemByUniqueId(string uniqueId)
    {
        if (string.IsNullOrEmpty(uniqueId) || !itemInstanceCache.ContainsKey(uniqueId)) return;

        D_ItemData itemData = itemInstanceCache[uniqueId];
        BGId itemId = itemData.Id;

        // 목록에서 제거
        if (inventoryItemIds.ContainsKey(itemId))
        {
            inventoryItemIds[itemId].Remove(uniqueId);

            // 수량 감소
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

        // 캐시에서 제거
        itemInstanceCache.Remove(uniqueId);

        OnInventoryChanged?.Invoke();

        Debug.Log($"아이템 ID '{uniqueId}'가 인벤토리에서 제거되었습니다.");
    }

    // 아이템을 인벤토리로 반환
    public void ReturnItemToInventory(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // 아이템을 인벤토리에 추가
        AddItem(itemId, itemData);
    }
}