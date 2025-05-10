using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public class SlotItemObject : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private GameObject equippedIndicator;

    private BGId itemId;
    private D_ItemData itemData;
    private AsyncOperationHandle<Sprite> spriteHandle;

    private int slotIndex = -1;

    // 드래그 앤 드롭 관련 변수
    private Transform originalParent;
    private Vector3 originalPosition;
    private bool isDragging = false;

    // 이벤트
    public delegate void ItemDroppedHandler(D_ItemData item);
    public event ItemDroppedHandler OnItemDropped;

    // 장비 시스템 참조
    private IEquipmentSystem equipmentSystem;

    private void Awake()
    {
        // 장비 시스템 참조 가져오기
        equipmentSystem = InventoryManager.Instance.GetEquipmentSystem();
    }

    public void InitializeSlot(BGId id, D_ItemData item)
    {
        if (item == null) return;

        itemId = id;
        itemData = item;

        slotIndex = -1;       // 일반 슬롯은 인덱스 없음

        // 아이콘 설정
        if (itemIcon != null && item.f_iconImage != null)
        {
            // 기존에 로드된 리소스가 있다면 해제
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.f_iconImage.f_addressableKey);
            spriteHandle.Completed += (handle) =>
            {
                // 이 시점에 gameObject가 파괴되었거나 itemIcon이 null인지 확인
                if (this == null || !this.gameObject || itemIcon == null)
                {
                    // 객체가 파괴된 경우, 로드한 리소스 해제하고 리턴
                    Addressables.Release(handle);
                    return;
                }

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    itemIcon.sprite = handle.Result;
                    itemIcon.enabled = true;
                }
                else
                {
                    Debug.LogError($"아이템 아이콘 로드 실패: {item.f_iconImage.f_addressableKey}");
                }
            };
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemData == null) return;

        isDragging = true;
        originalParent = transform.parent;
        originalPosition = transform.position;

        // UI 캔버스 최상위로 이동
        transform.SetParent(UIManager.Instance.fullWindowCanvas.transform);

        // 레이캐스트 차단
        itemIcon.raycastTarget = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // 레이캐스트 복원
        itemIcon.raycastTarget = true;

        bool validDrop = TryDropOnUnit(eventData);

        // 유효한 드롭이 아니면 원래 위치로 복귀
        if (!validDrop)
        {
            ReturnToOriginalPosition();
        }
    }

    // 유닛에 아이템 드롭 시도
    private bool TryDropOnUnit(PointerEventData eventData)
    {
        // 스크린 좌표를 월드 좌표로 변환
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPosition.z = 0;

        // 월드 좌표를 타일 좌표로 변환
        Vector2 tilePosition = TileMapManager.Instance.GetWorldToTilePosition(worldPosition);

        // 타일 위치에 있는 유닛 찾기
        TileData tileData = TileMapManager.Instance.GetTileData(tilePosition);

        // 타일에 유닛이 있는지 확인
        if (tileData != null && tileData.placedUnit != null && equipmentSystem != null && itemData != null)
        {
            UnitController unit = tileData.placedUnit;

            // 최대 슬롯 수 확인
            int maxSlots = (int)unit.GetStat(StatName.UnitInventoryCount);

            // 빈 슬롯 찾아 장착
            for (int i = 0; i < maxSlots; i++)
            {
                if (equipmentSystem.IsSlotAvailable(unit, i))
                {
                    equipmentSystem.EquipItem(unit, itemData, i);
                    Debug.Log($"아이템이 유닛 {unit.name}의 슬롯 {i}에 장착되었습니다.");
                    return true;
                }
            }
        }

        return false;
    }

    // 원래 위치로 돌아가기
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }

    private void OnDestroy()
    {
        // Addressable 해제
        if (spriteHandle.IsValid())
        {
            Addressables.Release(spriteHandle);
        }
    }
}