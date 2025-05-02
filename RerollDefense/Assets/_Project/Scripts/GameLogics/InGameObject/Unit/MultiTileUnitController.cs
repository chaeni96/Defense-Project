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

    private TileData mergeTargetTile = null;
    private bool isShowingMergePreview = false;

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

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData); // �θ� �޼��� ȣ��

        // �ʿ��� ��� �߰� �ʱ�ȭ
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);
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

        // ���� Ÿ�� ���� ��������
        TileData tileData = TileMapManager.Instance.GetTileData(currentTilePos);

        // �ռ� ���� ���� Ȯ��
        bool canMerge = CanMergeWithTarget(tileData);

        // �ռ��� �����ϸ� canPlace�� true�� ����, �ƴϸ� �Ϲ� ��ġ ���ɼ� Ȯ��
        if (canMerge)
        {
            canPlace = true;
        }
        else
        {
            // ��ġ ���� ���� Ȯ�� (�ռ��� �ƴ� ���)
            canPlace = CheckPlacementPossibility(currentTilePos);
        }

        // Ÿ�� ��ġ�� ����Ǿ��ų� �ռ� ���°� ����� ��쿡�� ó��
        bool canUpdatePreview = currentTilePos != previousTilePosition || (canMerge != isShowingMergePreview);

        if (canUpdatePreview)
        {
            // ���� �ռ� ������ ���� �ʱ�ȭ
            ResetMergePreview();

            // �ռ� ���� ���� Ȯ�� �� ������ ǥ��
            if (canMerge)
            {
                ShowMergePreview(tileData);
            }
            else
            {
                UpdateDragPosition(currentTilePos);
            }

            previousTilePosition = currentTilePos;
        }
    }

    // �ռ� ������ ǥ�� �޼��� �߰�
    private void ShowMergePreview(TileData tileData)
    {
        mergeTargetTile = tileData;
        isShowingMergePreview = true;

        // Ÿ�� ������ �� ��Ȱ��ȭ
        MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;
        if (targetMultiUnit != null)
        {
            foreach (var star in targetMultiUnit.starObjects)
            {
                star.SetActive(false);
            }
        }

        // �� ������ ���׷��̵�� ������ ǥ��
        int newStarLevel = originalStarLevel + 1;
        UpdateStarDisplay(newStarLevel);

        // �߿�: �巡�� ���� ������ ��Ȯ�� �ռ� ��� ���� ���� ��ġ
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        // Ȯ�� Ÿ�� ��ġ�� ������Ʈ
        OnPositionChanged?.Invoke(targetPosition);

        // ������ ���׸��� ����
        SetPreviewMaterial(canPlace);

        // �ð��� ȿ�� (�� ���� ����)
        //unitSprite.transform.DOKill();
        //unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
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
       
      if (hasDragged && previousTilePosition != originalTilePosition)
        {
            // �ռ� ó��
            if (isShowingMergePreview && mergeTargetTile != null && CanMergeWithTarget(mergeTargetTile))
            {
                PerformMerge();
                isSuccess = true;
            }
            // �̵� ó��
            else if (canPlace)
            {
                MoveUnit();
                isSuccess = true;
            }
        }

        // ������ ��� ���� ���·� ����
        if (!isSuccess)
        {
            ResetMergePreview();
            ReturnToOriginalPosition();
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

   
    }

    // Ȯ�� Ÿ�� ��ü ������Ʈ
    public void UpdateExtensionObjects()
    {
        if (extensionObjects.Count > 0)
        {
            Vector3 basePos = transform.position;

            // OnPositionChanged �̺�Ʈ�� ����Ͽ� ��� Ȯ�� Ÿ�Ͽ� �˸�
            OnPositionChanged?.Invoke(basePos);
        }
    }

    //���� ��ġ�� ���ư���
    public override void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
        OnMovingPositionChanged?.Invoke(originalPosition, 0.3f);
    }


    // �ռ� ���� �޼��� ��Ȱ��ȭ (��ƼŸ���� �ռ� �Ұ�)
    protected override bool CanMergeWithTarget(TileData tileData)
    {
        // �⺻ �˻�: Ÿ�� �����Ͱ� �ְ�, ������ ������, �ڱ� �ڽ��� �ƴ���
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // Ÿ�� ������ ��ƼŸ�� �������� Ȯ��
        MultiTileUnitController targetMultiUnit = targetUnit as MultiTileUnitController;
        if (targetMultiUnit == null)
            return false;

        // ���� Ÿ�԰� ���� Ȯ��
        if (unitType != targetUnit.unitType ||
         originalStarLevel != targetUnit.GetStat(StatName.UnitStarLevel) ||
         targetUnit.GetStat(StatName.UnitStarLevel) >= 5)
            return false;


        // ��� Ÿ���� ��ġ���� Ȯ��
        bool allTilesOverlap = CheckAllTilesOverlap(targetMultiUnit);

        return allTilesOverlap;
    }

    // ��� Ÿ���� ��ġ���� Ȯ�� �޼���
    private bool CheckAllTilesOverlap(MultiTileUnitController targetUnit)
    {
        // �� ������ ���� ��ǥ (0,0) ��ġ ��������
        Vector2 myBasePos = previousTilePosition;
        Vector2 targetBasePos = targetUnit.tilePosition;

        // Ȯ�� Ÿ�� ������ ����Ʈ ũ�Ⱑ ������ Ȯ��
        if (multiTilesOffset.Count != targetUnit.multiTilesOffset.Count)
            return false;

        // ��� Ÿ���� ��ġ���� Ȯ��
        HashSet<Vector2> myTilePositions = new HashSet<Vector2>();
        HashSet<Vector2> targetTilePositions = new HashSet<Vector2>();

        // �� ������ �����ϴ� ��� Ÿ�� ��ġ �߰�
        foreach (var offset in multiTilesOffset)
        {
            myTilePositions.Add(myBasePos + offset);
        }

        // Ÿ�� ������ �����ϴ� ��� Ÿ�� ��ġ �߰�
        foreach (var offset in targetUnit.multiTilesOffset)
        {
            targetTilePositions.Add(targetBasePos + offset);
        }

        // �� ������ ������ ������ Ȯ�� (��� Ÿ���� ��ġ����)
        return myTilePositions.SetEquals(targetTilePositions);
    }

    protected override void PerformMerge()
    {
        if (mergeTargetTile == null || mergeTargetTile.placedUnit == null)
        {
            ResetMergePreview();
            return;
        }

        // Ÿ�� ������ MultiTileUnitController�� ĳ����
        MultiTileUnitController targetMultiUnit = mergeTargetTile.placedUnit as MultiTileUnitController;
        if (targetMultiUnit == null)
            return;

        // ���� �����ϴ� ��� Ÿ�� ����
        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileMapManager.Instance.ReleaseTile(originalPos);
        }

        // Ÿ�� ���� ���׷��̵�
        int newStarLevel = (int)GetStat(StatName.UnitStarLevel) + 1;
        targetMultiUnit.UpGradeUnitLevel(newStarLevel);

        // ȿ�� ����
        targetMultiUnit.ApplyEffect(1.0f);

        // ���� ���� ����
        UnitManager.Instance.UnregisterUnit(this);

        // ���� ���ְ� ����� Ȯ�� Ÿ�� ��ü�� ����
        foreach (var extObj in extensionObjects)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }
        extensionObjects.Clear();

        // ���� Ǯ�� �ý������� ��ȯ
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    private void ResetMergePreview()
    {
        // ���� Ÿ���� �ְ�, ���� �ռ� �����並 �����ְ� �ִٸ�
        if (mergeTargetTile != null && isShowingMergePreview)
        {
            // Ÿ�� ������ �� ����
            MultiTileUnitController targetMultiUnit = mergeTargetTile.placedUnit as MultiTileUnitController;
            if (targetMultiUnit != null)
            {
                foreach (var star in targetMultiUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }

            // �� ������ ���� ������ ���� (���� ���� ���� ���� ǥ��)
            UpdateStarDisplay(originalStarLevel);

            // �ִϸ��̼� ����
            //unitSprite.transform.DORewind();

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }
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