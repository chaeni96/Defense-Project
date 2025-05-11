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

    private int currentItemCount = 0;

    // 인벤토리 아이템 목록 (아이템 ID와 수량)
    private Dictionary<BGId, int> inventoryItemCounts = new Dictionary<BGId, int>();

    // 아이템 데이터 캐시 (BGId -> ItemData)
    private Dictionary<BGId, D_ItemData> itemDataCache = new Dictionary<BGId, D_ItemData>();


    // 인벤토리 변경 이벤트
    public delegate void InventoryChangedHandler();
    public event InventoryChangedHandler OnInventoryChanged;

    public event System.Action<int> OnItemCountUpdate;

    // 인벤토리 로드 및 캐시 초기화
    public void LoadInventory()
    {
        // 인벤토리 데이터 초기화
        inventoryItemCounts.Clear();
        itemDataCache.Clear();

        // 인벤토리 변경 이벤트 발생
        OnInventoryChanged?.Invoke();

        OnItemCountUpdate?.Invoke(currentItemCount);

    }

    // 아이템 추가
    public bool AddItem(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return false;

        // 인벤토리 최대 개수 확인
        int maxInventoryCount = Mathf.FloorToInt(GameManager.Instance.GetSystemStat(StatName.InventoryCount));

        // 현재 개수가 최대 개수보다 크거나 같으면 추가 불가
        if (currentItemCount >= maxInventoryCount)
        {
            Debug.Log($"인벤토리가 가득 찼습니다. 최대 개수: {maxInventoryCount}");
            // 여기서 인벤토리 가득참 알림 UI를 표시할 수 있습니다
            return false;
        }

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

        // 현재 아이템 개수 증가
        currentItemCount++;

        // 인벤토리 변경 이벤트 발생
        OnInventoryChanged?.Invoke();
        // 아이템 개수 업데이트 이벤트 발생
        OnItemCountUpdate?.Invoke(currentItemCount);

        Debug.Log($"아이템 '{itemData.f_name}'이(가) 인벤토리에 추가되었습니다. 현재 수량: {inventoryItemCounts[itemId]}, 총 아이템 개수: {currentItemCount}/{maxInventoryCount}");

        return true;
    }

    // 아이템 추가 (ItemData로부터 ID 가져오기)
    public bool AddItem(D_ItemData itemData)
    {
        if (itemData == null) return false;

        // 아이템의 BGId 가져오기
        BGId itemId = itemData.Id;

        // 아이템 추가
        return AddItem(itemId, itemData);
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
            // 인벤토리에 추가 (성공 여부 확인)
            bool success = AddItem(item);

            // 인벤토리 가득 찼을 경우 처리 
            if (!success)
            {
                //TODO : 인벤토리 가득 찼을 때 UI 만들기
                Debug.Log("인벤토리가 가득 찼습니다!");
                
                
            }
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

    // 인벤토리에서 아이템 제거
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

            // 현재 아이템 개수 감소
            currentItemCount--;

            // 인벤토리 변경 이벤트 발생
            OnInventoryChanged?.Invoke();
            // 아이템 개수 업데이트 이벤트 발생
            OnItemCountUpdate?.Invoke(currentItemCount);

            int maxInventoryCount = Mathf.FloorToInt(GameManager.Instance.GetSystemStat(StatName.InventoryCount));
            Debug.Log($"아이템 ID '{itemId}'가 인벤토리에서 제거되었습니다. 총 아이템 개수: {currentItemCount}/{maxInventoryCount}");

            return true;
        }

        return false;
    }

    public void ReturnItemToInventory(BGId itemId, D_ItemData itemData)
    {
        if (itemId == null || itemData == null) return;

        // 아이템을 인벤토리에 추가
        AddItem(itemId, itemData);

    }

}
