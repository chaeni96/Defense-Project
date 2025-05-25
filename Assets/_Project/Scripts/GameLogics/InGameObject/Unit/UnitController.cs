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

    public List<GameObject> starObjects = new List<GameObject>();  // ������ ������ �����ϱ� ���� ����Ʈ

    public UnitType unitType;

    public GameObject unitStarObject;
    [HideInInspector] public GameObject itemSlotObject;

    public bool canAttack = true;

    // �巡�� �� ����� ���� ���� �߰�
    public bool isDragging = false;
    public Vector3 originalPosition;
    public Vector2 originalTilePosition;
    protected Vector2 previousTilePosition; // ���� Ÿ�� ��ġ ������
    protected bool hasDragged = false;
    protected bool canPlace = false;

    // �ռ� ���� ����
    private TileData mergeTargetTile = null;
    protected int originalStarLevel = 0;
    protected bool isShowingMergePreview = false;

    //�ռ� �Ұ��Ҷ� ��ġ ��ȯ 
    private UnitController swapTargetUnit = null;
    private bool isSwapping = false;

    [HideInInspector]
    public bool isMultiUnit = false; // ���� Ÿ���� �����ϴ� ��Ƽ ��������

    // ������ ���� ������ ���� (�ν����Ϳ��� �Ҵ� �ʿ�)
    [SerializeField] private GameObject itemSlotPrefab;

    // ������ ���� ������Ʈ ĳ��
    private UnitItemSlotObject itemSlotComponent;

    // ���� �ν��Ͻ��� ���� ID �߰�
    public string uniqueId { get; private set; }

    // ���� Ÿ�� ���ֿ� ���� ������ �� ��ųʸ�
    private static readonly List<Vector2> singleTileOffset = new List<Vector2>(1) { Vector2.zero };
    private Dictionary<int, UnitController> tempUnitDict = new Dictionary<int, UnitController>(1);

    // UnitController �̺�Ʈ
    public static event System.Action<UnitController> OnUnitClicked;

    public override void Initialize()
    {
        base.Initialize();

        gameObject.layer = LayerMask.NameToLayer("Player");  // �ʱ�ȭ�� �� ���̾� ����

        // ���� ID ���� (System.Guid ���)
        uniqueId = System.Guid.NewGuid().ToString();
        
        // ������ ���� �ʱ�ȭ
        InitializeItemSlot();
        
        // �ӽ� ��ųʸ� �ʱ�ȭ
        tempUnitDict.Clear();
    }

    private void InitializeItemSlot()
    {
        // �� ������ ���� ���� �� ��Ȱ��ȭ
        if (itemSlotPrefab != null)
        {
            itemSlotObject = Instantiate(itemSlotPrefab, transform);
            itemSlotComponent = itemSlotObject.GetComponent<UnitItemSlotObject>();
            itemSlotObject.SetActive(false);
        }
    }

    public override BasicObject GetNearestTarget()
    {
        // EnemyManager���� ���� ����� ���� ã��
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

        // ���� ǥ�õ� �� ������ ������ ���ʿ��� �۾� ��ŵ
        if (starObjects.Count == currentStarLevel) return;
        
        // ���� �� ����
        foreach (var star in starObjects)
        {
            Destroy(star);
        }
        starObjects.Clear();

        // �� �� ����
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

    // ���� ���� �ʱ�ȭ
    public virtual void InitializeUnitInfo(D_UnitData unit, D_TileCardData tileCard = null)
    {
        if (unit == null) return;

        isEnemy = false;
        canChase = false;

        unitData = unit;
        unitType = unitData.f_UnitType;

        // fsmID ����
        fsmObj.UpdateFSM(unitData.f_fsmId);
        
        // �ִϸ����� ��Ʈ�ѷ� ����
        SetAnimatorController(unitData.f_animControllerType);

        // ���� ���� ���� �߰�
        if (appearanceProvider != null && unitData.f_unitAppearanceData != null)
        {
            // ������ unitAppearance�� null�̸� ���� ����
            if (appearanceProvider.unitAppearance == null)
            {
                appearanceProvider.unitAppearance = new BGEntityGo();
            }

            // Entity �Ӽ��� ���� ���� �����͸� ����
            appearanceProvider.unitAppearance.Entity = unitData.f_unitAppearanceData;

            // ���� �ε�
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
        // ���� ���ȵ� �ʱ�ȭ
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

        // ���� ������ �⺻ �������� �ʱ�ȭ
        foreach (var baseStat in baseStats)
        {
            int statValue = baseStat.Value.value;

            // StarLevel�� ���� ���� ����
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

        // currentHP�� maxHP�� �ʱ�ȭ
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
            // ������ ������ ���ų� ������Ʈ�� ���� ��� �ʱ�ȭ
            InitializeItemSlot();

            if (itemSlotObject == null || itemSlotComponent == null)
            {
                Debug.LogError("Failed to initialize itemSlotObject or itemSlotComponent");
                return;
            }
        }

        // ������ ������ ����
        itemSlotComponent.InitItemIcon(itemIcon);

        // ������ ���� Ȱ��ȭ
        itemSlotObject.SetActive(true);
    }

    // ���� ����� �� �ൿ��
    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (GetStat(StatName.CurrentHp) <= 0) return;  // �̹� �׾��ų� �״� ���̸� ���� ���� ����

        base.OnStatChanged(subject, statChange);

        // ü�� ���� ������ ����Ǿ��� ��
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            // HP �� ������Ʈ
            UpdateHpBar();
        }
        
        // attackSpeed �ٲ�������� attackTimer 0���� �ٽ� ����
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

        // ���� ���� Ÿ�� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);
        
        // ���� �Ŵ������� ��� ����
        UnitManager.Instance.UnregisterUnit(this);
        UnitManager.Instance.NotifyUnitDead(this);
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // Ÿ�̸� ����
        }
    }
    
    public void CheckAttackAvailability()
    {
        // y�� 9���� ũ�� ���� �Ұ�
        canAttack = tilePosition.y < 9;
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        // Ŭ�� �̺�Ʈ�� ó�� (�巡�װ� �ƴ� ���)
        if (!hasDragged)
        {
            if (StageManager.Instance.IsBattleActive)
                return;

            OnUnitClicked?.Invoke(this);
        }
    }

    // �巡�� ���� 
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (StageManager.Instance.IsBattleActive)
            return;

        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // �巡�� ���� �� �ʱ�ȭ
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        // ���� �� ������ �ֽ� ���·� ����
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);

        // �ռ� ���� ���� �ʱ�ȭ
        mergeTargetTile = null;
        isShowingMergePreview = false;
        swapTargetUnit = null;
        isSwapping = false;
        canPlace = false;
    }

    // �巡�� �� ȣ��
    public virtual void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true;

        // ���콺 ��ġ���� Ÿ�� ��ġ ���
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        
        // ��ġ�� ������� �ʾ����� ���ʿ��� ó�� ��ŵ
        if (currentTilePos == previousTilePosition) return;

        // ��ġ ���� ���� Ȯ��
        canPlace = CheckPlacementPossibility(currentTilePos);

        // ���� Ÿ�� ���� ��������
        TileData tileData = TileMapManager.Instance.GetTileData(currentTilePos);
        
        // ���� ���� �ʱ�ȭ
        ResetPreviewStates();
        
        // ���� �켱������ ���� ó�� (�ռ� > ��ȯ > �Ϲ� �̵�)
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

    // ���� ��ġ ������Ʈ ���� �޼���
    private void UpdateUnitPosition(Vector3 targetPosition)
    {
        targetPosition.z = -0.1f;
        transform.position = targetPosition;
    }
    
    // �巡�� ����
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        bool isSuccess = false;

        if (hasDragged && previousTilePosition != originalTilePosition)
        {
            // �ռ� ó��
            if (isShowingMergePreview && mergeTargetTile != null && CanMergeWithTarget(mergeTargetTile))
            {
                PerformMerge();
                isSuccess = true;
            }
            // ��ȯ ó��
            else if (isSwapping && swapTargetUnit != null && CanSwapWithTarget(TileMapManager.Instance.GetTileData(previousTilePosition)))
            {
                PerformSwap();
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
            ResetPreviewStates();
            ReturnToOriginalPosition();
        }

        CheckAttackAvailability();
    }

    protected virtual void PerformSwap()
    {
        if (swapTargetUnit == null) return;

        // Ÿ�� Ÿ�� ������ Ȯ��
        TileData targetTileData = TileMapManager.Instance.GetTileData(previousTilePosition);
        if (targetTileData == null) return;

        // ���� ��ġ�� Ÿ�� ��ġ ����
        Vector2 myOriginalPos = originalTilePosition;
        Vector2 targetOriginalPos = swapTargetUnit.tilePosition;

        Vector3 myWorldPos = TileMapManager.Instance.GetTileToWorldPosition(myOriginalPos);
        Vector3 targetWorldPos = TileMapManager.Instance.GetTileToWorldPosition(targetOriginalPos);

        // Ÿ�� ������ ������Ʈ
        TileMapManager.Instance.ReleaseTile(myOriginalPos);
        TileMapManager.Instance.ReleaseTile(targetOriginalPos);

        // Ÿ�� ���� ��ġ ������Ʈ
        swapTargetUnit.tilePosition = myOriginalPos;
        tempUnitDict.Clear();
        tempUnitDict[0] = swapTargetUnit;
        TileMapManager.Instance.OccupyTiles(myOriginalPos, singleTileOffset, tempUnitDict);

        // ���� ���� ��ġ ������Ʈ
        tilePosition = targetOriginalPos;
        tempUnitDict.Clear();
        tempUnitDict[0] = this;
        TileMapManager.Instance.OccupyTiles(targetOriginalPos, singleTileOffset, tempUnitDict);

        // �ִϸ��̼� ����
        transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutBack);
        swapTargetUnit.transform.DOMove(myWorldPos, 0.3f).SetEase(Ease.OutBack);

        ResetPreviewStates();
    }

    // ������ ���� �ʱ�ȭ (�ռ�, ��ȯ ��)
    private void ResetPreviewStates()
    {
        // �ռ� ������ �ʱ�ȭ
        if (mergeTargetTile != null && isShowingMergePreview)
        {
            // Ÿ�� ������ �� ����
            if (mergeTargetTile.placedUnit != null)
            {
                foreach (var star in mergeTargetTile.placedUnit.starObjects)
                {
                    star.SetActive(true);
                }
            }

            // �� ������ ���� ������ ����
            UpdateStarDisplay(originalStarLevel);

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }

        // ��ġ ��ȯ ������ �ʱ�ȭ
        if (swapTargetUnit != null && isSwapping)
        {
            isSwapping = false;
            swapTargetUnit = null;
        }
    }

    public virtual void DeleteUnit()
    {
        // ������ �������� �ִٸ� �κ��丮�� ��ȯ
        IEquipmentSystem equipSystem = InventoryManager.Instance?.GetEquipmentSystem();
        if (equipSystem != null)
        {
            // �������� �������� �κ��丮�� �ڵ� ��ȯ���� ����
            D_ItemData equipItem = equipSystem.GetAndRemoveEquippedItem(this);

            // �������� �ִٸ� �κ��丮�� �������� ��ȯ
            if (equipItem != null)
            {
                InventoryManager.Instance.AddItem(equipItem);
            }
        }

        // Ÿ�� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // ���� �Ŵ������� ��� ����
        UnitManager.Instance.UnregisterUnit(this);

        // �ڽ�Ʈ ����
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

    // ��ġ �������� Ȯ��
    protected virtual bool CheckPlacementPossibility(Vector2 targetPos)
    {
        // ���� Ÿ�� ������ �ӽ� ����
        TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
        bool originalAvailable = false;

        if (originalTileData != null)
        {
            originalAvailable = originalTileData.isAvailable;
            originalTileData.isAvailable = true;
            originalTileData.placedUnit = null;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        // ��ġ ���ɼ� Ȯ��
        bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, this);

        // ���� Ÿ�� ���� ����
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
            // ������ ���� ������Ʈ ��Ȱ��ȭ
            itemSlotObject.SetActive(false);
        }
    }

    // �ռ� ���� ���� Ȯ�� - ����ȭ
    protected virtual bool CanMergeWithTarget(TileData tileData)
    {
        // Ÿ�� �����ͳ� ��ġ�� ������ ���ų� �ڱ� �ڽ��� ���
        if (tileData == null || tileData.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // ��Ƽ ������ �ռ� �Ұ���
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;
            
        // ���� Ÿ���� �ٸ��� �ռ� �Ұ�
        if (unitType != targetUnit.unitType)
            return false;
            
        // ���� ���� �˻�
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int targetStarLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel);
            
        return currentStarLevel == targetStarLevel && targetStarLevel < 5;
    }

    // �ռ� ������ ǥ��
    private void ShowMergePreview(TileData tileData)
    {
        mergeTargetTile = tileData;
        isShowingMergePreview = true;

        // Ÿ�� ������ �� ��Ȱ��ȭ
        foreach (var star in tileData.placedUnit.starObjects)
        {
            star.SetActive(false);
        }

        // ���� ������ �������� �� ���� ���
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int newStarLevel = currentStarLevel + 1;

        // �ӽ÷� ǥ�ÿ� ���� ���� (���� ���� ���� ����)
        UpdateStarDisplay(newStarLevel);
        // ȿ�� ����
        ApplyEffect(0.3f);

        // ���� ��ġ ������Ʈ
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        UpdateUnitPosition(targetPosition);
    }

    // ��ġ ��ȯ ���� ���� Ȯ�� - ����ȭ
    public bool CanSwapWithTarget(TileData tileData)
    {
        if (tileData == null || tileData.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // ��Ƽ ������ ��ȯ �Ұ���
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;

        // �ռ��� ������ ���� ��ȯ ��� �ռ� ó��
        if (CanMergeWithTarget(tileData))
            return false;

        // ���� ������ ��� ��������� ��ȯ ����
        return true;
    }

    private void ShowSwapPreview(TileData tileData)
    {
        if (tileData?.placedUnit == null) return;

        swapTargetUnit = tileData.placedUnit;
        isSwapping = true;

        // ���� ��ġ ������Ʈ
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        UpdateUnitPosition(targetPosition);
    }

    // ���� �̵� 
    protected virtual void MoveUnit()
    {
        // ���� ���·� ����
        ResetPreviewStates();
       
        // ���� Ÿ�Ͽ��� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // �� Ÿ�� ���� - �̸� ������ ��ü ����
        tempUnitDict.Clear();
        tempUnitDict[0] = this;
        TileMapManager.Instance.OccupyTiles(previousTilePosition, singleTileOffset, tempUnitDict);

        // ���� ��ġ ������Ʈ
        tilePosition = previousTilePosition;
        Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
        finalPosition.z = 0;
        transform.position = finalPosition;
    }

    // �ռ� ����
    protected virtual void PerformMerge()
    {
        if (mergeTargetTile == null || mergeTargetTile.placedUnit == null)
        {
            ResetPreviewStates();
            return;
        }

        // ���� Ÿ�Ͽ��� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // Ÿ�� ���� ���׷��̵�
        UnitController targetUnit = mergeTargetTile.placedUnit;

        // ���� ������ �������� �� ���� ���
        int currentStarLevel = (int)GetStat(StatName.UnitStarLevel);
        int targetStarLevel = (int)targetUnit.GetStat(StatName.UnitStarLevel);

        // ���� ���� Ȯ�� (������ġ)
        if (currentStarLevel != targetStarLevel)
        {
            Debug.LogWarning("�ռ��Ϸ��� ������ ������ ��ġ���� �ʽ��ϴ�: " + currentStarLevel + " vs " + targetStarLevel);
            ReturnToOriginalPosition();
            return;
        }

        int newStarLevel = currentStarLevel + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // ���� ���� ����
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);
    }

    // ���� ��ġ�� ���ư���
    public virtual void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
    }

    // ���� ���� ���׷��̵� - ����ȭ
    public void UpGradeUnitLevel(int value)
    {
        if (!currentStats.ContainsKey(StatName.UnitStarLevel)) return;
        
        // ������ �̹� ������ ���ʿ��� �۾� ��ŵ
        if (currentStats[StatName.UnitStarLevel].value == value) return;

        // ���ο� StarLevel ����
        currentStats[StatName.UnitStarLevel].value = value;
        originalStarLevel = value;

        // �� ���Ⱥ� ���׷��̵� 
        foreach (var stat in currentStats)
        {
            if (stat.Key == StatName.UnitStarLevel) continue;

            float upgradePercent = GetStatUpgradePercent(stat.Key);
            
            if (upgradePercent == 100)
            {
                // level��ŭ ���ϴ� ���̽�
                currentStats[stat.Key].value = baseStats[stat.Key].value * value;
            }
            else if (upgradePercent > 0)
            {
                // �ۼ�Ʈ ���� ���̽�
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
                return 100;  // level��ŭ ���ϱ�

            case StatName.AttackRange:
            case StatName.ProjectileSpeed:
                return 10;   // 10% ����, level3������ 20%

            // �ٸ� ���ȵ�
            default:
                return 0;    // �������� ����
        }
    }
    public void RefillHp()
    {
        // ���� ������ Ȱ��ȭ �������� Ȯ��
        if (!isActive || isDead) return;

        // �ִ� HP ���
        float maxHp = GetStat(StatName.MaxHP);

        // ���� HP�� �ִ� HP�� ����
        if (currentStats.ContainsKey(StatName.CurrentHp))
        {
            currentStats[StatName.CurrentHp].value = Mathf.FloorToInt(maxHp);
        }
        else
        {
            // ���� HP ������ ������ ���� ����
            currentStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }
       
        // ���� ���� �Ҹ�
        ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(GetStat(StatName.CurrentMana)), 1f);
       
        // HP �� ������Ʈ
        UpdateHpBar();
    }

    // ���ֿ� ȿ�� ���� (��� ȿ��)
    public void ApplyEffect(float duration = 0.5f)
    {
        if(this == null || transform == null) return;
        
        // ���� Tween ����
        transform.DOKill(true);
   
        // ���� ������ ����
        Vector3 originalScale = transform.localScale;
   
        // ��� ȿ�� ������
        Sequence sequence = DOTween.Sequence();
   
        // 1. Ŀ���� ȿ��
        sequence.Append(transform.DOScale(originalScale * 1.7f, duration * 0.3f).SetEase(Ease.OutQuad));
   
        // 2. �۾����� ȿ��
        sequence.Append(transform.DOScale(originalScale * 0.8f, duration * 0.2f).SetEase(Ease.InQuad));
   
        // 3. ���� ũ��� ���ƿ���
        sequence.Append(transform.DOScale(originalScale, duration * 0.5f).SetEase(Ease.OutElastic, 1.2f, 0.5f));
    }
}