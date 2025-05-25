using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using Kylin.FSM;
using BansheeGz.BGDatabase;
using Sequence = DG.Tweening.Sequence;

public class UnitController : BasicObject, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    [HideInInspector]
    public D_UnitData unitData;

    public List<GameObject> starObjects = new List<GameObject>();  // 생성된 별들을 관리하기 위한 리스트

    public UnitType unitType;

    public GameObject unitStarObject;
    [HideInInspector] public GameObject itemSlotObject;

    public bool canAttack = true;

    // 드래그 앤 드롭을 위한 변수 추가
    public bool isDragging = false;
    public Vector3 originalPosition;
    public Vector2 originalTilePosition;
    protected Vector2 previousTilePosition; // 이전 타일 위치 추적용
    protected bool hasDragged = false;
    protected bool canPlace = false;

    // 합성 관련 변수
    private TileData mergeTargetTile = null;
    protected int originalStarLevel = 0;
    protected bool isShowingMergePreview = false;

    //합성 불가할때 위치 변환 
    private UnitController swapTargetUnit = null;
    private bool isSwapping = false;

    [HideInInspector]
    public bool isMultiUnit = false; // 여러 타일을 차지하는 멀티 유닛인지

    // 아이템 슬롯 프리팹 참조 (인스펙터에서 할당 필요)
    [SerializeField] private GameObject itemSlotPrefab;

    // 아이템 슬롯 컴포넌트 캐싱
    private UnitItemSlotObject itemSlotComponent;

    // 유닛 인스턴스별 고유 ID 추가
    public string uniqueId { get; private set; }

    // 단일 타일 유닛용 정적 오프셋 및 딕셔너리
    private static readonly List<Vector2> singleTileOffset = new List<Vector2>(1) { Vector2.zero };
    private Dictionary<int, UnitController> tempUnitDict = new Dictionary<int, UnitController>(1);

    // UnitController 이벤트
    public static event System.Action<UnitController> OnUnitClicked;

    public override void Initialize()
    {
        base.Initialize();

        gameObject.layer = LayerMask.NameToLayer("Player");  // 초기화할 때 레이어 설정

        // 고유 ID 생성 (System.Guid 사용)
        uniqueId = System.Guid.NewGuid().ToString();
        
        // 아이템 슬롯 초기화
        InitializeItemSlot();
        
        // 임시 딕셔너리 초기화
        tempUnitDict.Clear();
    }

    private void InitializeItemSlot()
    {
        // 새 아이템 슬롯 생성 및 비활성화
        if (itemSlotPrefab != null)
        {
            itemSlotObject = Instantiate(itemSlotPrefab, transform);
            itemSlotComponent = itemSlotObject.GetComponent<UnitItemSlotObject>();
            itemSlotObject.SetActive(false);
        }
    }

    public override BasicObject GetNearestTarget()
    {
        // EnemyManager에서 가장 가까운 적을 찾음
        return EnemyManager.Instance.GetNearestEnemy(transform.position);
    }

    public override List<BasicObject> GetActiveTargetList()
    {
        List<Enemy> enemys = EnemyManager.Instance.GetAllEnemys();
        
        
        List<BasicObject> basicObjects = enemys
            .Where(enemy => enemy != null && 
                            enemy.GetStat(StatName.CurrentHp) > 0 && 
                            enemy.isActive)
            .Cast<BasicObject>()
            .ToList();
        return basicObjects;
    }

    public void UpdateStarDisplay(int? starLevel = null)
    {
        if (unitStarObject == null) return;

        int currentStarLevel = starLevel ?? Mathf.RoundToInt(GetStat(StatName.UnitStarLevel));

        // 현재 표시된 별 개수가 같으면 불필요한 작업 스킵
        if (starObjects.Count == currentStarLevel) return;
        
        // 기존 별 제거
        foreach (var star in starObjects)
        {
            Destroy(star);
        }
        starObjects.Clear();

        // 새 별 생성
        float spacing = 1.5f;
        float totalWidth = (currentStarLevel - 1) * spacing;
        float startX = -totalWidth / 2f;

        for (int i = 0; i < currentStarLevel; i++)
        {
            GameObject star = Instantiate(unitStarObject, transform);
            star.transform.localPosition = new Vector3(startX + (i * spacing), 0, 0);
            starObjects.Add(star);
        }
    }

    // 유닛 정보 초기화
    public virtual void InitializeUnitInfo(D_UnitData unit, D_TileCardData tileCard = null)
    {
        if (unit == null) return;

        isEnemy = false;
        canChase = false;

        unitData = unit;
        unitType = unitData.f_UnitType;

        // fsmID 설정
        fsmObj.UpdateFSM(unitData.f_fsmId);
        
        // 애니메이터 컨트롤러 설정
        SetAnimatorController(unitData.f_animControllerType);

        // 외형 설정 로직 추가
        if (appearanceProvider != null && unitData.f_unitAppearanceData != null)
        {
            // 기존에 unitAppearance가 null이면 새로 생성
            if (appearanceProvider.unitAppearance == null)
            {
                appearanceProvider.unitAppearance = new BGEntityGo();
            }

            // Entity 속성을 통해 외형 데이터를 설정
            appearanceProvider.unitAppearance.Entity = unitData.f_unitAppearanceData;

            // 외형 로드
            appearanceProvider.LoadAppearance();
        }

        SettingBasicStats();
        UpdateHpBar();
        UpdateStarDisplay();

        if (tileCard != null)
        {
            isMultiUnit = tileCard.f_isMultiTileUinit;
        }

        if (itemSlotObject != null)
        {
            itemSlotObject.SetActive(false);
        }

        CheckAttackAvailability();
    }

    public void SaveOriginalUnitPos()
    {
        canChase = true;
        originalPosition = transform.position;
    }

    private void SettingBasicStats()
    {
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

        // currentHP를 maxHP로 초기화
        if (!currentStats.ContainsKey(StatName.CurrentHp))
        {
            var maxHp = GetStat(StatName.MaxHP);
            currentStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }
    }

    public void EquipItemSlot(Sprite itemIcon)
    {
        if (itemSlotObject == null || itemSlotComponent == null)
        {
            // 아이템 슬롯이 없거나 컴포넌트가 없는 경우 초기화
            InitializeItemSlot();

            if (itemSlotObject == null || itemSlotComponent == null)
            {
                Debug.LogError("Failed to initialize itemSlotObject or itemSlotComponent");
                return;
            }
        }

        // 아이템 아이콘 설정
        itemSlotComponent.InitItemIcon(itemIcon);

        // 아이템 슬롯 활성화
        itemSlotObject.SetActive(true);
    }

    // 스탯 변경시 할 행동들
    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (GetStat(StatName.CurrentHp) <= 0) return;  // 이미 죽었거나 죽는 중이면 스탯 변경 무시

        base.OnStatChanged(subject, statChange);

        // 체력 관련 스탯이 변경되었을 때
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            // HP 바 업데이트
            UpdateHpBar();
        }
        
        // attackSpeed 바뀌었을때는 attackTimer 0부터 다시 시작
        if (statChange.statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }
    
    public override void OnDead()
    {
        isActive = false;
        isDead = true;
        baseStats.Clear();
        currentStats.Clear();

        // 기존 단일 타일 로직
        TileMapManager.Instance.ReleaseTile(originalTilePosition);
        
        // 유닛 매니저에서 등록 해제
        UnitManager.Instance.UnregisterUnit(this);
        UnitManager.Instance.NotifyUnitDead(this);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }
    
    public void CheckAttackAvailability()
    {
        // y가 9보다 크면 공격 불가
        canAttack = tilePosition.y < 9;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // 클릭 이벤트만 처리 (드래그가 아닌 경우)
        if (!hasDragged)
        {
            if (StageManager.Instance.IsBattleActive)
                return;

            OnUnitClicked?.Invoke(this);
        }
    }

    // 드래그 시작 
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (StageManager.Instance.IsBattleActive)
            return;

        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // 드래그 시작 시 초기화
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        // 현재 별 레벨을 최신 상태로 저장
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);

        // 합성 관련 변수 초기화
        mergeTargetTile = null;
        isShowingMergePreview = false;
        swapTargetUnit = null;
        isSwapping = false;
        canPlace = false;
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
        
        // 위치가 변경되지 않았으면 불필요한 처리 스킵
        if (currentTilePos == previousTilePosition) return;

        // 배치 가능 여부 확인
        canPlace = CheckPlacementPossibility(currentTilePos);

        // 현재 타일 정보 가져오기
        TileData tileData = TileMapManager.Instance.GetTileData(currentTilePos);
        
        // 이전 상태 초기화
        ResetPreviewStates();
        
        // 상태 우선순위에 따라 처리 (합성 > 교환 > 일반 이동)
        if (tileData != null && CanMergeWithTarget(tileData))
        {
            ShowMergePreview(tileData);
        }
        else if (tileData != null && CanSwapWithTarget(tileData))
        {
            ShowSwapPreview(tileData);
        }
        else
        {
            UpdateUnitPosition(TileMapManager.Instance.GetTileToWorldPosition(currentTilePos));
        }
        
        previousTilePosition = currentTilePos;
    }

    // 유닛 위치 업데이트 공통 메서드
    private void UpdateUnitPosition(Vector3 targetPosition)
    {
        targetPosition.z = -0.1f;
        transform.position = targetPosition;
    }
    
    // 드래그 종료
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        bool isSuccess = false;

        if (hasDragged && previousTilePosition != originalTilePosition)
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
            ReturnToOriginalPosition();
        }

        CheckAttackAvailability();
    }

    protected virtual void PerformSwap()
    {
        if (swapTargetUnit == null) return;

        // 타겟 타일 데이터 확인
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

        // 타겟 유닛 위치 업데이트
        swapTargetUnit.tilePosition = myOriginalPos;
        tempUnitDict.Clear();
        tempUnitDict[0] = swapTargetUnit;
        TileMapManager.Instance.OccupyTiles(myOriginalPos, singleTileOffset, tempUnitDict);

        // 현재 유닛 위치 업데이트
        tilePosition = targetOriginalPos;
        tempUnitDict.Clear();
        tempUnitDict[0] = this;
        TileMapManager.Instance.OccupyTiles(targetOriginalPos, singleTileOffset, tempUnitDict);

        // 애니메이션 적용
        transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutBack);
        swapTargetUnit.transform.DOMove(myWorldPos, 0.3f).SetEase(Ease.OutBack);

        ResetPreviewStates();
    }

    // 프리뷰 상태 초기화 (합성, 교환 등)
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
            UpdateStarDisplay(originalStarLevel);

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }

        // 위치 교환 프리뷰 초기화
        if (swapTargetUnit != null && isSwapping)
        {
            isSwapping = false;
            swapTargetUnit = null;
        }
    }

    public virtual void DeleteUnit()
    {
        // 장착된 아이템이 있다면 인벤토리로 반환
        IEquipmentSystem equipSystem = InventoryManager.Instance?.GetEquipmentSystem();
        if (equipSystem != null)
        {
            // 아이템을 가져오되 인벤토리에 자동 반환하지 않음
            D_ItemData equipItem = equipSystem.GetAndRemoveEquippedItem(this);

            // 아이템이 있다면 인벤토리에 수동으로 반환
            if (equipItem != null)
            {
                InventoryManager.Instance.AddItem(equipItem);
            }
        }

        // 타일 해제
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 유닛 매니저에서 등록 해제
        UnitManager.Instance.UnregisterUnit(this);

        // 코스트 정산
        int refundCost = 0;

        if (unitType == UnitType.None)
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
        // 원래 타일 데이터 임시 수정
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
        bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, this);

        // 원래 타일 상태 복원
        if (originalTileData != null)
        {
            originalTileData.isAvailable = originalAvailable;
            originalTileData.placedUnit = this;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        return canPlace;
    }

    public void UnequipItemSlot()
    {
        if (itemSlotObject != null)
        {
            // 아이템 슬롯 오브젝트 비활성화
            itemSlotObject.SetActive(false);
        }
    }

    // 합성 가능 여부 확인 - 최적화
    protected virtual bool CanMergeWithTarget(TileData tileData)
    {
        // 타일 데이터나 배치된 유닛이 없거나 자기 자신인 경우
        if (tileData == null || tileData.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // 멀티 유닛은 합성 불가능
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;
            
        // 유닛 타입이 다르면 합성 불가
        if (unitType != targetUnit.unitType)
            return false;
            
        // 레벨 관련 검사
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int targetStarLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel);
            
        return currentStarLevel == targetStarLevel && targetStarLevel < 5;
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

        // 현재 레벨을 기준으로 새 레벨 계산
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int newStarLevel = currentStarLevel + 1;

        // 임시로 표시용 레벨 변경 (실제 값은 변경 안함)
        UpdateStarDisplay(newStarLevel);
        // 효과 적용
        ApplyEffect(0.3f);

        // 유닛 위치 업데이트
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        UpdateUnitPosition(targetPosition);
    }

    // 위치 교환 가능 여부 확인 - 최적화
    public bool CanSwapWithTarget(TileData tileData)
    {
        if (tileData == null || tileData.placedUnit == null || tileData.placedUnit == this)
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

        swapTargetUnit = tileData.placedUnit;
        isSwapping = true;

        // 유닛 위치 업데이트
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        UpdateUnitPosition(targetPosition);
    }

    // 유닛 이동 
    protected virtual void MoveUnit()
    {
        // 원래 상태로 복원
        ResetPreviewStates();
       
        // 원본 타일에서 유닛 제거
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 새 타일 점유 - 미리 만들어둔 객체 재사용
        tempUnitDict.Clear();
        tempUnitDict[0] = this;
        TileMapManager.Instance.OccupyTiles(previousTilePosition, singleTileOffset, tempUnitDict);

        // 유닛 위치 업데이트
        tilePosition = previousTilePosition;
        Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        finalPosition.z = 0;
        transform.position = finalPosition;
    }

    // 합성 수행
    protected virtual void PerformMerge()
    {
        if (mergeTargetTile == null || mergeTargetTile.placedUnit == null)
        {
            ResetPreviewStates();
            return;
        }

        // 원본 타일에서 유닛 제거
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // 타겟 유닛 업그레이드
        UnitController targetUnit = mergeTargetTile.placedUnit;

        // 현재 레벨을 기준으로 새 레벨 계산
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int targetStarLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel);

        // 동일 레벨 확인 (안전장치)
        if (currentStarLevel != targetStarLevel)
        {
            Debug.LogWarning("합성하려는 유닛의 레벨이 일치하지 않습니다: " + currentStarLevel + " vs " + targetStarLevel);
            ReturnToOriginalPosition();
            return;
        }

        int newStarLevel = currentStarLevel + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // 원본 유닛 제거
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }

    // 원래 위치로 돌아가기
    public virtual void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
    }

    // 유닛 레벨 업그레이드 - 최적화
    public void UpGradeUnitLevel(int value)
    {
        if (!currentStats.ContainsKey(StatName.UnitStarLevel)) return;
        
        // 레벨이 이미 같으면 불필요한 작업 스킵
        if (currentStats[StatName.UnitStarLevel].value == value) return;

        // 새로운 StarLevel 설정
        currentStats[StatName.UnitStarLevel].value = value;
        originalStarLevel = value;

        // 각 스탯별 업그레이드 
        foreach (var stat in currentStats)
        {
            if (stat.Key == StatName.UnitStarLevel) continue;

            float upgradePercent = GetStatUpgradePercent(stat.Key);
            
            if (upgradePercent == 100)
            {
                // level만큼 곱하는 케이스
                currentStats[stat.Key].value = baseStats[stat.Key].value * value;
            }
            else if (upgradePercent > 0)
            {
                // 퍼센트 증가 케이스
                float percentIncrease = (value == 3) ? upgradePercent * 2 : upgradePercent;
                float multiplier = 1 + (percentIncrease / 100f);
                currentStats[stat.Key].value = Mathf.RoundToInt(baseStats[stat.Key].value * multiplier);
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
    public void RefillHp()
    {
        // 현재 유닛이 활성화 상태인지 확인
        if (!isActive || isDead) return;

        // 최대 HP 계산
        float maxHp = GetStat(StatName.MaxHP);

        // 현재 HP를 최대 HP로 설정
        if (currentStats.ContainsKey(StatName.CurrentHp))
        {
            currentStats[StatName.CurrentHp].value = Mathf.FloorToInt(maxHp);
        }
        else
        {
            // 현재 HP 스탯이 없으면 새로 생성
            currentStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }
       
        // 현재 마나 소모
        ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(GetStat(StatName.CurrentMana)), 1f);
       
        // HP 바 업데이트
        UpdateHpBar();
    }

    // 유닛에 효과 적용 (띠용 효과)
    public void ApplyEffect(float duration = 0.5f)
    {
        if(this == null || transform == null) return;
        
        // 기존 Tween 종료
        transform.DOKill(true);
   
        // 원래 스케일 저장
        Vector3 originalScale = transform.localScale;
   
        // 띠용 효과 시퀀스
        Sequence sequence = DOTween.Sequence();
   
        // 1. 커지는 효과
        sequence.Append(transform.DOScale(originalScale * 1.7f, duration * 0.3f).SetEase(Ease.OutQuad));
   
        // 2. 작아지는 효과
        sequence.Append(transform.DOScale(originalScale * 0.8f, duration * 0.2f).SetEase(Ease.InQuad));
   
        // 3. 원래 크기로 돌아오기
        sequence.Append(transform.DOScale(originalScale, duration * 0.5f).SetEase(Ease.OutElastic, 1.2f, 0.5f));
    }
}