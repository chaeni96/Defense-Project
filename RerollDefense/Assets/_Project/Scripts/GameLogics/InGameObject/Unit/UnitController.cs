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
    public float attackTimer = 0f;  // Ÿ�̸� �߰�

    [HideInInspector]
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;

    [HideInInspector]
    public bool canAttack = true; // ���� ���� ����

    [HideInInspector]
    public D_UnitData unitData;

    public List<GameObject> starObjects = new List<GameObject>();  // ������ ������ �����ϱ� ���� ����Ʈ

    public UnitType unitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;


    //inspector�� �Ҵ�
    [SerializeField] private Material enabledMaterial;   // ��ġ ������ �� ���
    [SerializeField] private Material disabledMaterial; // ��ġ �Ұ����� �� ���
    [SerializeField] private Material deleteMaterial;   // ��ġ ������ �� ���
    [SerializeField] private Material originalMaterial; //�⺻ ���׸���
    [SerializeField] private LayerMask unitLayer;  // Inspector���� Unit ���̾� üũ

    private int unitSortingOrder;
    private int baseSortingOrder;
    private bool isActive = true;

    // �巡�� �� ����� ���� ���� �߰�
    public bool isDragging = false;
    private Vector3 originalPosition;
    private Vector2 originalTilePosition;
    private Vector2 previousTilePosition; // ���� Ÿ�� ��ġ ������
    private bool hasDragged = false;
    private bool canPlace = false;
    // �ռ� ���� ����
    private TileData mergeTargetTile = null;
    private int originalStarLevel = 0;
    private bool isShowingMergePreview = false;


    private bool isOverTrashCan = false;

    public override void Initialize()
    {
        base.Initialize();
        gameObject.layer = LayerMask.NameToLayer("Player");  // �ʱ�ȭ�� �� ���̾� ����

        unitSortingOrder = 0;
        baseSortingOrder = -1;
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = baseSortingOrder; // ���̽��� �׻� �Ѵܰ� �ڿ�

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

    //���� ���� �ʱ�ȭ
    public void InitializeUnitInfo(D_UnitData unit)
    {
        if (unit == null) return;

        isEnemy = false;

        unitData = unit;
        attackType = unitData.f_SkillAttackType;
        unitType = unitData.f_UnitType;

        // ���� ���ȵ� �ʱ�ȭ
        baseStats.Clear();
        currentStats.Clear();


        //TODO : enemy�� ����Ҽ������Ƿ� basicObject�� �̵�
        // Subject ���
        // ��� StatSubject�� ���� ���� �������� �� �ջ�
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

        UpdateStarDisplay();

    }


    //���� ����� �� �ൿ��
    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        base.OnStatChanged(subject, statChange);
        
        ApplyEffect();

        //attackSpeed �ٲ�������� attackTimer 0���� �ٽ� ����
        if (statChange.statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }
    }

    public void MoveScale()
    {
        // DOPunchScale �� �Ķ����
        // punch:ũ�� ��ȭ
        // duration: ��ü ��� �ð�
        // vibrato: ���� Ƚ��
        // elasticity: ź�� (0~1)

        unitSprite.transform.DOPunchScale(punch: new Vector3(0.4f, 0.4f, 0f), duration: 0.1f, vibrato: 4, elasticity: 0.8f);

    }

    // ���� ���� ���θ� Ȯ���ϴ� �޼���
    public void CheckAttackAvailability()
    {
        // y�� 9���� ũ�� ���� �Ұ�
        canAttack = tilePosition.y <= 9;
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
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // �巡�� ���� �� �ʱ�ȭ
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        canAttack = false;

        // ���� �� ���� ����
        originalStarLevel = (int)GetStat(StatName.UnitStarLevel);

        // �ռ� ���� ���� �ʱ�ȭ
        mergeTargetTile = null;
        isShowingMergePreview = false;

        // �巡�� ���� ����
        Vector3 position = transform.position;
        position.z = -1;
        transform.position = position;

        unitSprite.sortingOrder = 100;
        unitBaseSprite.sortingOrder = 99;

        canPlace = false;

        // �������� ǥ��
        GameManager.Instance.ShowTrashCan();
    }

    // �巡�� �� ȣ��
    public void OnDrag(PointerEventData eventData)
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

        // Ÿ�� ��ġ�� ����Ǿ��ų� �ռ� ���°� ����� ��쿡�� ó��
        bool canUpdatePreview = currentTilePos != previousTilePosition || (CanMergeWithTarget(tileData) != isShowingMergePreview);

        if (canUpdatePreview)
        {
            // ���� �ռ� ������ ���� �ʱ�ȭ
            ResetMergePreview();

            // �ռ� ���� ���� Ȯ�� �� ������ ǥ��
            if (CanMergeWithTarget(tileData))
            {
                ShowMergePreview(tileData);
            }
            else
            {
                // �Ϲ� �̵� - ��Ȯ�� ���콺 ��ġ�� ��ġ
                Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
                newPosition.z = -0.1f;
                transform.position = newPosition;
                SetPreviewMaterial(canPlace);
            }

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

    // �巡�� ����
    public void OnPointerUp(PointerEventData eventData)
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
        else if(hasDragged && previousTilePosition != originalTilePosition)
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
            DestroyPreviewUnit();
            ReturnToOriginalPosition();

            CheckAttackAvailability();
        }
    }

    private void DeleteUnit()
    {
        // ���� Ÿ�Ͽ��� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // ���� �Ŵ������� ��� ����
        UnitManager.Instance.UnregisterUnit(this);
        EnemyManager.Instance.UpdateEnemiesPath();

        //�ڽ�Ʈ ����
        int refundCost = 0;

        if(unitType == UnitType.Base)
        {
            refundCost = -1;
        }
        else
        {
            int starLevel = (int)GetStat(StatName.UnitStarLevel);

            refundCost = (starLevel == 1) ? 1 : starLevel - 1;
        }

        // costPerTick ���� �ٷ� �Ѱ� ������� ��ε�ĳ��Ʈ
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = refundCost, 
            multiply = 1f
        });

    }

    // ��ġ �������� Ȯ��
    private bool CheckPlacementPossibility(Vector2 targetPos)
    {
        // ���� Ÿ�� �Ͻ������� ����
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
                unitSprite.transform.DORewind(); // �ִϸ��̼� ����
            }

            isShowingMergePreview = false;
            mergeTargetTile = null;
        }
    }

    // �ռ� ���� ���� Ȯ��
    private bool CanMergeWithTarget(TileData tileData)
    {
        if (tileData?.placedUnit == null || tileData.placedUnit == this)
            return false;

        var targetUnit = tileData.placedUnit;

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

        // �ð��� ȿ�� (�� ���� ����)
        unitSprite.transform.DOKill();
        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
    }

    // ���� �̵�
    private void MoveUnit()
    {
        // ���� ���·� ����
        ResetMergePreview();

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

        // ������ ����
        DestroyPreviewUnit();

        // �� ��� ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();

        //���ݰ����� ��ġ���� üũ
        CheckAttackAvailability();
    }

    // �ռ� ����
    private void PerformMerge()
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

        // �� ��� ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();
    }

    // ���� ��ġ�� ���ư���
    private void ReturnToOriginalPosition()
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
            float baseValue = baseStats[stat.Key].value;

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



    public void SetPreviewMaterial(bool canPlace)
    {
        // ��ġ ������ ���� �Ұ����� ���� ���׸��� ����
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // �������� ���� Sorting Order�� ���� ����
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base�� �Ѵܰ� �Ʒ���
        }
    }

    private void SetDeleteMat()
    {
        if (unitSprite != null)
        {
            unitSprite.material = deleteMaterial;
            unitBaseSprite.material = deleteMaterial;
        }
    }

    // ������ ���� �� ���� ���׸���� ����
    public void DestroyPreviewUnit()
    {

        // ���� �������� ��ȯ �� ���� sorting order�� ����
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = unitSprite.sortingOrder - 1;

        unitSprite.material = originalMaterial;
        unitBaseSprite.material = originalMaterial;


    }


    public void ApplyEffect(float duration = 0.5f)
    {
        // ���� ���� ����
        UnityEngine.Color originalColor = unitSprite.color;
        UnityEngine.Color effectColor = UnityEngine.Color.yellow; // ������ �����

        // DOTween���� ���� ����
        unitSprite.DOColor(effectColor, duration * 0.5f)
            .OnComplete(() =>
            {
                unitSprite.DOColor(originalColor, duration * 0.5f);
            });

        unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

    }

}
