using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.IO;


public class UnitController : BasicObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector]
    public float attackTimer = 0f;  // 타이머 추가

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    [HideInInspector]
    public bool canAttack = true; // 공격 가능 여부

    [HideInInspector]
    public D_UnitData unitData;

    public List<GameObject> starObjects = new List<GameObject>();  // 생성된 별들을 관리하기 위한 리스트

    public UnitType unitType;

    public GameObject unitStarObject;


    //inspector에 할당

    public SpriteRenderer unitSprite;

    //TODO : Multitile용 유닛 만들면 다시 private
    [SerializeField] public SpriteRenderer unitBaseSprite;
    [SerializeField] public Material enabledMaterial;   // 배치 가능할 때 사용
    [SerializeField] public Material disabledMaterial; // 배치 불가능할 때 사용
    [SerializeField] public Material deleteMaterial;   // 배치 삭제할 때 사용
    [SerializeField] public Material originalMaterial; //기본 머테리얼
    [SerializeField] public LayerMask unitLayer;  // Inspector에서 Unit 레이어 체크

    private int unitSortingOrder;
    private int baseSortingOrder;
    private bool isActive = true;

    // 드래그 앤 드롭을 위한 변수 추가
    public bool isDragging = false;
    protected Vector3 originalPosition;
    protected Vector2 originalTilePosition;
    protected Vector2 previousTilePosition; // 이전 타일 위치 추적용
    protected bool hasDragged = false;
    protected bool canPlace = false;
    // 합성 관련 변수
    private TileData mergeTargetTile = null;
    protected int originalStarLevel = 0;
    private bool isShowingMergePreview = false;

    //합성 불가할대 위치 변환 
    private UnitController swapTargetUnit = null;
    private bool isSwapping = false;

    protected bool isOverTrashCan = false;

    [HideInInspector]
    public bool isMultiUnit = false; // 여러 타일을 차지하는 멀티 유닛인지


    public override void Initialize()
    {
        base.Initialize();
        gameObject.layer = LayerMask.NameToLayer("Player");  // 초기화할 때 레이어 설정

        unitSortingOrder = 0;
        baseSortingOrder = -1;
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = baseSortingOrder; // 베이스는 항상 한단계 뒤에

    }

    public void UpdateStarDisplay(int? starLevel = null)
    {
        if (unitStarObject == null) return;

        int currentStarLevel = Mathf.RoundToInt(GetStat(StatName.UnitStarLevel));

        if(starLevel != null)
        {
            currentStarLevel = starLevel.Value;
        }

        foreach (var star in starObjects)
        {
            Destroy(star);
        }
        starObjects.Clear();

        float spacing = 0.4f;
        float totalWidth = (currentStarLevel - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < currentStarLevel; i++)
        {
            GameObject star = Instantiate(unitStarObject, transform);
            star.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
            starObjects.Add(star);
        }
    }

    //유닛 정보 초기화
    public void InitializeUnitInfo(D_UnitData unit, D_TileCardData tileCard = null)
    {
        if (unit == null) return;

        isEnemy = false;

        unitData = unit;
        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

        // 기존 스탯들 초기화
        baseStats.Clear();
        currentStats.Clear();

        foreach (var subject in unitData.f_StatSubject)
        {
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);

            foreach (var stat in subjectStats)
            {
                if (!baseStats.ContainsKey(stat.statName))
                {
                    baseStats[stat.statName] = new StatStorage
                    {
                        statName = stat.statName,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    baseStats[stat.statName].value += stat.value;
                    baseStats[stat.statName].multiply *= stat.multiply;
                }
            }

            AddSubject(subject);
        }


        // 현재 스탯을 기본 스탯으로 초기화
        foreach (var baseStat in baseStats)
        {
            int statValue = baseStat.Value.value;

            // StarLevel에 따른 스탯 보정
            if (baseStat.Key != StatName.UnitStarLevel && baseStats.ContainsKey(StatName.UnitStarLevel))
            {
                statValue *= baseStats[StatName.UnitStarLevel].value;
            }

            currentStats[baseStat.Key] = new StatStorage
            {
                statName = baseStat.Value.statName,
                value = statValue,
                multiply = baseStat.Value.multiply
            };
        }

        UpdateStarDisplay();

        if (tileCard != null)
        {

            isMultiUnit = tileCard.f_isMultiTileUinit;
        }


    }


    //스탯 변경시 할 행동들
    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        base.OnStatChanged(subject, statChange);
        
        ApplyEffect();

        //attackSpeed 바뀌었을때는 attackTimer 0부터 다시 시작
        if (statChange.statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }

    public void MoveScale()
    {
        // DOPunchScale 상세 파라미터
        // punch:크기 변화
        // duration: 전체 재생 시간
        // vibrato: 진동 횟수
        // elasticity: 탄성 (0~1)

        unitSprite.transform.DOPunchScale(punch: new Vector3(0.4f, 0.4f, 0f), duration: 0.1f, vibrato: 4, elasticity: 0.8f);

    }

    // 공격 가능 여부를 확인하는 메서드
    public void CheckAttackAvailability()
    {
        // y가 9보다 크면 공격 불가
        canAttack = tilePosition.y < 10;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }

    // 드래그 시작 
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // 드래그 시작 시 초기화
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        canAttack = false;

        // 현재 별 레벨 저장
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);

        // 합성 관련 변수 초기화
        mergeTargetTile = null;
        isShowingMergePreview = false;

        // 드래그 시작 설정
        Vector3 position = transform.position;
        position.z = -1;
        transform.position = position;

        unitSprite.sortingOrder = 100;
        unitBaseSprite.sortingOrder = 99;

        canPlace = false;

        // 쓰레기통 표시
        GameManager.Instance.ShowTrashCan();
    }

    // 드래그 중 호출
    public virtual void OnDrag(PointerEventData eventData)
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


        // 타일 위치가 변경되었거나 합성 상태가 변경된 경우에만 처리
        bool canUpdatePreview = currentTilePos != previousTilePosition ||
                           (tileData != null && CanMergeWithTarget(tileData) != isShowingMergePreview) ||
                           (tileData != null && CanSwapWithTarget(tileData) != isSwapping);

        if (canUpdatePreview)
        {
            // 이전 합성 프리뷰 상태 초기화
            ResetMergePreview();

            // 합성 가능 여부 확인 및 프리뷰 표시
            if (CanMergeWithTarget(tileData))
            {
                ShowMergePreview(tileData);
            }
            // 합성이 불가능하지만 교환은 가능한 경우
            else if (CanSwapWithTarget(tileData))
            {
                ShowSwapPreview(tileData);
            }
            else
            {
                UpdateDragPosition(currentTilePos);
            }

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

    private void UpdateDragPosition(Vector2 currentTilePos)
    {
        // 일반 이동 - 정확히 마우스 위치에 배치
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
        newPosition.z = -0.1f;
        transform.position = newPosition;
        SetPreviewMaterial(canPlace);

    }

    // 드래그 종료
    public virtual void OnPointerUp(PointerEventData eventData)
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
            // 합성 처리
            if (isShowingMergePreview && mergeTargetTile != null && CanMergeWithTarget(mergeTargetTile))
            {
                PerformMerge();
                isSuccess = true;
            }
            // 교환 처리
            else if (isSwapping && swapTargetUnit != null && CanSwapWithTarget(TileMapManager.Instance.GetTileData(previousTilePosition)))
            {
                PerformSwap();
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
            ResetPreviewStates();
            DestroyPreviewUnit();
            ReturnToOriginalPosition();

            CheckAttackAvailability();
        }
    }

    protected virtual void PerformSwap()
    {
        if (swapTargetUnit == null) return;

        TileData targetTileData = TileMapManager.Instance.GetTileData(previousTilePosition);
        if (targetTileData == null) return;

        // 원래 위치와 타겟 위치 저장
        Vector2 myOriginalPos = originalTilePosition;
        Vector2 targetOriginalPos = swapTargetUnit.tilePosition;

        Vector3 myWorldPos = TileMapManager.Instance.GetTileToWorldPosition(myOriginalPos);
        Vector3 targetWorldPos = TileMapManager.Instance.GetTileToWorldPosition(targetOriginalPos);

        // 타일 데이터 업데이트
        TileMapManager.Instance.ReleaseTile(myOriginalPos);
        TileMapManager.Instance.ReleaseTile(targetOriginalPos);

        // 타겟 유닛의 위치 업데이트
        swapTargetUnit.tilePosition = myOriginalPos;
        List<Vector2> targetTileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> targetUnits = new Dictionary<int, UnitController>() { { 0, swapTargetUnit } };
        TileMapManager.Instance.OccupyTiles(myOriginalPos, targetTileOffsets, targetUnits);

        // 현재 유닛의 위치 업데이트
        tilePosition = targetOriginalPos;
        List<Vector2> myTileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> myUnits = new Dictionary<int, UnitController>() { { 0, this } };
        TileMapManager.Instance.OccupyTiles(targetOriginalPos, myTileOffsets, myUnits);

        // 애니메이션 적용
        transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutBack);
        swapTargetUnit.transform.DOMove(myWorldPos, 0.3f).SetEase(Ease.OutBack);

        // 정리
        DestroyPreviewUnit();
        swapTargetUnit.DestroyPreviewUnit();

        // 공격 가능 상태 업데이트
        CheckAttackAvailability();
        swapTargetUnit.CheckAttackAvailability();


        ResetPreviewStates();

        // 적 경로 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();
    }

    private void ResetPreviewStates()
    {
        // 합성 프리뷰 초기화
        if (mergeTargetTile != null && isShowingMergePreview)
        {
            // 타겟 유닛의 별 복원
            if (mergeTargetTile.placedUnit != null)
            {
                foreach (var star in mergeTargetTile.placedUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }

            // 내 유닛을 원래 레벨로 복원
            if (GetStat(StatName.UnitStarLevel) != originalStarLevel)
            {
                UpGradeUnitLevel(originalStarLevel);
                unitSprite.transform.DORewind(); // 애니메이션 리셋
            }

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }

        // 위치 교환 프리뷰 초기화
        if (swapTargetUnit != null && isSwapping)
        {
            swapTargetUnit.unitSprite.DORewind();
            swapTargetUnit.unitBaseSprite.DORewind();
            isSwapping = false;
            swapTargetUnit = null;
        }
    }


    public virtual void DeleteUnit()
    {
     
        // 기존 단일 타일 로직
        TileMapManager.Instance.ReleaseTile(originalTilePosition);
     

        // 유닛 매니저에서 등록 해제
        UnitManager.Instance.UnregisterUnit(this);
        EnemyManager.Instance.UpdateEnemiesPath();

        //코스트 정산
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
    }

// 배치 가능한지 확인
 protected virtual bool CheckPlacementPossibility(Vector2 targetPos)
    {
        {
            // 기존 단일 타일 로직
            TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
            bool originalAvailable = false;

            if (originalTileData != null)
            {
                originalAvailable = originalTileData.isAvailable;
                originalTileData.isAvailable = true;
                originalTileData.placedUnit = null;
                TileMapManager.Instance.SetTileData(originalTileData);
            }

            // 배치 가능성 확인
            List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
            Dictionary<int, UnitController> units = new Dictionary<int, UnitController>() { { 0, this } };
            bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, tileOffsets, units);

            // 원래 타일 상태 복원
            if (originalTileData != null)
            {
                originalTileData.isAvailable = originalAvailable;
                originalTileData.placedUnit = this;
                TileMapManager.Instance.SetTileData(originalTileData);
            }

            return canPlace;
        }
    }




    // 합성 프리뷰 초기화
    private void ResetMergePreview()
    {
        // 이전 타겟이 있고, 현재 합성 프리뷰를 보여주고 있다면
        if (mergeTargetTile != null && isShowingMergePreview)
        {
            // 타겟 유닛의 별 복원
            if (mergeTargetTile.placedUnit != null)
            {
                foreach (var star in mergeTargetTile.placedUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }

            // 내 유닛을 원래 레벨로 복원
            if (GetStat(StatName.UnitStarLevel) != originalStarLevel)
            {
                UpGradeUnitLevel(originalStarLevel);
                unitSprite.transform.DORewind(); // 애니메이션 리셋
            }

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }

        // 위치 교환 프리뷰 초기화
        if (swapTargetUnit != null && isSwapping)
        {
            swapTargetUnit.unitSprite.DORewind();
            swapTargetUnit.unitBaseSprite.DORewind();
            isSwapping = false;
            swapTargetUnit = null;
        }
    }

    // 합성 가능 여부 확인
    protected virtual bool CanMergeWithTarget(TileData tileData)
    {
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // 멀티 유닛은 합성 불가능
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;

        return unitType == targetUnit.unitType &&
               originalStarLevel == targetUnit.GetStat(StatName.UnitStarLevel) &&
               targetUnit.GetStat(StatName.UnitStarLevel) < 5;
    }

    // 합성 프리뷰 표시
    private void ShowMergePreview(TileData tileData)
    {

        mergeTargetTile = tileData;
        isShowingMergePreview = true;

        // 타겟 유닛의 별 비활성화
        foreach (var star in tileData.placedUnit.starObjects)
        {
            star.SetActive(false);
        }

        // 내 유닛을 업그레이드된 레벨로 표시
        int newStarLevel = originalStarLevel + 1;
        UpGradeUnitLevel(newStarLevel);

        // 중요: 드래그 중인 유닛을 정확히 합성 대상 유닛 위에 배치
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        // 프리뷰 머테리얼 설정
        SetPreviewMaterial(canPlace);

        // 시각적 효과 (한 번만 실행)
        unitSprite.transform.DOKill();
        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
    }

    // 위치 교환 가능 여부 확인
    public bool CanSwapWithTarget(TileData tileData)
    {
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // 멀티 유닛은 교환 불가능
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;

        // 합성이 가능한 경우는 교환 대신 합성 처리
        if (CanMergeWithTarget(tileData))
            return false;

        // 위의 조건을 모두 통과했으면 교환 가능
        return true;
    }

    private void ShowSwapPreview(TileData tileData)
    {
        if (tileData?.placedUnit == null) return;

        TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
        if (originalTileData == null) return;

        swapTargetUnit = tileData.placedUnit;
        isSwapping = true;

        // 교환 대상 유닛에 시각적 효과 적용
        swapTargetUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.5f, 0.3f, 3, 0.7f);
        swapTargetUnit.unitBaseSprite.transform.DOPunchScale(Vector3.one * 0.3f, 0.3f, 3, 0.7f);

        // 현재 드래그 중인 유닛의 위치를 교환 대상 유닛 위치로 설정
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        //합성 가능한지 아닌지로 적용
        SetPreviewMaterial(canPlace);

    }

    // 유닛 이동
    protected virtual void MoveUnit()
    {
        // 원래 상태로 복원
        ResetMergePreview();
       
        // 기존 단일 타일 로직
        // 원본 타일에서 유닛 제거
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 새 타일 점유
        List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> units = new Dictionary<int, UnitController>() { { 0, this } };
        TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, units);
       

        // 유닛 위치 업데이트
        tilePosition = previousTilePosition;
        Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        finalPosition.z = 0;
        transform.position = finalPosition;

        // 프리뷰 종료
        DestroyPreviewUnit();

        // 적 경로 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();

        //공격가능한 위치인지 체크
        CheckAttackAvailability();
    }

    // 합성 수행
    protected virtual void PerformMerge()
    {
        if (mergeTargetTile == null || mergeTargetTile.placedUnit == null)
        {
            ResetMergePreview();
            return;
        }

        // 원본 타일에서 유닛 제거
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 타겟 유닛 업그레이드
        UnitController targetUnit = mergeTargetTile.placedUnit;
        int newStarLevel = originalStarLevel + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // 효과 적용
        targetUnit.ApplyEffect(1.0f);

        // 원본 유닛 제거
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);

        // 적 경로 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();
    }

    // 원래 위치로 돌아가기
    protected virtual void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
    }


    public void UpGradeUnitLevel(int value)
    {

        if (!currentStats.ContainsKey(StatName.UnitStarLevel)) return;

        // 새로운 StarLevel 설정
        currentStats[StatName.UnitStarLevel].value = value;

        // StarLevel이 변경되었으므로 다른 모든 스탯도 새로운 StarLevel에 맞춰 조정
        foreach (var stat in currentStats)
        {
            if (stat.Key == StatName.UnitStarLevel) continue;

            float upgradePercent = GetStatUpgradePercent(stat.Key);
            float baseValue = baseStats[stat.Key].value;

            if (upgradePercent == 100)
            {
                // 100은 level만큼 곱하는 특수 케이스 (Atk, ProjectileCount 등)
                stat.Value.value = baseStats[stat.Key].value * value;
            }
            else if (upgradePercent > 0)
            {
                // 일반적인 퍼센트 증가
                float percentIncrease = (value == 3) ? upgradePercent * 2 : upgradePercent;
                float multiplier = 1 + (percentIncrease / 100f);
                stat.Value.value = Mathf.RoundToInt(baseStats[stat.Key].value * multiplier);
            }
        }
   

        UpdateStarDisplay();
    }

    private float GetStatUpgradePercent(StatName statName)
    {
        switch (statName)
        {
            case StatName.ATK:
            case StatName.ProjectileCount:
                return 100;  // level만큼 곱하기

            case StatName.AttackRange:
            case StatName.ProjectileSpeed:
                return 10;   // 10% 증가, level3에서는 20%

            // 다른 스탯들
            default:
                return 0;    // 증가하지 않음
        }
    }



    public virtual void SetPreviewMaterial(bool canPlace)
    {
        // 배치 가능할 때와 불가능할 때의 머테리얼 설정
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // 프리뷰일 때는 Sorting Order를 높게 설정
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base는 한단계 아래로
        }
    }

    protected virtual void SetDeleteMat()
    {
        if (unitSprite != null)
        {
            unitSprite.material = deleteMaterial;
            unitBaseSprite.material = deleteMaterial;
        }
    }

    // 프리뷰 종료 시 원본 머테리얼로 복원
    public virtual void DestroyPreviewUnit()
    {
        // 실제 유닛으로 전환 시 원래 sorting order로 복구
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = unitSprite.sortingOrder - 1;

        unitSprite.material = originalMaterial;
        unitBaseSprite.material = originalMaterial;
    }


    public void ApplyEffect(float duration = 0.5f)
    {
        // 원래 색상 저장
        UnityEngine.Color originalColor = unitSprite.color;
        UnityEngine.Color effectColor = UnityEngine.Color.yellow; // 고정된 노란색

        // DOTween으로 색상 변경
        unitSprite.DOColor(effectColor, duration * 0.5f)
            .OnComplete(() =>
            {
                unitSprite.DOColor(originalColor, duration * 0.5f);
            });

        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

    }

}
