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

    private TileData mergeTargetTile = null;
    private bool isShowingMergePreview = false;

    // 확장 타일과 통신할 이벤트 정의
    public event System.Action<Vector3> OnPositionChanged;
    public event System.Action<Vector3, float> OnMovingPositionChanged;
    public event System.Action<Material> OnMaterialChanged;
    public event System.Action OnUnitDeleted;

    // UnitController에서 오버라이딩할 메서드들
    public override void Initialize()
    {
        base.Initialize();
        // 추가 초기화
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData); // 부모 메서드 호출

        // 필요한 경우 추가 초기화
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);
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

        // 현재 타일 정보 가져오기
        TileData tileData = TileMapManager.Instance.GetTileData(currentTilePos);

        // 합성 가능 여부 확인
        bool canMerge = CanMergeWithTarget(tileData);

        // 합성이 가능하면 canPlace를 true로 설정, 아니면 일반 배치 가능성 확인
        if (canMerge)
        {
            canPlace = true;
        }
        else
        {
            // 배치 가능 여부 확인 (합성이 아닌 경우)
            canPlace = CheckPlacementPossibility(currentTilePos);
        }

        // 타일 위치가 변경되었거나 합성 상태가 변경된 경우에만 처리
        bool canUpdatePreview = currentTilePos != previousTilePosition || (canMerge != isShowingMergePreview);

        if (canUpdatePreview)
        {
            // 이전 합성 프리뷰 상태 초기화
            ResetMergePreview();

            // 합성 가능 여부 확인 및 프리뷰 표시
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

    // 합성 프리뷰 표시 메서드 추가
    private void ShowMergePreview(TileData tileData)
    {
        mergeTargetTile = tileData;
        isShowingMergePreview = true;

        // 타겟 유닛의 별 비활성화
        MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;
        if (targetMultiUnit != null)
        {
            foreach (var star in targetMultiUnit.starObjects)
            {
                star.SetActive(false);
            }
        }

        // 내 유닛을 업그레이드된 레벨로 표시
        int newStarLevel = originalStarLevel + 1;
        UpdateStarDisplay(newStarLevel);

        // 중요: 드래그 중인 유닛을 정확히 합성 대상 유닛 위에 배치
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        // 확장 타일 위치도 업데이트
        OnPositionChanged?.Invoke(targetPosition);

        // 프리뷰 머테리얼 설정
        SetPreviewMaterial(canPlace);

        // 시각적 효과 (한 번만 실행)
        //unitSprite.transform.DOKill();
        //unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
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
       
      if (hasDragged && previousTilePosition != originalTilePosition)
        {
            // 합성 처리
            if (isShowingMergePreview && mergeTargetTile != null && CanMergeWithTarget(mergeTargetTile))
            {
                PerformMerge();
                isSuccess = true;
            }
            // 이동 처리
            else if (canPlace)
            {
                MoveUnit();
                isSuccess = true;
            }
        }

        // 실패한 경우 원래 상태로 복원
        if (!isSuccess)
        {
            ResetMergePreview();
            ReturnToOriginalPosition();
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
                // 기존에 점유된 타일이라면 일시적으로 사용 가능하게 만들기
                bool wasOccupied = !tileData.isAvailable;
                tileData.isAvailable = true;
                tileData.placedUnit = null;
                TileMapManager.Instance.SetTileData(tileData);

                // 원래 점유 상태를 기록
                originalTiles.Add(new TileData(originalPos)
                {
                    isAvailable = !wasOccupied,
                    placedUnit = wasOccupied ? this : null
                });
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

    //원래 위치로 돌아가기
    public override void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
        OnMovingPositionChanged?.Invoke(originalPosition, 0.3f);
    }


    // 합성 관련 메서드 비활성화 (멀티타일은 합성 불가)
    protected override bool CanMergeWithTarget(TileData tileData)
    {
        // 기본 검사: 타일 데이터가 있고, 유닛이 있으며, 자기 자신이 아닌지
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // 타겟 유닛이 멀티타일 유닛인지 확인
        MultiTileUnitController targetMultiUnit = targetUnit as MultiTileUnitController;
        if (targetMultiUnit == null)
            return false;

        // 유닛 타입과 레벨 확인
        if (unitType != targetUnit.unitType ||
         originalStarLevel != targetUnit.GetStat(StatName.UnitStarLevel) ||
         targetUnit.GetStat(StatName.UnitStarLevel) >= 5)
            return false;


        // 모든 타일이 겹치는지 확인
        bool allTilesOverlap = CheckAllTilesOverlap(targetMultiUnit);

        return allTilesOverlap;
    }

    // 모든 타일이 겹치는지 확인 메서드
    private bool CheckAllTilesOverlap(MultiTileUnitController targetUnit)
    {
        // 두 유닛의 기준 좌표 (0,0) 위치 가져오기
        Vector2 myBasePos = previousTilePosition;
        Vector2 targetBasePos = targetUnit.tilePosition;

        // 확장 타일 오프셋 리스트 크기가 같은지 확인
        if (multiTilesOffset.Count != targetUnit.multiTilesOffset.Count)
            return false;

        // 모든 타일이 겹치는지 확인
        HashSet<Vector2> myTilePositions = new HashSet<Vector2>();
        HashSet<Vector2> targetTilePositions = new HashSet<Vector2>();

        // 내 유닛이 차지하는 모든 타일 위치 추가
        foreach (var offset in multiTilesOffset)
        {
            myTilePositions.Add(myBasePos + offset);
        }

        // 타겟 유닛이 차지하는 모든 타일 위치 추가
        foreach (var offset in targetUnit.multiTilesOffset)
        {
            targetTilePositions.Add(targetBasePos + offset);
        }

        // 두 집합이 완전히 같은지 확인 (모든 타일이 겹치는지)
        return myTilePositions.SetEquals(targetTilePositions);
    }

    protected override void PerformMerge()
    {
        if (mergeTargetTile == null || mergeTargetTile.placedUnit == null)
        {
            ResetMergePreview();
            return;
        }

        // 타겟 유닛을 MultiTileUnitController로 캐스팅
        MultiTileUnitController targetMultiUnit = mergeTargetTile.placedUnit as MultiTileUnitController;
        if (targetMultiUnit == null)
            return;

        // 원래 점유하던 모든 타일 해제
        foreach (var offset in multiTilesOffset)
        {
            Vector2 originalPos = originalTilePosition + offset;
            TileMapManager.Instance.ReleaseTile(originalPos);
        }

        // 타겟 유닛 업그레이드
        int newStarLevel = (int)GetStat(StatName.UnitStarLevel) + 1;
        targetMultiUnit.UpGradeUnitLevel(newStarLevel);

        // 효과 적용
        targetMultiUnit.ApplyEffect(1.0f);

        // 원본 유닛 제거
        UnitManager.Instance.UnregisterUnit(this);

        // 원본 유닛과 연결된 확장 타일 객체들 제거
        foreach (var extObj in extensionObjects)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }
        extensionObjects.Clear();

        // 유닛 풀링 시스템으로 반환
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    private void ResetMergePreview()
    {
        // 이전 타겟이 있고, 현재 합성 프리뷰를 보여주고 있다면
        if (mergeTargetTile != null && isShowingMergePreview)
        {
            // 타겟 유닛의 별 복원
            MultiTileUnitController targetMultiUnit = mergeTargetTile.placedUnit as MultiTileUnitController;
            if (targetMultiUnit != null)
            {
                foreach (var star in targetMultiUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }

            // 내 유닛을 원래 레벨로 복원 (스탯 변경 없이 별만 표시)
            UpdateStarDisplay(originalStarLevel);

            // 애니메이션 리셋
            //unitSprite.transform.DORewind();

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }
    }

    //풀링으로 돌아갈때 필요
    private void OnDisable()
    {
        // 모든 이벤트 구독자에게 삭제 알림
        OnUnitDeleted?.Invoke();

        // 이벤트 정리
        OnPositionChanged = null;
        OnMovingPositionChanged = null;
        OnMaterialChanged = null;
        OnUnitDeleted = null;
    }

}