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

    // �巡�� �� ��� ���� ����
    private Transform originalParent;
    private Vector3 originalPosition;
    private bool isDragging = false;

    // �̺�Ʈ
    public delegate void ItemDroppedHandler(D_ItemData item);
    public event ItemDroppedHandler OnItemDropped;

    // ��� �ý��� ����
    private IEquipmentSystem equipmentSystem;

    private void Awake()
    {
        // ��� �ý��� ���� ��������
        equipmentSystem = InventoryManager.Instance.GetEquipmentSystem();
    }

    public void InitializeSlot(BGId id, D_ItemData item)
    {
        if (item == null) return;

        itemId = id;
        itemData = item;

        slotIndex = -1;       // �Ϲ� ������ �ε��� ����

        // ������ ����
        if (itemIcon != null && item.f_iconImage != null)
        {
            // ������ �ε�� ���ҽ��� �ִٸ� ����
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }

            spriteHandle = Addressables.LoadAssetAsync<Sprite>(item.f_iconImage.f_addressableKey);
            spriteHandle.Completed += (handle) =>
            {
                // �� ������ gameObject�� �ı��Ǿ��ų� itemIcon�� null���� Ȯ��
                if (this == null || !this.gameObject || itemIcon == null)
                {
                    // ��ü�� �ı��� ���, �ε��� ���ҽ� �����ϰ� ����
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
                    Debug.LogError($"������ ������ �ε� ����: {item.f_iconImage.f_addressableKey}");
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

        // UI ĵ���� �ֻ����� �̵�
        transform.SetParent(UIManager.Instance.fullWindowCanvas.transform);

        // ����ĳ��Ʈ ����
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

        // ����ĳ��Ʈ ����
        itemIcon.raycastTarget = true;

        bool validDrop = TryDropOnUnit(eventData);

        // ��ȿ�� ����� �ƴϸ� ���� ��ġ�� ����
        if (!validDrop)
        {
            ReturnToOriginalPosition();
        }
    }

    // ���ֿ� ������ ��� �õ�
    private bool TryDropOnUnit(PointerEventData eventData)
    {
        // ��ũ�� ��ǥ�� ���� ��ǥ�� ��ȯ
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(eventData.position);
        worldPosition.z = 0;

        // ���� ��ǥ�� Ÿ�� ��ǥ�� ��ȯ
        Vector2 tilePosition = TileMapManager.Instance.GetWorldToTilePosition(worldPosition);

        // Ÿ�� ��ġ�� �ִ� ���� ã��
        TileData tileData = TileMapManager.Instance.GetTileData(tilePosition);

        // Ÿ�Ͽ� ������ �ִ��� Ȯ��
        if (tileData != null && tileData.placedUnit != null && equipmentSystem != null && itemData != null)
        {
            UnitController unit = tileData.placedUnit;

            // �ִ� ���� �� Ȯ��
            int maxSlots = (int)unit.GetStat(StatName.UnitInventoryCount);

            // �� ���� ã�� ����
            for (int i = 0; i < maxSlots; i++)
            {
                if (equipmentSystem.IsSlotAvailable(unit, i))
                {
                    equipmentSystem.EquipItem(unit, itemData, i);
                    Debug.Log($"�������� ���� {unit.name}�� ���� {i}�� �����Ǿ����ϴ�.");
                    return true;
                }
            }
        }

        return false;
    }

    // ���� ��ġ�� ���ư���
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(originalParent);
        transform.position = originalPosition;
    }

    private void OnDestroy()
    {
        // Addressable ����
        if (spriteHandle.IsValid())
        {
            Addressables.Release(spriteHandle);
        }
    }
}