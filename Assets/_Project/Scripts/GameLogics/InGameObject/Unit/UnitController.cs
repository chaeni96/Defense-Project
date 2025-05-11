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
using Unity.VisualScripting;


public class UnitController : BasicObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HideInInspector]
    public float attackTimer = 0f;  // Ÿ�̸� �߰�

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    [HideInInspector]
    public D_UnitData unitData;

    public List<GameObject> starObjects = new List<GameObject>();  // ������ ������ �����ϱ� ���� ����Ʈ

    public UnitType unitType;

    public GameObject unitStarObject;
    public GameObject itemSlotObject;

    public bool isActive = true;

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

    //�ռ� �Ұ��Ҵ� ��ġ ��ȯ 
    private UnitController swapTargetUnit = null;
    private bool isSwapping = false;

    [HideInInspector]
    public bool isMultiUnit = false; // ���� Ÿ���� �����ϴ� ��Ƽ ��������

    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�


    // ������ ���� ������ ���� (�ν����Ϳ��� �Ҵ� �ʿ�)
    [SerializeField] private GameObject itemSlotPrefab;

    // ������ ���� ������Ʈ ĳ��
    private UnitItemSlotObject itemSlotComponent;


    // UnitController.cs�� �߰�
    public static event System.Action<UnitController> OnUnitClicked;

    public override void Initialize()
    {
        base.Initialize();

        gameObject.layer = LayerMask.NameToLayer("Player");  // �ʱ�ȭ�� �� ���̾� ����
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;


        // ������ ���� �ʱ�ȭ
        InitializeItemSlot();
    }

    private void InitializeItemSlot()
    {
        // ���� ������ ������ ������ ����
        if (itemSlotObject != null)
        {
            Destroy(itemSlotObject);
        }

        // �� ������ ���� ���� �� ��Ȱ��ȭ
        if (itemSlotPrefab != null)
        {
            itemSlotObject = Instantiate(itemSlotPrefab, transform);
            itemSlotComponent = itemSlotObject.GetComponent<UnitItemSlotObject>();
            itemSlotPrefab.SetActive(false);
        }
        else
        {
            Debug.LogError("itemSlotPrefab is not assigned to UnitController");
        }
    }

    public override BasicObject GetTarget()
    {
        // EnemyManager���� ���� ����� ���� ã��
        return EnemyManager.Instance.GetNearestEnemy(transform.position);
    }


    private void UpdateHpBar()
    {
        float currentHp = GetStat(StatName.CurrentHp);
        float maxHp = GetStat(StatName.MaxHP);

        if (hpBar != null && maxHp > 0)
        {
            hpBar.value = currentHp / maxHp;
        }
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

    //���� ���� �ʱ�ȭ
    public void InitializeUnitInfo(D_UnitData unit, D_TileCardData tileCard = null)
    {
        if (unit == null) return;

        isEnemy = false;

        unitData = unit;
        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

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
    }

    public void SaveOriginalUnitPos()
    {
        originalPosition = transform.position;

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

    //���� ����� �� �ൿ��
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
        ApplyEffect();

        //attackSpeed �ٲ�������� attackTimer 0���� �ٽ� ����
        if (statChange.statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }

    public void ModifyStat(StatName statName, int value, float multiply)
    {
        // ���� ������ ������ �� ����
        if (currentStats.TryGetValue(statName, out var currentStat))
        {
            currentStat.value += value;
            currentStat.multiply *= multiply;
        }
        // ������ �� ���� �߰�
        else
        {
            currentStats[statName] = new StatStorage
            {
                statName = statName,
                value = value,
                multiply = multiply
            };
        }

        // ���� ���濡 ���� ȿ�� ���� (HP �� ������Ʈ ��)
        if (statName == StatName.CurrentHp || statName == StatName.MaxHP)
        {
            UpdateHpBar();
        }
        else if (statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }

        // ��Ÿ �ʿ��� ȿ��
        ApplyEffect();
    }


    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker�� ���ݷ� 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
                HitEffect();
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            OnDead();
        }
    }

    public void HitEffect()
    {

        // �������� ������ ���������� ������
    }

    public void OnDead()
    {
        isActive = false;
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

    // �巡�� ���� 
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        // �巡�� ���� ���� Ŭ�� �̺�Ʈ �߻�
        OnUnitClicked?.Invoke(this);


        isDragging = true;
        hasDragged = false; // �巡�� ���� �� �ʱ�ȭ
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        // ���� �� ���� ����
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);

        // �ռ� ���� ���� �ʱ�ȭ
        mergeTargetTile = null;
        isShowingMergePreview = false;

        // �巡�� ���� ����
        Vector3 position = transform.position;
        position.z = -1;
        transform.position = position;

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

        // ��ġ ���� ���� Ȯ��
        canPlace = CheckPlacementPossibility(currentTilePos);

        // ���� Ÿ�� ���� ��������
        TileData tileData = TileMapManager.Instance.GetTileData(currentTilePos);


        // Ÿ�� ��ġ�� ����Ǿ��ų� �ռ� ���°� ����� ��쿡�� ó��
        bool canUpdatePreview = currentTilePos != previousTilePosition ||
                           (tileData != null && CanMergeWithTarget(tileData) != isShowingMergePreview) ||
                           (tileData != null && CanSwapWithTarget(tileData) != isSwapping);

        if (canUpdatePreview)
        {
            // ���� �ռ� ������ ���� �ʱ�ȭ
            ResetMergePreview();

            // �ռ� ���� ���� Ȯ�� �� ������ ǥ��
            if (CanMergeWithTarget(tileData))
            {
                ShowMergePreview(tileData);
            }
            // �ռ��� �Ұ��������� ��ȯ�� ������ ���
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
    }

    private void UpdateDragPosition(Vector2 currentTilePos)
    {
        // �Ϲ� �̵� - ��Ȯ�� ���콺 ��ġ�� ��ġ
        Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
        newPosition.z = -0.1f;
        transform.position = newPosition;
        SetPreviewMaterial(canPlace);

    }

    // �巡�� ����
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // �������� �����
        GameManager.Instance.HideTrashCan();

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
    }

    protected virtual void PerformSwap()
    {
        if (swapTargetUnit == null) return;

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

        // Ÿ�� ������ ��ġ ������Ʈ
        swapTargetUnit.tilePosition = myOriginalPos;
        List<Vector2> targetTileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> targetUnits = new Dictionary<int, UnitController>() { { 0, swapTargetUnit } };
        TileMapManager.Instance.OccupyTiles(myOriginalPos, targetTileOffsets, targetUnits);

        // ���� ������ ��ġ ������Ʈ
        tilePosition = targetOriginalPos;
        List<Vector2> myTileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> myUnits = new Dictionary<int, UnitController>() { { 0, this } };
        TileMapManager.Instance.OccupyTiles(targetOriginalPos, myTileOffsets, myUnits);

        // �ִϸ��̼� ����
        transform.DOMove(targetWorldPos, 0.3f).SetEase(Ease.OutBack);
        swapTargetUnit.transform.DOMove(myWorldPos, 0.3f).SetEase(Ease.OutBack);



        ResetPreviewStates();
    }

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
            if (GetStat(StatName.UnitStarLevel) != originalStarLevel)
            {
                UpGradeUnitLevel(originalStarLevel);
            }

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
     
        // ���� ���� Ÿ�� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);
     

        // ���� �Ŵ������� ��� ����
        UnitManager.Instance.UnregisterUnit(this);

        //�ڽ�Ʈ ����
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

