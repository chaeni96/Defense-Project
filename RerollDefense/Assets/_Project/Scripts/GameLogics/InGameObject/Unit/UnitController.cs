using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;


public class UnitController : BasicObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector]
    public float attackTimer = 0f;  // 타이머 추가

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    public List<GameObject> starObjects = new List<GameObject>();  // 생성된 별들을 관리하기 위한 리스트

    public UnitType unitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;


    //inspector에 할당
    [SerializeField] private Material enabledMaterial;   // 배치 가능할 때 사용
    [SerializeField] private Material disabledMaterial; // 배치 불가능할 때 사용
    [SerializeField] private Material originalMaterial; //기본 머테리얼
    [SerializeField] private LayerMask unitLayer;  // Inspector에서 Unit 레이어 체크

    private int unitSortingOrder;
    private int baseSortingOrder;
    private bool isActive = true;

    // 드래그 앤 드롭을 위한 변수 추가
    public bool isDragging = false;
    private Vector3 originalPosition;
    private Vector2 originalTilePosition;
    private Vector2 previousTilePosition; // 이전 타일 위치 추적용
    private bool hasDragged = false;
    private bool canPlace = false;

    public D_UnitData unitData;
    private UnitController mergedPreviewUnit = null; // 합성 미리보기 유닛
    private TileData targetTileData = null; // 합성 대상 타일 데이터
    private Dictionary<Vector2, TileData> mergeTargets = new Dictionary<Vector2, TileData>(); // 합성 타겟 추적

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
    public void InitializeUnitInfo(D_UnitData unit)
    {
        if (unit == null) return;

        isEnemy = false;

        unitData = unit;
        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

        // 기존 스탯들 초기화
        baseStats.Clear();
        currentStats.Clear();


        //TODO : enemy도 사용할수있으므로 basicObject로 이동
        // Subject 등록
        // 모든 StatSubject에 대해 스탯 가져오기 및 합산
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

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }

    // 드래그 시작 시 호출
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // 드래그 시작 시 초기화
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        mergeTargets.Clear(); // 합성 타겟 초기화

        // 드래깅 시 유닛을 약간 위로 올려 드래그 중임을 시각적으로 표시
        Vector3 position = transform.position;
        position.z = -1;
        transform.position = position;

        // 드래그 중 Sorting Order 변경하여 다른 유닛 위에 표시되도록
        unitSprite.sortingOrder = 100;
        unitBaseSprite.sortingOrder = 99;

        // 배치 가능 여부 초기화
        canPlace = false;
    }

    // 드래그 중 호출
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true; // 드래그 발생 표시

        // 마우스 위치에서 타일 위치 계산
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (currentTilePos != previousTilePosition)
        {

            // 타일맵 색상 업데이트
            TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0.1f));

            // 합성 미리보기 유닛이 있다면 제거
            ClearMergedPreview();

            // 배치 가능 여부 확인
            List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
            Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>()
            {
                { 0, this }
            };

            // 배치 가능성 확인 (원래 위치 고려)
            canPlace = CheckPlacementPossibility(currentTilePos, tileOffsets, currentPreviews);

            // 타겟 타일 데이터 획득
            targetTileData = TileMapManager.Instance.GetTileData(currentTilePos);

            // 합성 가능 여부 확인 및 처리
            bool isMergePreview = CheckAndCreateMergePreview(currentTilePos);

            if (!isMergePreview)
            {
                // 일반 이동 - 타일 위치에 맞게 유닛 이동
                Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
                newPosition.z = -0.1f; // 약간 앞에 표시
                transform.position = newPosition;

                // 머테리얼 업데이트
                SetPreviewMaterial(canPlace);

                // 유닛을 보이게 설정
                unitSprite.enabled = true;
                unitBaseSprite.enabled = true;
            }

            // 이전 위치 업데이트
            previousTilePosition = currentTilePos;
        }
    }

    // 합성 타일의 별 상태 복원
    private void RestoreMergeTileStars()
    {
        foreach (var entry in mergeTargets)
        {
            if (entry.Value?.placedUnit != null)
            {
                foreach (var star in entry.Value.placedUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }
        }
        mergeTargets.Clear();
    }

    // 드래그 종료 시 호출
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // 타일 색상 복원
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0));

        // 위치 및 머테리얼 복원
        DestroyPreviewUnit();

        // 드래그 후 배치 처리
        if (hasDragged)
        {
            // 원래 위치와 다른 곳에 배치하려는 경우만 처리
            if (previousTilePosition != originalTilePosition)
            {
                // 합성 미리보기가 있는 경우
                if (mergedPreviewUnit != null)
                {
                    // 합성 처리
                    PerformMerge();
                    return;
                }

                if (canPlace)
                {
                    // 일반 이동 처리
                    // 기존 타일에서 유닛 제거
                    TileMapManager.Instance.ReleaseTile(originalTilePosition);

                    // 새 타일 점유
                    List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
                    Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>()
                    {
                        { 0, this }
                    };
                    TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);

                    // 유닛 위치 최종 업데이트
                    tilePosition = previousTilePosition;
                    Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
                    finalPosition.z = 0;
                    transform.position = finalPosition;

                    // 렌더러 상태 복원 (항상 활성화)
                    unitSprite.enabled = true;
                    unitBaseSprite.enabled = true;

                    // 적 경로 업데이트
                    EnemyManager.Instance.UpdateEnemiesPath();
                    return;
                }
            }
        }

        // 배치 실패 또는 원래 위치로 돌아가는 경우
        ReturnToOriginalPosition();
    }

    // 합성 가능 여부 확인 및 프리뷰 생성
    private bool CheckAndCreateMergePreview(Vector2 tilePos)
    {
        // 타일에 유닛이 있고 합성이 가능한 경우
        if (targetTileData?.placedUnit != null &&
            targetTileData.placedUnit != this &&
            unitType == targetTileData.placedUnit.unitType &&
            GetStat(StatName.UnitStarLevel) == targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
            targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) < 3)
        {
            // 합성 타겟 기록
            mergeTargets[tilePos] = targetTileData;

            // 기존 유닛의 별 비활성화
            foreach (var star in targetTileData.placedUnit.starObjects)
            {
                star.SetActive(false);
            }

            // 현재 유닛의 렌더러만 숨기고 GameObject는 활성화 유지
            unitSprite.enabled = false;
            unitBaseSprite.enabled = false;

            // 새로운 합성 유닛 생성 및 설정
            Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
            GameObject mergedPreviewObj = PoolingManager.Instance.GetObject(unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);

            // 기존 미리보기가 있었다면 먼저 제거
            if (mergedPreviewUnit != null)
            {
                PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
            }

            mergedPreviewUnit = mergedPreviewObj.GetComponent<UnitController>();
            mergedPreviewUnit.Initialize();
            mergedPreviewUnit.InitializeUnitInfo(unitData);

            // 새 스타 레벨 설정
            int newStarLevel = (int)targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
            mergedPreviewUnit.UpGradeUnitLevel(newStarLevel);

            // 미리보기 머테리얼 설정
            mergedPreviewUnit.SetPreviewMaterial(canPlace);

            // 시각적 효과
            mergedPreviewUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

            return true;
        }

        return false;
    }

    // 합성 미리보기 정리
    private void ClearMergedPreview()
    {
        if (mergedPreviewUnit != null)
        {
            // 합성 미리보기 유닛 반환
            PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
            mergedPreviewUnit = null;
        }

        // 현재 유닛 렌더러 다시 표시
        unitSprite.enabled = true;
        unitBaseSprite.enabled = true;
    }

    // 실제 합성 수행
    private void PerformMerge()
    {
        if (mergedPreviewUnit == null || targetTileData?.placedUnit == null) return;

        // 원래 타일에서 현재 유닛 제거
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 합성 대상 유닛 가져오기
        UnitController targetUnit = targetTileData.placedUnit;

        // 새 스타 레벨 설정
        int newStarLevel = (int)GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // 합성 효과 적용
        targetUnit.ApplyEffect(1.0f);

        // 합성 프리뷰 유닛 반환
        PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
        mergedPreviewUnit = null;

        // 현재 유닛(드래그한 유닛) 제거
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);

        // 별 상태 복원
        RestoreMergeTileStars();

        // 적 경로 업데이트
        EnemyManager.Instance.UpdateEnemiesPath();
    }

    // 배치 가능성 확인을 위한 별도 메서드 (원래 위치 처리를 위해)
    private bool CheckPlacementPossibility(Vector2 targetPos, List<Vector2> offsets, Dictionary<int, UnitController> units)
    {
        // 원래 타일 데이터 임시 저장
        TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
        bool originalAvailable = false;

        if (originalTileData != null)
        {
            // 원래 위치는 일시적으로 사용 가능한 것으로 표시
            originalAvailable = originalTileData.isAvailable;
            originalTileData.isAvailable = true;
            originalTileData.placedUnit = null;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        // 배치 가능성 확인
        bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, offsets, units);

        // 원래 타일 상태 복원
        if (originalTileData != null)
        {
            originalTileData.isAvailable = originalAvailable;
            originalTileData.placedUnit = this;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        return canPlace;
    }


    // 원래 위치로 돌아가기
    private void ReturnToOriginalPosition()
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



    public void SetPreviewMaterial(bool canPlace)
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


    // 프리뷰 종료 시 원본 머테리얼로 복원
    public void DestroyPreviewUnit()
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
