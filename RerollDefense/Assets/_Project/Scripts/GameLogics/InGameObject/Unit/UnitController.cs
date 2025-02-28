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

    public List<GameObject> starObjects = new List<GameObject>();  // ������ ������ �����ϱ� ���� ����Ʈ

    public UnitType unitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;


    //inspector�� �Ҵ�
    [SerializeField] private Material enabledMaterial;   // ��ġ ������ �� ���
    [SerializeField] private Material disabledMaterial; // ��ġ �Ұ����� �� ���
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

    public D_UnitData unitData;
    private UnitController mergedPreviewUnit = null; // �ռ� �̸����� ����
    private TileData targetTileData = null; // �ռ� ��� Ÿ�� ������
    private Dictionary<Vector2, TileData> mergeTargets = new Dictionary<Vector2, TileData>(); // �ռ� Ÿ�� ����

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

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // Ÿ�̸� ����
        }
    }

    // �巡�� ���� �� ȣ��
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!isActive) return;

        isDragging = true;
        hasDragged = false; // �巡�� ���� �� �ʱ�ȭ
        originalPosition = transform.position;
        originalTilePosition = tilePosition;

        mergeTargets.Clear(); // �ռ� Ÿ�� �ʱ�ȭ

        // �巡�� �� ������ �ణ ���� �÷� �巡�� ������ �ð������� ǥ��
        Vector3 position = transform.position;
        position.z = -1;
        transform.position = position;

        // �巡�� �� Sorting Order �����Ͽ� �ٸ� ���� ���� ǥ�õǵ���
        unitSprite.sortingOrder = 100;
        unitBaseSprite.sortingOrder = 99;

        // ��ġ ���� ���� �ʱ�ȭ
        canPlace = false;
    }

    // �巡�� �� ȣ��
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true; // �巡�� �߻� ǥ��

        // ���콺 ��ġ���� Ÿ�� ��ġ ���
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 currentTilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (currentTilePos != previousTilePosition)
        {

            // Ÿ�ϸ� ���� ������Ʈ
            TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0.1f));

            // �ռ� �̸����� ������ �ִٸ� ����
            ClearMergedPreview();

            // ��ġ ���� ���� Ȯ��
            List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
            Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>()
            {
                { 0, this }
            };

            // ��ġ ���ɼ� Ȯ�� (���� ��ġ ���)
            canPlace = CheckPlacementPossibility(currentTilePos, tileOffsets, currentPreviews);

            // Ÿ�� Ÿ�� ������ ȹ��
            targetTileData = TileMapManager.Instance.GetTileData(currentTilePos);

            // �ռ� ���� ���� Ȯ�� �� ó��
            bool isMergePreview = CheckAndCreateMergePreview(currentTilePos);

            if (!isMergePreview)
            {
                // �Ϲ� �̵� - Ÿ�� ��ġ�� �°� ���� �̵�
                Vector3 newPosition = TileMapManager.Instance.GetTileToWorldPosition(currentTilePos);
                newPosition.z = -0.1f; // �ణ �տ� ǥ��
                transform.position = newPosition;

                // ���׸��� ������Ʈ
                SetPreviewMaterial(canPlace);

                // ������ ���̰� ����
                unitSprite.enabled = true;
                unitBaseSprite.enabled = true;
            }

            // ���� ��ġ ������Ʈ
            previousTilePosition = currentTilePos;
        }
    }

    // �ռ� Ÿ���� �� ���� ����
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

    // �巡�� ���� �� ȣ��
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Ÿ�� ���� ����
        TileMapManager.Instance.SetAllTilesColor(new UnityEngine.Color(1, 1, 1, 0));

        // ��ġ �� ���׸��� ����
        DestroyPreviewUnit();

        // �巡�� �� ��ġ ó��
        if (hasDragged)
        {
            // ���� ��ġ�� �ٸ� ���� ��ġ�Ϸ��� ��츸 ó��
            if (previousTilePosition != originalTilePosition)
            {
                // �ռ� �̸����Ⱑ �ִ� ���
                if (mergedPreviewUnit != null)
                {
                    // �ռ� ó��
                    PerformMerge();
                    return;
                }

                if (canPlace)
                {
                    // �Ϲ� �̵� ó��
                    // ���� Ÿ�Ͽ��� ���� ����
                    TileMapManager.Instance.ReleaseTile(originalTilePosition);

                    // �� Ÿ�� ����
                    List<Vector2> tileOffsets = new List<Vector2>() { Vector2.zero };
                    Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>()
                    {
                        { 0, this }
                    };
                    TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);

                    // ���� ��ġ ���� ������Ʈ
                    tilePosition = previousTilePosition;
                    Vector3 finalPosition = TileMapManager.Instance.GetTileToWorldPosition(previousTilePosition);
                    finalPosition.z = 0;
                    transform.position = finalPosition;

                    // ������ ���� ���� (�׻� Ȱ��ȭ)
                    unitSprite.enabled = true;
                    unitBaseSprite.enabled = true;

                    // �� ��� ������Ʈ
                    EnemyManager.Instance.UpdateEnemiesPath();
                    return;
                }
            }
        }

        // ��ġ ���� �Ǵ� ���� ��ġ�� ���ư��� ���
        ReturnToOriginalPosition();
    }

    // �ռ� ���� ���� Ȯ�� �� ������ ����
    private bool CheckAndCreateMergePreview(Vector2 tilePos)
    {
        // Ÿ�Ͽ� ������ �ְ� �ռ��� ������ ���
        if (targetTileData?.placedUnit != null &&
            targetTileData.placedUnit != this &&
            unitType == targetTileData.placedUnit.unitType &&
            GetStat(StatName.UnitStarLevel) == targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
            targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) < 3)
        {
            // �ռ� Ÿ�� ���
            mergeTargets[tilePos] = targetTileData;

            // ���� ������ �� ��Ȱ��ȭ
            foreach (var star in targetTileData.placedUnit.starObjects)
            {
                star.SetActive(false);
            }

            // ���� ������ �������� ����� GameObject�� Ȱ��ȭ ����
            unitSprite.enabled = false;
            unitBaseSprite.enabled = false;

            // ���ο� �ռ� ���� ���� �� ����
            Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
            GameObject mergedPreviewObj = PoolingManager.Instance.GetObject(unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);

            // ���� �̸����Ⱑ �־��ٸ� ���� ����
            if (mergedPreviewUnit != null)
            {
                PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
            }

            mergedPreviewUnit = mergedPreviewObj.GetComponent<UnitController>();
            mergedPreviewUnit.Initialize();
            mergedPreviewUnit.InitializeUnitInfo(unitData);

            // �� ��Ÿ ���� ����
            int newStarLevel = (int)targetTileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
            mergedPreviewUnit.UpGradeUnitLevel(newStarLevel);

            // �̸����� ���׸��� ����
            mergedPreviewUnit.SetPreviewMaterial(canPlace);

            // �ð��� ȿ��
            mergedPreviewUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);

            return true;
        }

        return false;
    }

    // �ռ� �̸����� ����
    private void ClearMergedPreview()
    {
        if (mergedPreviewUnit != null)
        {
            // �ռ� �̸����� ���� ��ȯ
            PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
            mergedPreviewUnit = null;
        }

        // ���� ���� ������ �ٽ� ǥ��
        unitSprite.enabled = true;
        unitBaseSprite.enabled = true;
    }

    // ���� �ռ� ����
    private void PerformMerge()
    {
        if (mergedPreviewUnit == null || targetTileData?.placedUnit == null) return;

        // ���� Ÿ�Ͽ��� ���� ���� ����
        TileMapManager.Instance.ReleaseTile(originalTilePosition);

        // �ռ� ��� ���� ��������
        UnitController targetUnit = targetTileData.placedUnit;

        // �� ��Ÿ ���� ����
        int newStarLevel = (int)GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // �ռ� ȿ�� ����
        targetUnit.ApplyEffect(1.0f);

        // �ռ� ������ ���� ��ȯ
        PoolingManager.Instance.ReturnObject(mergedPreviewUnit.gameObject);
        mergedPreviewUnit = null;

        // ���� ����(�巡���� ����) ����
        UnitManager.Instance.UnregisterUnit(this);
        Destroy(gameObject);

        // �� ���� ����
        RestoreMergeTileStars();

        // �� ��� ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();
    }

    // ��ġ ���ɼ� Ȯ���� ���� ���� �޼��� (���� ��ġ ó���� ����)
    private bool CheckPlacementPossibility(Vector2 targetPos, List<Vector2> offsets, Dictionary<int, UnitController> units)
    {
        // ���� Ÿ�� ������ �ӽ� ����
        TileData originalTileData = TileMapManager.Instance.GetTileData(originalTilePosition);
        bool originalAvailable = false;

        if (originalTileData != null)
        {
            // ���� ��ġ�� �Ͻ������� ��� ������ ������ ǥ��
            originalAvailable = originalTileData.isAvailable;
            originalTileData.isAvailable = true;
            originalTileData.placedUnit = null;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        // ��ġ ���ɼ� Ȯ��
        bool canPlace = TileMapManager.Instance.CanPlaceObject(targetPos, offsets, units);

        // ���� Ÿ�� ���� ����
        if (originalTileData != null)
        {
            originalTileData.isAvailable = originalAvailable;
            originalTileData.placedUnit = this;
            TileMapManager.Instance.SetTileData(originalTileData);
        }

        return canPlace;
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
