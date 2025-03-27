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
    // 멀티타일 관련 추가 속성
    [HideInInspector]
    public List<TileExtensionObject> extensionObjects = new List<TileExtensionObject>();

    [HideInInspector]
    public List<Vector2> multiTilesOffset = new List<Vector2>(); // 이 유닛이 차지하는 타일 위치들


    // 확장 타일과 통신할 이벤트 정의
    public event System.Action<Vector3> OnPositionChanged;
    public event System.Action<Material> OnMaterialChanged;
    public event System.Action OnUnitDeleted;

    // 확장 타일 관리를 위한 딕셔너리 추가
    private Dictionary<Vector2, TileExtensionObject> extensionTiles = new Dictionary<Vector2, TileExtensionObject>();


    // UnitController에서 오버라이딩할 메서드들
    public override void Initialize()
    {
        base.Initialize();
        // 추가 초기화
    }

    // 드래그 관련 오버라이딩
    public override void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true;

        // 마우스 위치에서 타일 위치 계산
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        // 타일맵 색상 업데이트
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0.1f));

        // 배치 가능 여부 확인
        canPlace = CheckPlacementPossibility(currentTilePos);

        // 위치 변경
        if (currentTilePos != previousTilePosition)
        {
            UpdateDragPosition(currentTilePos);
            previousTilePosition = currentTilePos;
        }

        // 쓰레기통 위에 있는지 확인
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

    // 드래그 위치 업데이트
    private void UpdateDragPosition(Vector2 currentTilePos)
    {
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
        newPosition.z = -0.1f;
        transform.position = newPosition;
        SetPreviewMaterial(canPlace);

        // 위치 변경 이벤트 발생
        OnPositionChanged?.Invoke(newPosition);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // 쓰레기통 숨기기
        GameManager.Instance.HideTrashCan();

        // 타일 색상 복원
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0));

        bool isSuccess = false;

        // 쓰레기통 위에 드롭한 경우 유닛 삭제
        if (isOverTrashCan)
        {
            DeleteUnit();
            isSuccess = true;
        }
        else if (hasDragged && previousTilePosition != originalTilePosition)
        {
            // 이동 처리 (멀티타일은 합성 불가)
            if (canPlace)
            {
                MoveUnit();
                isSuccess = true;
            }
        }

        // 실패한 경우 원래 상태로 복원
        if (!isSuccess)
        {
            ReturnToOriginalPosition();
            CheckAttackAvailability();
        }
    }


    // 확장 타일 추가 메서드
    public void AddExtensionTile(Vector2 offset, TileExtensionObject extensionTile)
    {
        if (!extensionTiles.ContainsKey(offset))
        {
            extensionTiles.Add(offset, extensionTile);
        }
    }

    // 멀티타일 유닛 삭제
    public override void DeleteUnit()
    {
        // 대형 유닛이 점유한 모든 타일 해제
        foreach (var offset in multiTilesOffset)
        {
            Vector2 pos = tilePosition + offset;
            TileMapManager.Instance.ReleaseTile(pos);
        }

        // 모든 확장 타일 객체 제거
        foreach (var extObj in extensionObjects)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }
        extensionObjects.Clear();

        // 유닛 매니저에서 등록 해제
        UnitManager.Instance.UnregisterUnit(this);
        EnemyManager.Instance.UpdateEnemiesPath();

        // 코스트 정산
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

        // 삭제 이벤트 발생
        OnUnitDeleted?.Invoke();

        // 유닛 풀링 시스템으로 반환
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    protected override void SetDeleteMat()
    {
        if (unitSprite != null)
        {
            unitSprite.material = deleteMaterial;
            unitBaseSprite.material = deleteMaterial;

            // 머테리얼 변경 이벤트 발생
            OnMaterialChanged?.Invoke(deleteMaterial);
        }
    }

    // 배치 가능 여부 확인
    protected override bool CheckPlacementPossibility(Vector2 targetPos)
    {
        // 원래 점유하던 모든 타일 일시적으로 비우기
        List<TileData> originalTiles = new List<TileData>();

        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileData tileData = TileMapManager.Instance.GetTileData(originalPos);

            if (tileData != null)
            {
                bool originalAvailable = tileData.isAvailable;
                tileData.isAvailable = true;
                tileData.placedUnit = null;
                TileMapManager.Instance.SetTileData(tileData);
                originalTiles.Add(tileData);
            }
        }

        // 새 위치에 배치 가능한지 확인
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

        // 원래 타일 상태 복원
        foreach (var tile in originalTiles)
        {
            bool isOriginalPosition = false;

            foreach (var offset in multiTilesOffset)
            {
                Vector2 pos = originalTilePosition + offset;
                if (tile.tilePosX == pos.x && tile.tilePosY == pos.y)
                {
                    tile.isAvailable = false;
                    tile.placedUnit = this;
                    isOriginalPosition = true;
                    break;
                }
            }

            if (isOriginalPosition)
            {
                TileMapManager.Instance.SetTileData(tile);
            }
        }

        return canPlace;
    }

    // 유닛 이동
    protected override void MoveUnit()
    {
        // 원래 점유하던 모든 타일에서 유닛 제거
        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileMapManager.Instance.ReleaseTile(originalPos);
        }

        // 새 위치의 모든 타일 점유
        foreach (var offset in multiTilesOffset)
        {
            Vector2 newPos = previousTilePosition + offset;
            TileData targetTile = TileMapManager.Instance.GetTileData(newPos);

            targetTile.isAvailable = false;
            targetTile.placedUnit = this;
            TileMapManager.Instance.SetTileData(targetTile);
        }

        // 유닛 위치 업데이트
        tilePosition = previousTilePosition;
        Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        finalPosition.z = 0;
        transform.position = finalPosition;

        // 확장 타일 객체 위치 업데이트
        UpdateExtensionObjects();

        // 프리뷰 종료
        DestroyPreviewUnit();

        // 적 경로 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();

        // 공격가능한 위치인지 체크
        CheckAttackAvailability();
    }

    // 확장 타일 객체 업데이트
    public void UpdateExtensionObjects()
    {
        if (extensionObjects.Count > 0)
        {
            Vector3 basePos = transform.position;

            // OnPositionChanged 이벤트를 사용하여 모든 확장 타일에 알림
            OnPositionChanged?.Invoke(basePos);
        }
    }

    // 머테리얼 변경 (오버라이드)
    public override void SetPreviewMaterial(bool canPlace)
    {
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // 프리뷰일 때는 Sorting Order를 높게 설정
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base는 한단계 아래로

            // 머테리얼 변경 이벤트 발생
            OnMaterialChanged?.Invoke(targetMaterial);
        }

    }

    // 프리뷰 삭제 (오버라이드)
    public override void DestroyPreviewUnit()
    {
        base.DestroyPreviewUnit();

        OnMaterialChanged?.Invoke(originalMaterial);
    }

    // 합성 관련 메서드 비활성화 (멀티타일은 합성 불가)
    protected override bool CanMergeWithTarget(TileData tileData)
    {
        return false; // 멀티타일은 항상 합성 불가
    }


    //풀링으로 돌아갈때 필요
    private void OnDisable()
    {
        // 모든 이벤트 구독자에게 삭제 알림
        OnUnitDeleted?.Invoke();

        // 이벤트 정리
        OnPositionChanged = null;
        OnMaterialChanged = null;
        OnUnitDeleted = null;
    }

}