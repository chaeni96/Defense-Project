using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.IO;

public class MultiTileUnitController : UnitController
{
    // ��ƼŸ�� ���� �߰� �Ӽ�
    [HideInInspector]
    public List<TileExtensionObject> extensionObjects = new List<TileExtensionObject>();

    [HideInInspector]
    public List<Vector2> multiTilesOffset = new List<Vector2>(); // �� ������ �����ϴ� Ÿ�� ��ġ��


    // Ȯ�� Ÿ�ϰ� ����� �̺�Ʈ ����
    public event System.Action<Vector3> OnPositionChanged;
    public event System.Action<Vector3, float> OnMovingPositionChanged;
    public event System.Action<Material> OnMaterialChanged;
    public event System.Action OnUnitDeleted;

    // UnitController���� �������̵��� �޼����
    public override void Initialize()
    {
        base.Initialize();
        // �߰� �ʱ�ȭ
    }

    // �巡�� ���� �������̵�
    public override void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true;

        // ���콺 ��ġ���� Ÿ�� ��ġ ���
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        // Ÿ�ϸ� ���� ������Ʈ
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0.1f));

        // ��ġ ���� ���� Ȯ��
        canPlace = CheckPlacementPossibility(currentTilePos);

        // ��ġ ����
        if (currentTilePos != previousTilePosition)
        {
            UpdateDragPosition(currentTilePos);
            previousTilePosition = currentTilePos;
        }

        // �������� ���� �ִ��� Ȯ��
        if (GameManager.Instance.IsOverTrashCan(worldPos))
        {
            isOverTrashCan = true;
            SetDeleteMat();
        }
        else
        {
            isOverTrashCan = false;
        }
    }

    // �巡�� ��ġ ������Ʈ
    private void UpdateDragPosition(Vector2 currentTilePos)
    {
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
        newPosition.z = -0.1f;
        transform.position = newPosition;
        SetPreviewMaterial(canPlace);

        // ��ġ ���� �̺�Ʈ �߻�
        OnPositionChanged?.Invoke(newPosition);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // �������� �����
        GameManager.Instance.HideTrashCan();

        // Ÿ�� ���� ����
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0));

        bool isSuccess = false;

        // �������� ���� ����� ��� ���� ����
        if (isOverTrashCan)
        {
            DeleteUnit();
            isSuccess = true;
        }
        else if (hasDragged && previousTilePosition != originalTilePosition)
        {
            // �̵� ó�� (��ƼŸ���� �ռ� �Ұ�)
            if (canPlace)
            {
                MoveUnit();
                isSuccess = true;
            }
        }

        // ������ ��� ���� ���·� ����
        if (!isSuccess)
        {
            DestroyPreviewUnit();
            ReturnToOriginalPosition();
            CheckAttackAvailability();
        }
    }

    // ��ƼŸ�� ���� ����
    public override void DeleteUnit()
    {
        // ���� ������ ������ ��� Ÿ�� ����
        foreach (var offset in multiTilesOffset)
        {
            Vector2 pos = tilePosition + offset;
            TileMapManager.Instance.ReleaseTile(pos);
        }

        // ��� Ȯ�� Ÿ�� ��ü ����
        foreach (var extObj in extensionObjects)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }
        extensionObjects.Clear();

        // ���� �Ŵ������� ��� ����
        UnitManager.Instance.UnregisterUnit(this);
        EnemyManager.Instance.UpdateEnemiesPath();

        // �ڽ�Ʈ ����
        int refundCost = 0;

        if (unitType == UnitType.Base)
        {
            refundCost = -1;
        }
        else
        {
            int starLevel = (int)GetStat(StatName.UnitStarLevel);
            refundCost = (starLevel == 1) ? 1 : starLevel - 1;
        }

        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = refundCost,
            multiply = 1f
        });

        // ���� �̺�Ʈ �߻�
        OnUnitDeleted?.Invoke();

        // ���� Ǯ�� �ý������� ��ȯ
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    protected override void SetDeleteMat()
    {
        if (unitSprite != null)
        {
            unitSprite.material = deleteMaterial;
            unitBaseSprite.material = deleteMaterial;

            // ���׸��� ���� �̺�Ʈ �߻�
            OnMaterialChanged?.Invoke(deleteMaterial);
        }
    }

    // ��ġ ���� ���� Ȯ��
    protected override bool CheckPlacementPossibility(Vector2 targetPos)
    {
        // ���� �����ϴ� ��� Ÿ�� �Ͻ������� ����
        List<TileData> originalTiles = new List<TileData>();

        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileData tileData = TileMapManager.Instance.GetTileData(originalPos);

            if (tileData != null)
            {
                // ������ ������ Ÿ���̶�� �Ͻ������� ��� �����ϰ� �����
                bool wasOccupied = !tileData.isAvailable;
                tileData.isAvailable = true;
                tileData.placedUnit = null;
                TileMapManager.Instance.SetTileData(tileData);

                // ���� ���� ���¸� ���
                originalTiles.Add(new TileData(originalPos)
                {
                    isAvailable = !wasOccupied,
                    placedUnit = wasOccupied ? this : null
                });
            }
        }

        // �� ��ġ�� ��ġ �������� Ȯ��
        bool canPlace = true;

        foreach (var offset in multiTilesOffset)
        {
            Vector2 newPos = targetPos + offset;
            TileData targetTile = TileMapManager.Instance.GetTileData(newPos);

            if (targetTile == null || !targetTile.isAvailable)
            {
                canPlace = false;
                break;
            }
        }

        // ���� Ÿ�� ���� ����
        foreach (var tile in originalTiles)
        {
            TileData existingTile = TileMapManager.Instance.GetTileData(new Vector2(tile.tilePosX, tile.tilePosY));
            if (existingTile != null)
            {
                existingTile.isAvailable = tile.isAvailable;
                existingTile.placedUnit = tile.placedUnit;
                TileMapManager.Instance.SetTileData(existingTile);
            }
        }

        return canPlace;
    }

    // ���� �̵�
    protected override void MoveUnit()
    {
        // ���� �����ϴ� ��� Ÿ�Ͽ��� ���� ����
        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileMapManager.Instance.ReleaseTile(originalPos);
        }

        // �� ��ġ�� ��� Ÿ�� ����
        foreach (var offset in multiTilesOffset)
        {
            Vector2 newPos = previousTilePosition + offset;
            TileData targetTile = TileMapManager.Instance.GetTileData(newPos);

            targetTile.isAvailable = false;
            targetTile.placedUnit = this;
            TileMapManager.Instance.SetTileData(targetTile);
        }

        // ���� ��ġ ������Ʈ
        tilePosition = previousTilePosition;
        Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        finalPosition.z = 0;
        transform.position = finalPosition;

        // Ȯ�� Ÿ�� ��ü ��ġ ������Ʈ
        UpdateExtensionObjects();

        // ������ ����
        DestroyPreviewUnit();

        // �� ��� ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();

        // ���ݰ����� ��ġ���� üũ
        CheckAttackAvailability();
    }

    // Ȯ�� Ÿ�� ��ü ������Ʈ
    public void UpdateExtensionObjects()
    {
        if (extensionObjects.Count > 0)
        {
            Vector3 basePos = transform.position;

            Debug.Log($"[MultiTileUnitController] Base Position: {basePos}");

            // Ȯ�� ��ü���� �� ���� �α�
            foreach (var extObj in extensionObjects)
            {
                if (extObj != null)
                {
                    Vector2 offset = extObj.offsetFromParent;
                    Debug.Log($"Extension Object Offset: {offset}");

                    // �ش� ��ġ�� Ÿ�� ��ǥ Ȯ��
                    Vector2 baseTilePos = TileMapManager.Instance.GetWorldToTilePosition(basePos);
                    Vector2 extensionTilePos = baseTilePos + offset;

                    Debug.Log($"Base Tile Position: {baseTilePos}");
                    Debug.Log($"Extension Tile Position: {extensionTilePos}");
                }
            }


            // OnPositionChanged �̺�Ʈ�� ����Ͽ� ��� Ȯ�� Ÿ�Ͽ� �˸�
            OnPositionChanged?.Invoke(basePos);
        }
    }

    // ���׸��� ���� (�������̵�)
    public override void SetPreviewMaterial(bool canPlace)
    {
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // �������� ���� Sorting Order�� ���� ����
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base�� �Ѵܰ� �Ʒ���

            // ���׸��� ���� �̺�Ʈ �߻�
            OnMaterialChanged?.Invoke(targetMaterial);
        }

    }

    //���� ��ġ�� ���ư���
    protected override void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
        OnMovingPositionChanged?.Invoke(originalPosition, 0.3f);
    }

    // ������ ���� (�������̵�)
    public override void DestroyPreviewUnit()
    {
        base.DestroyPreviewUnit();

        OnMaterialChanged?.Invoke(originalMaterial);
    }

    // �ռ� ���� �޼��� ��Ȱ��ȭ (��ƼŸ���� �ռ� �Ұ�)
    protected override bool CanMergeWithTarget(TileData tileData)
    {
        return false; // ��ƼŸ���� �׻� �ռ� �Ұ�
    }


    //Ǯ������ ���ư��� �ʿ�
    private void OnDisable()
    {
        // ��� �̺�Ʈ �����ڿ��� ���� �˸�
        OnUnitDeleted?.Invoke();

        // �̺�Ʈ ����
        OnPositionChanged = null;
        OnMovingPositionChanged = null;
        OnMaterialChanged = null;
        OnUnitDeleted = null;
    }

}