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
        }

     
    }

    // 인벤토리 아이템 목록 (아이템 ID와 수량)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // 아이템 데이터 캐시 (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();

    // 장착 중인 아이템 (유닛 ID -> 아이템 ID 목록) -> 나중에 유닛에 장착하면서 추가할 부분
    //private Dictionary<BGId, List<BGId>> equippedItems = new Dictionary<BGId, List<BGId>>();


    // 인벤토리 변경 이벤트
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    // 아이템 수집 완료 이벤트
    public delegate void ItemCollectedHandler(D_ItemData item);
    public event ItemCollectedHandler OnItemCollected;

    // 인벤토리 로드 및 캐시 초기화
    public void LoadInventory()
    {
        // 인벤토리 데이터 초기화
        inventoryItemCounts.Clear();
        itemDataCache.Clear();
        //equippedItems.Clear();

        // 인벤토리 변경 이벤트 발생
        OnInventoryChanged?.Invoke();
    }


    // 아이템 추가
    public void AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // 아이템 데이터 캐시에 추가 또는 업데이트
        itemDataCache[itemId] = itemData;

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
    }

    // 아이템 추가 (ItemData로부터 ID 가져오기)
    public void AddItem(D_ItemData itemData)
    {
        if (itemData == null) return;

        // 아이템의 BGId 가져오기
        BGId itemId = itemData.Id;

        // 아이템 추가
        AddItem(itemId, itemData);
    }

    // 아이템 제거
    public bool RemoveItem(BGId itemId)
    {
        if (itemId == null) return false;

        // 인벤토리에 아이템이 있는지 확인
        if (inventoryItemCounts.TryGetValue(itemId, out int count))
        {
            // 아이템 수량이 1 이상이면 1 감소
            if (count > 0)
            {
                inventoryItemCounts[itemId]--;

                // 수량이 0이 되면 인벤토리에서 제거
                if (inventoryItemCounts[itemId] <= 0)
                {
                    inventoryItemCounts.Remove(itemId);
                    // 아이템 데이터 캐시는 유지 (나중에 다시 사용할 수 있음)
                }

                // 인벤토리 변경 이벤트 발생
                OnInventoryChanged?.Invoke();

                D_ItemData itemData = itemDataCache[itemId];
                Debug.Log($"아이템 '{itemData.f_name}'이(가) 인벤토리에서 제거되었습니다. 남은 수량: {(inventoryItemCounts.ContainsKey(itemId) ? inventoryItemCounts[itemId] : 0)}");

                return true;
            }
        }

        return false;
    }


    // 모든 인벤토리 아이템 가져오기
    public Dictionary<BGId, D_ItemData> GetAllItems()
    {
        Dictionary<BGId, D_ItemData> result = new Dictionary<BGId, D_ItemData>();

        foreach (var pair in inventoryItemCounts)
        {
            if (itemDataCache.TryGetValue(pair.Key, out D_ItemData itemData))
            {
                result[pair.Key] = itemData;
            }
        }

        return result;
    }

    // 아이템 갯수 가져오기
    public int GetItemCount(BGId itemId)
    {
        if (itemId == null) return 0;

        if (inventoryItemCounts.TryGetValue(itemId, out int count))
        {
            return count;
        }

        return 0;
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
        foreach (var pair in inventoryItemCounts)
        {
            if (itemDataCache.TryGetValue(pair.Key, out D_ItemData itemData))
            {
                // 각 아이템마다 수량만큼 슬롯 생성
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
}