// ��ġ �������� Ȯ��
 protected virtual bool CheckPlacementPossibility(Vector2 targetPos)
    {
        {
            // ���� ���� Ÿ�� ����
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
            List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
            Dictionary<int, UnitController> units = new Dictionary<int, UnitController>() { { 0, this } };
            bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, tileOffsets, units);

            // ���� Ÿ�� ���� ����
            if (originalTileData != null)
            {
                originalTileData.isAvailable = originalAvailable;
                originalTileData.placedUnit = this;
                TileMapManager.Instance.SetTileData(originalTileData);
            }

            return canPlace;
        }
    }

    public void UnequipItemSlot()
    {
        if (itemSlotObject != null)
        {
            // ������ ���� ������Ʈ ��Ȱ��ȭ
            itemSlotObject.SetActive(false);
        }
    }


    // �ռ� ������ �ʱ�ȭ
    private void ResetMergePreview()
    {
        // ���� Ÿ���� �ְ�, ���� �ռ� �����並 �����ְ� �ִٸ�
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
            if (GetStat(StatName.UnitStarLevel) != originalStarLevel)
            {
                UpGradeUnitLevel(originalStarLevel);
            }

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

    // �ռ� ���� ���� Ȯ��
    protected virtual bool CanMergeWithTarget(TileData tileData)
    {
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

        // ��Ƽ ������ �ռ� �Ұ���
        if (isMultiUnit || targetUnit.isMultiUnit)
            return false;

        return unitType == targetUnit.unitType &&
               originalStarLevel == targetUnit.GetStat(StatName.UnitStarLevel) &&
               targetUnit.GetStat(StatName.UnitStarLevel) < 5;
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

        // �� ������ ���׷��̵�� ������ ǥ��
        int newStarLevel = originalStarLevel + 1;
        UpGradeUnitLevel(newStarLevel);

        // �߿�: �巡�� ���� ������ ��Ȯ�� �ռ� ��� ���� ���� ��ġ
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        // ������ ���׸��� ����
        SetPreviewMaterial(canPlace);

        // TODO: �ռ������� �ð��� ȿ�� (�� ���� ����)

    }

    // ��ġ ��ȯ ���� ���� Ȯ��
    public bool CanSwapWithTarget(TileData tileData)
    {
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
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

        TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
        if (originalTileData == null) return;

        swapTargetUnit = tileData.placedUnit;
        isSwapping = true;

        // ���� �巡�� ���� ������ ��ġ�� ��ȯ ��� ���� ��ġ�� ����
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(new Vector2(tileData.tilePosX, tileData.tilePosY));
        targetPosition.z = -0.1f;
        transform.position = targetPosition;

        //�ռ� �������� �ƴ����� ����
        SetPreviewMaterial(canPlace);

    }

    // ���� �̵�
    protected virtual void MoveUnit()
    {
        // ���� ���·� ����
        ResetMergePreview();
       
        // ���� ���� Ÿ�� ����
        // ���� Ÿ�Ͽ��� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // �� Ÿ�� ����
        List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
        Dictionary<int, UnitController> units = new Dictionary<int, UnitController>() { { 0, this } };
        TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, units);
       

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
            ResetMergePreview();
            return;
        }

        // ���� Ÿ�Ͽ��� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // Ÿ�� ���� ���׷��̵�
        UnitController targetUnit = mergeTargetTile.placedUnit;
        int newStarLevel = originalStarLevel + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // ȿ�� ����
        targetUnit.ApplyEffect(1.0f);

        // ���� ���� ����
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);

    }

    // ���� ��ġ�� ���ư���
    public virtual void ReturnToOriginalPosition()
    {
        transform.DOMove(originalPosition, 0.3f).SetEase(Ease.OutBack);
    }


    public void UpGradeUnitLevel(int value)
    {

        if (!currentStats.ContainsKey(StatName.UnitStarLevel)) return;

        // ���ο� StarLevel ����
        currentStats[StatName.UnitStarLevel].value = value;

        // StarLevel�� ����Ǿ����Ƿ� �ٸ� ��� ���ȵ� ���ο� StarLevel�� ���� ����
        foreach (var stat in currentStats)
        {
            if (stat.Key == StatName.UnitStarLevel) continue;

            float upgradePercent = GetStatUpgradePercent(stat.Key);

            if (upgradePercent == 100)
            {
                // 100�� level��ŭ ���ϴ� Ư�� ���̽� (Atk, ProjectileCount ��)
                stat.Value.value = baseStats[stat.Key].value * value;
            }
            else if (upgradePercent > 0)
            {
                // �Ϲ����� �ۼ�Ʈ ����
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
                return 100;  // level��ŭ ���ϱ�

            case StatName.AttackRange:
            case StatName.ProjectileSpeed:
                return 10;   // 10% ����, level3������ 20%

            // �ٸ� ���ȵ�
            default:
                return 0;    // �������� ����
        }
    }



    public virtual void SetPreviewMaterial(bool canPlace)
    {
        // ��ġ ���� ���ο� ���� Ÿ�� ���� ����
        UnityEngine.Color tileColor = canPlace ? UnityEngine.Color.green : UnityEngine.Color.red;

        // ���İ� ���� (�������ϰ�)
        tileColor.a = 0.5f;

        // ���� ������ ��ġ�� Ÿ�� ���� ����
        //TileMapManager.Instance.SetTileColor(tilePosition, tileColor);

    }




    public void ApplyEffect(float duration = 0.5f)
    {
    }

}
