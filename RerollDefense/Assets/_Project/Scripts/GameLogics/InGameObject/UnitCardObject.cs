using BGDatabaseEnum;
using CatDarkGame.PerObjectRTRenderForUGUI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ���� Ÿ�� UI ��Ҹ� �����ϰ� �巡�� �� ������� �ʿ� ������ ��ġ�ϴ� Ŭ����
public class UnitCardObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private GameObject tileImageLayout; // TileImage_Layout ���ӿ�����Ʈ
    [SerializeField] private GameObject tileImagePrefab; // Tile_Image ������

    //[SerializeField] private GameObject unitImageObject;
    [SerializeField] private PerObjectRTRenderer unitRTObject;
    [SerializeField] private GameObject unitTraitObject;
    [SerializeField] private GameObject costImageObject;
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;

    //������ ���ֵ��� ����, �ռ� ���� �Ǵ�, �巡�� ���� ���� ���� �� ������Ʈ

    //TileData�� ������ ����
    private Dictionary<int, UnitController> originalPreviews = new Dictionary<int, UnitController>();

    //���� ȭ�鿡 �������� ������ ����, �ռ� ���ο� ���� ���� ������ ��ü, ��ġ ����, ���͸��� ������Ʈ
    private Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>();

    // Ȯ�� Ÿ�� �����並 ������ Dictionary �߰�
    private Dictionary<Vector2, TileExtensionObject> extensionPreviews = new Dictionary<Vector2, TileExtensionObject>();


    // ������ (0,0)���κ����� �����°���(����Ÿ���� ����)
    private List<Vector2> tileOffsets;
    private Vector2 previousTilePosition;
    private string tileCardName;
    private int cardCost;
    private bool isDragging = false;
    private bool hasDragged = false;  // ���� �巡�� ���θ� üũ�ϴ� ���� �߰�
    private bool canPlace = false;
    private PerObjectRTSource rtSource;

    // Ȱ��ȭ�Ǿ��� �̹����� �ε����� ������ ����Ʈ �߰�
    private List<int> activeImageIndices = new List<int>();

    // ī�尡 ���Ǿ��� �� �߻��ϴ� �̺�Ʈ (���� ī�嵦�� �Ű������� ����)
    public static event System.Action<GameObject> OnCardUsed;


    // ī�� �ʱ�ȭ: Ÿ�� ����, �ڽ�Ʈ, ������ ����
    public void InitializeCardInform(D_TileCardData tileCardData)
    {
        CleanUp();

        tileCardName = tileCardData.f_name;
        cardCost = tileCardData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        UpdateCostTextColor();
        CreateTilePreview(tileCardData);

        //cost ��� �̺�Ʈ ���� -> cost �߰��ɶ� tile Cost Text ������Ʈ
        GameManager.Instance.OnCostAdd += OnCostChanged;

        StageManager.Instance.OnWaveFinish += CancelDragging;

        rtSource = CreatePreviewInstances(new Vector3(3000f, 0f)).GetComponent<PerObjectRTSource>();

        if(rtSource != null && unitRTObject != null)
        {
            unitRTObject.source = rtSource;
            rtSource.CalculateAutoBounds();

        }

    }

    // Ÿ�� ��� �����͸� ������� ������ ��ġ �ʱ�ȭ
    private void InitializeTileShape()
    {
        var tileCardData = D_TileCardData.FindEntity(data => data.f_name == tileCardName);

        if (tileCardData != null)
        {
            tileOffsets = new List<Vector2>();

            foreach(var tile in tileCardData.f_unitBuildData)
            {
                //���� ��ǥ
                Vector2 originalPos = tile.f_TilePosData.f_TilePos;

                //2D ��ǥ��� �Ʒ������� ������ Y�� �����ϹǷ� Y�� ����
                Vector2 correctedPos = new Vector2(originalPos.x, -originalPos.y);

                tileOffsets.Add(correctedPos);
            }
        }
    }

    //unitCard �巡�������� �Ⱥ����ߵ�
    private void SetUIVisibility(bool visible)
    {
        foreach (int index in activeImageIndices)
        {
            if (index < tileImages.Count && tileImages[index] != null)
            {
                tileImages[index].gameObject.SetActive(visible);
            }
        }

        cardCostText.gameObject.SetActive(visible);
        unitRTObject.gameObject.SetActive(visible);
        unitTraitObject.SetActive(visible);
        costImageObject.SetActive(visible);
    }

    // �ڽ�Ʈ ����� ȣ��� �޼���
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    //cost �� -> ���� ���� : �Ͼ��, �Ұ��� : ������
    public void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.GetSystemStat(StatName.Cost) >= cardCost ? Color.white : Color.red;
    }

    // �巡�� ����: �ڽ�Ʈ üũ �� ������ �ν��Ͻ� ����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < cardCost) return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;  // �巡�� ���� �� false�� �ʱ�ȭ

        GameManager.Instance.PrepareUseCost(cardCost);

        SetUIVisibility(false);  // UI �����

        // ���콺 Ŀ���� ���� ��ǥ ���
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.2f; // ������ ����
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        CreatePreviewInstances(tilePos);
        UpdatePreviewInstancesPosition(tilePos);
    }

    // �巡�� ��: ������ ��ġ ������Ʈ �� ��ġ ���� ���� ǥ��
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        hasDragged = true;
        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;
        worldPos.y += 0.1f; // ������ ����
        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));

            // ��ƼŸ�� ���� Ȯ��
            UnitController multiTileUnit = null;
            foreach (var pair in originalPreviews)
            {
                if (pair.Value.isMultiUnit)
                {
                    multiTileUnit = pair.Value;
                    break;
                }
            }

            // ��ƼŸ�� ������ ���
            if (multiTileUnit != null)
            {
                MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;
                if (multiController != null)
                {
                    // ��ƼŸ�� ���� �ռ� ���� ���� Ȯ��
                    TileData tileData = TileMapManager.Instance.GetTileData(tilePos);
                    if (tileData?.placedUnit != null && tileData.placedUnit is MultiTileUnitController)
                    {
                        MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;

                        // �ռ� ���� ���� Ȯ�� ����
                        bool canMerge = CheckMultiTileMergePossibility(multiController, targetMultiUnit, tilePos);

                        if (canMerge)
                        {
                            // �ռ� ������ ǥ��
                            ShowMultiTileMergePreview(multiController, targetMultiUnit);
                            canPlace = true;
                            previousTilePosition = tilePos;
                            return;
                        }
                    }
                }

                // �ռ��� �Ұ����� ��� �Ϲ� ��ġ ���� ���� Ȯ��
                canPlace = CheckMultiTilePlacement(tilePos, multiTileUnit);
            }
            else
            {
                // �Ϲ� ���� ó�� (���� �ڵ�)
                canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, originalPreviews);
            }

            UpdatePreviewInstancesPosition(tilePos);
            previousTilePosition = tilePos;
        }
    }


    // ���� ������ ��ġ ���� ���� Ȯ�� �޼��� �߰�
    private bool CheckMultiTilePlacement(Vector2 basePosition, UnitController multiTileUnit)
    {
        // tileOffsets ���
        foreach (var offset in tileOffsets)
        {
            Vector2 tilePos = basePosition + offset;
            TileData tileData = TileMapManager.Instance.GetTileData(tilePos);

            // Ÿ���� ���ų� ��� �Ұ����� ���
            if (tileData == null || !tileData.isAvailable)
            {
                // �̹� �ڽ��� ������ Ÿ���� �ƴ϶�� ��ġ �Ұ�
                if (tileData?.placedUnit != multiTileUnit)
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool CheckMultiTileMergePossibility(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit, Vector2 basePosition)
    {
        // ���� Ÿ�� �� ���� Ȯ��
        if (previewUnit.unitType != targetUnit.unitType ||
            previewUnit.GetStat(StatName.UnitStarLevel) != targetUnit.GetStat(StatName.UnitStarLevel) ||
            targetUnit.GetStat(StatName.UnitStarLevel) >= 5)
        {
            return false;
        }

        // ��� Ÿ���� ��ġ���� Ȯ��
        HashSet<Vector2> previewTilePositions = new HashSet<Vector2>();
        HashSet<Vector2> targetTilePositions = new HashSet<Vector2>();

        // ������ ������ ��� Ÿ�� ��ġ ���
        foreach (var offset in previewUnit.multiTilesOffset)
        {
            previewTilePositions.Add(basePosition + offset);
        }

        // Ÿ�� ������ ��� Ÿ�� ��ġ ���
        foreach (var offset in targetUnit.multiTilesOffset)
        {
            targetTilePositions.Add(targetUnit.tilePosition + offset);
        }

        // �� ������ ������ ������ Ȯ��
        return previewTilePositions.SetEquals(targetTilePositions);
    }

    private void ShowMultiTileMergePreview(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit)
    {
        // Ÿ�� ������ �� ��Ȱ��ȭ
        foreach (var star in targetUnit.starObjects)
        {
            star.SetActive(false);
        }

        // ������ ���� ���� ���׷��̵�
        int currentLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel);
        int newLevel = currentLevel + 1;
        previewUnit.UpdateStarDisplay(newLevel);

        // ������ ���� ��ġ ����
        Vector3 targetPosition = TileMapManager.Instance.GetTileToWorldPosition(targetUnit.tilePosition);
        targetPosition.z = -0.1f;
        previewUnit.transform.position = targetPosition;

        // Ȯ�� Ÿ�� ��ġ ������Ʈ - �̺�Ʈ ���� ȣ�� ��� UpdateExtensionObjects �޼��� ���
        // ����: previewUnit.OnPositionChanged?.Invoke(targetPosition);
        previewUnit.UpdateExtensionObjects();

        // ������ ���׸��� ����
        previewUnit.SetPreviewMaterial(true);

        // �ð��� ȿ��
        //previewUnit.unitSprite.transform.DOKill();
        //previewUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
    }

    private void PerformMultiTileMerge(MultiTileUnitController previewUnit, MultiTileUnitController targetUnit)
    {
        // Ÿ�� ���� ���׷��̵�
        int newStarLevel = (int)previewUnit.GetStat(StatName.UnitStarLevel) + 1;
        targetUnit.UpGradeUnitLevel(newStarLevel);

        // ȿ�� ����
        targetUnit.ApplyEffect(1.0f);

        // �ڽ�Ʈ ���
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1,
            multiply = 1f
        });

        // ���� ī�� ����
        OnCardUsed?.Invoke(transform.parent.gameObject);

        // ������ ��ü ����
        foreach (var instance in currentPreviews.Values)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }

        foreach (var extObj in extensionPreviews.Values)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();

        Destroy(gameObject);
    }

    // �巡�� ����: ��ġ ���� ���� Ȯ�� �� ���� ��ġ �Ǵ� ���
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //��ġ Ÿ�� ����ȭ
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        // ��ƼŸ�� ���� Ȯ��
        UnitController multiTileUnit = null;
        foreach (var pair in originalPreviews)
        {
            if (pair.Value.isMultiUnit)
            {
                multiTileUnit = pair.Value;
                break;
            }
        }

        if (multiTileUnit != null)
        {
            MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;
            if (multiController != null)
            {
                // �ռ� ���� ���� Ȯ��
                TileData tileData = TileMapManager.Instance.GetTileData(previousTilePosition);
                if (tileData?.placedUnit != null && tileData.placedUnit is MultiTileUnitController)
                {
                    MultiTileUnitController targetMultiUnit = tileData.placedUnit as MultiTileUnitController;

                    if (CheckMultiTileMergePossibility(multiController, targetMultiUnit, previousTilePosition))
                    {
                        // �ռ� ����
                        PerformMultiTileMerge(multiController, targetMultiUnit);
                        isDragging = false;
                        return;
                    }
                }
            }

            // �Ϲ� ��ġ ó��
            canPlace = CheckMultiTilePlacement(previousTilePosition, multiTileUnit);
        }
        else
        {
            canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, originalPreviews);
        }

        if (hasDragged && canPlace)
        {
            PlaceUnits();
        }
        else
        {
            SetUIVisibility(true);  // ��ġ ���н� UI �ٽ� ���̰�
            UnitInstanceViewOut();
        }

        isDragging = false;

        GameManager.Instance.CanclePrepareUseCost();
    }

    // ������ �ν��Ͻ� ����: �� Ÿ�� ��ġ�� ���� ������ ���� ����
    private UnitController CreatePreviewInstances(Vector3 tilePos)
    {
        var tileCardData = D_TileCardData.GetEntity(tileCardName);
        bool isMultiUnit = tileCardData.f_isMultiTileUinit;

        // ���� ���� ã�� (TilePos 0,0)
        D_unitBuildData baseUnitBuildData = null;
        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            if (buildData.f_TilePosData.f_TilePos == Vector2.zero)
            {
                baseUnitBuildData = buildData;
                break;
            }
        }

        if (isMultiUnit && baseUnitBuildData != null)
        {
            // ��Ƽ ���� ���� (���� ����)
            var unitPoolingKey = baseUnitBuildData.f_unitData.f_UnitPoolingKey;
            Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);

            GameObject previewInstance = PoolingManager.Instance.GetObject(
                unitPoolingKey.f_PoolObjectAddressableKey,
                worldPos,
                (int)ObjectLayer.Player
            );

            // UnitController�� MultiTileUnitController�� ��ü
            UnitController oldController = previewInstance.GetComponent<UnitController>();
            MultiTileUnitController multiController = null;

            if (oldController != null && !(oldController is MultiTileUnitController))
            {
                // TODO : ��ƼŸ�Ͽ� ���ֿ�����Ʈ ����� �̺κ� �� ����!!!!!!! ������ ���� �����տ� UnitController�� ����־ �̷��� �Ѱ���
                // ���� ������Ʈ�� �߿� ������ ����
                //SpriteRenderer unitSprite = oldController.unitSprite;
                //SpriteRenderer unitBaseSprite = oldController.unitBaseSprite;
                GameObject unitStarObject = oldController.unitStarObject;
                Material enabledMaterial = oldController.enabledMaterial;
                Material disabledMaterial = oldController.disabledMaterial;
                Material deleteMaterial = oldController.deleteMaterial;
                Material originalMaterial = oldController.originalMaterial;
                LayerMask unitLayer = oldController.unitLayer;

                // �� MultiTileUnitController �߰�
                multiController = previewInstance.AddComponent<MultiTileUnitController>();

                // ���� ������ ����
                //multiController.unitSprite = unitSprite;
                //multiController.unitBaseSprite = unitBaseSprite;
                multiController.unitStarObject = unitStarObject;
                multiController.enabledMaterial = enabledMaterial;
                multiController.disabledMaterial = disabledMaterial;
                multiController.deleteMaterial = deleteMaterial;
                multiController.originalMaterial = originalMaterial;
                multiController.unitLayer = unitLayer;

                // ���� ������Ʈ ���� (Initialize ���� �ؾ� ��)
                Destroy(oldController);

                // �ʱ�ȭ �� UnitData ����
                multiController.Initialize();
                multiController.InitializeUnitInfo(baseUnitBuildData.f_unitData, tileCardData);

                // ���� Ÿ�� ���� ����
                multiController.multiTilesOffset.Clear();
                foreach (var offset in tileOffsets)
                {
                    multiController.multiTilesOffset.Add(offset);
                }

                originalPreviews[0] = multiController;
                currentPreviews[0] = multiController;
            }
            else if (oldController is MultiTileUnitController)
            {
                // �̹� MultiTileUnitController�� ���
                multiController = (MultiTileUnitController)oldController;
                originalPreviews[0] = multiController;
                currentPreviews[0] = multiController;
            }

            // �ٸ� Ÿ�� ��ġ�� Ȯ�� ������Ʈ ����
            if (multiController != null)
            {
                foreach (var offset in tileOffsets)
                {
                    if (offset != Vector2.zero)
                    {
                        Vector2 extTilePos = (Vector2)tilePos + offset;
                        Vector3 extWorldPos = TileMapManager.Instance.GetTileToWorldPosition(extTilePos);

                        GameObject extPreview = PoolingManager.Instance.GetObject(
                            "TileExtensionObject",
                            extWorldPos,
                            (int)ObjectLayer.Player
                        );

                        TileExtensionObject extObject = extPreview.GetComponent<TileExtensionObject>();
                        if (extObject != null)
                        {
                            // ����� Initialize �޼��� ȣ��
                            extObject.Initialize(multiController, offset);
                            extensionPreviews[offset] = extObject;

                            // ��ƼŸ�� ��Ʈ�ѷ��� Ȯ�� ������Ʈ �߰�
                            multiController.extensionObjects.Add(extObject);
                        }
                    }
                }
            }
        }
        else
        {
            // ���� �Ϲ� ���� ���� ����
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                var unitBuildData = tileCardData.f_unitBuildData[i];
                var unitPoolingKey = unitBuildData.f_unitData.f_UnitPoolingKey;

                // ���� ��ġ ���
                Vector2 offsetPos = (Vector2)tilePos + tileOffsets[i];
                Vector3 unitPos = TileMapManager.Instance.GetTileToWorldPosition(offsetPos);

                GameObject previewInstance = PoolingManager.Instance.GetObject(
                    unitPoolingKey.f_PoolObjectAddressableKey,
                    unitPos,
                    (int)ObjectLayer.Player
                );

                UnitController previewUnit = previewInstance.GetComponent<UnitController>();
                if (previewUnit != null)
                {
                    previewUnit.Initialize();
                    previewUnit.InitializeUnitInfo(unitBuildData.f_unitData);

                    originalPreviews[i] = previewUnit;
                    currentPreviews[i] = previewUnit;
                }
            }
        }

        return originalPreviews[0];
    }

    // ������ ��ġ ������Ʈ: ���� ���콺 ��ġ�� ���� ������ ��ġ ����, ��ġ�Ұ��� ���� ���׸��� ����
    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {
        // ���� �������� Ȯ��
        UnitController multiTileUnit = null;
        foreach (var pair in originalPreviews)
        {
            if (pair.Value.isMultiUnit)
            {
                multiTileUnit = pair.Value;
                break;
            }
        }

        if (multiTileUnit != null)
        {
            // ���⿡ ���� ���� �߰�
            MultiTileUnitController multiController = multiTileUnit as MultiTileUnitController;

            // ������ �ռ� �����䰡 Ȱ��ȭ�Ǿ��� ��� �� ǥ�� ������� ����
            if (previousTilePosition != basePosition)
            {
                TileData previousTileData = TileMapManager.Instance.GetTileData(previousTilePosition);
                if (previousTileData?.placedUnit != null && previousTileData.placedUnit is MultiTileUnitController)
                {
                    MultiTileUnitController previousTarget = previousTileData.placedUnit as MultiTileUnitController;

                    // Ÿ�� ������ �� �ٽ� Ȱ��ȭ
                    foreach (var star in previousTarget.starObjects)
                    {
                        star.SetActive(true);
                    }

                    // ���� ������ ������ �� ���� ������� ����
                    int originalLevel = (int)multiController.GetStat(StatName.UnitStarLevel);
                    multiController.UpdateStarDisplay(originalLevel);
                }
            }

            // ���� ���� ó��
            multiTileUnit.gameObject.SetActive(true);
            multiTileUnit.transform.position = TileMapManager.Instance.GetTileToWorldPosition(basePosition);
            multiTileUnit.SetPreviewMaterial(canPlace);

            // Ȯ�� ������Ʈ ��ġ �� ���� ������Ʈ
            foreach (var offset in tileOffsets)
            {
                if (offset != Vector2.zero && extensionPreviews.ContainsKey(offset))
                {
                    TileExtensionObject extObj = extensionPreviews[offset];
                    if (extObj != null)
                    {
                        // ��ġ ������Ʈ
                        Vector2 extTilePos = basePosition + offset;
                        Vector3 extWorldPos = TileMapManager.Instance.GetTileToWorldPosition(extTilePos);
                        extObj.transform.position = extWorldPos;
                        //extObj.tileSprite.material = multiTileUnit.unitSprite.material;
                        extObj.tileSprite.sortingOrder = 100;
                    }
                }
            }
        }
        else
        {
            // �Ϲ� ���� ó�� - ���� �ڵ�
            // ���⼭ Dictionary Ű Ȯ�� ���� �߰� �ʿ�
            // �ռ��� ������ �ִٸ� ���� ���·� ����
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                // Ű ���� ���� Ȯ�� �߰�
                if (currentPreviews.ContainsKey(i) && originalPreviews.ContainsKey(i) &&
                    currentPreviews[i] != originalPreviews[i])
                {
                    // �ش� ��ġ�� ���� ��ġ�� ������ �� �ٽ� Ȱ��ȭ
                    Vector2 position = previousTilePosition + tileOffsets[i];
                    var tileData = TileMapManager.Instance.GetTileData(position);

                    if (tileData?.placedUnit != null)
                    {
                        foreach (var star in tileData.placedUnit.starObjects)
                        {
                            star.SetActive(true);
                        }
                    }

                    PoolingManager.Instance.ReturnObject(currentPreviews[i].gameObject);
                    currentPreviews[i] = originalPreviews[i];
                    currentPreviews[i].gameObject.SetActive(true);
                }
            }

            // ������ �Ϲ� ���� ó�� �ڵ� - ���⵵ Ű Ȯ�� ���� �߰�
            for (int i = 0; i < tileOffsets.Count; i++)
            {
                // Ű ���� ���� Ȯ�� �߰�
                if (!currentPreviews.ContainsKey(i) || !originalPreviews.ContainsKey(i))
                    continue;

                Vector2 previewPosition = basePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(previewPosition);
                var currentPreview = currentPreviews[i];

                // �ռ� ����
                if (tileData?.placedUnit != null &&
                    currentPreview.unitType == tileData.placedUnit.unitType &&
                    currentPreview.GetStat(StatName.UnitStarLevel) == tileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
                    tileData.placedUnit.GetStat(StatName.UnitStarLevel) < 5)
                {
                    // �ռ� ���� �ڵ�...
                    foreach (var star in tileData.placedUnit.starObjects)
                    {
                        star.SetActive(false);
                    }

                    Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                    currentPreview.gameObject.SetActive(false);

                    GameObject mergedPreview = PoolingManager.Instance.GetObject(currentPreview.unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);
                    var mergedUnit = mergedPreview.GetComponent<UnitController>();
                    mergedUnit.Initialize();
                    mergedUnit.InitializeUnitInfo(currentPreview.unitData);

                    int newStarLevel = (int)tileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
                    mergedUnit.UpdateStarDisplay(newStarLevel);  // ���⿡�� UpGradeUnitLevel ��� UpdateStarDisplay ���

                    mergedUnit.SetPreviewMaterial(canPlace);
                    //mergedUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
                    currentPreviews[i] = mergedUnit;
                }
                else
                {
                    // �Ϲ� �̵�
                    currentPreview.gameObject.SetActive(true);
                    currentPreview.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                    currentPreview.SetPreviewMaterial(canPlace);
                }
            }
        }
    }
    // ���� ��ġ: �����並 ���� �������� ��ȯ�ϰ� ���� ���� ������Ʈ
    private void PlaceUnits()
    {
        // ��ƼŸ�� ���� Ȯ��
        bool isMultiUnit = false;
        MultiTileUnitController multiTileUnit = null;

        foreach (var pair in currentPreviews)
        {
            multiTileUnit = pair.Value as MultiTileUnitController;
            if (multiTileUnit != null)
            {
                isMultiUnit = true;
                break;
            }
        }

        // ���� �ռ��� �Ͼ ��ġ�� ���� ���ֵ鸸 ����
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            // ���� �����䰡 ������ �ٸ��ٸ� �ռ��� �Ͼ ��ġ
            if (currentPreviews.ContainsKey(i) && originalPreviews.ContainsKey(i) &&
                currentPreviews[i] != originalPreviews[i])
            {
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);

                if (tileData?.placedUnit != null)
                {
                    UnitManager.Instance.UnregisterUnit(tileData.placedUnit);
                }
            }
        }

        // ��ƼŸ�� ���� ��ġ
        if (isMultiUnit && multiTileUnit != null)
        {
            multiTileUnit.DestroyPreviewUnit();
            multiTileUnit.tilePosition = previousTilePosition;
            UnitManager.Instance.RegisterUnit(multiTileUnit);

            // ���� ������ �����ϴ� ��� Ÿ�� ����
            foreach (var offset in tileOffsets)
            {
                Vector2 tilePos = previousTilePosition + offset;
                TileData tileData = TileMapManager.Instance.GetTileData(tilePos);

                if (tileData != null)
                {
                    tileData.isAvailable = false;
                    tileData.placedUnit = multiTileUnit; // ��� Ÿ���� ���� ���� ����
                    TileMapManager.Instance.SetTileData(tileData);

                    // ���� ���� ��ġ(0,0)�� �ƴ� ��쿡�� ���� TileExtensionObject ��ġ
                    if (offset != Vector2.zero)
                    {
                        // ������ ������Ʈ ����
                        if (extensionPreviews.ContainsKey(offset))
                        {
                            PoolingManager.Instance.ReturnObject(extensionPreviews[offset].gameObject);
                        }

                        // ���� Ȯ�� ������Ʈ ����
                        Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
                        GameObject extensionObject = PoolingManager.Instance.GetObject(
                            "TileExtensionObject",
                            worldPos,
                            (int)ObjectLayer.Player
                        );

                        // Extension ������Ʈ �ʱ�ȭ
                        TileExtensionObject extension = extensionObject.GetComponent<TileExtensionObject>();
                        if (extension != null)
                        {
                            // Ȯ�� ������Ʈ�� ���� ���� ����
                            extension.Initialize(multiTileUnit, offset);

                            // MultiTileUnitController���� Ȯ�� ������Ʈ �߰�
                            multiTileUnit.extensionObjects.Add(extension);
                        }
                    }
                }
            }
        }
        else
        {
            // ���� ���� Ÿ�� ����
            foreach (var pair in currentPreviews)
            {
                var unitInstance = pair.Value;
                unitInstance.DestroyPreviewUnit();
                UnitManager.Instance.RegisterUnit(unitInstance);
            }



            TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);
        }

        // �ڽ�Ʈ ���
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1, // ������ �Ҹ�
            multiply = 1f
        });

        // enemy path ������Ʈ
        //EnemyManager.Instance.UpdateEnemiesPath();

        // ���� ī�� ���� -> �̺�Ʈ ���ؼ�
        OnCardUsed?.Invoke(transform.parent.gameObject);

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();
        Destroy(gameObject);
    }
    private void UnitInstanceViewOut()
    {
        foreach (var instance in currentPreviews.Values)
        {
            instance.transform.position = new Vector3(3000f, 0f);
        }
    }

    // ��ġ ��ҽ� ȣ�� �޼���: ������ �ν��Ͻ� ����
    private void CancelPlacement()
    {
        // ���� ������ ��ȯ �ڵ� ����
        foreach (var instance in currentPreviews.Values)
        {
            instance.transform.position = new Vector3(3000f, 0f);
        }

        // Ȯ�� ������Ʈ �����䵵 ��ȯ
        foreach (var extObj in extensionPreviews.Values)
        {
            if (extObj != null)
            {
                PoolingManager.Instance.ReturnObject(extObj.gameObject);
            }
        }

        originalPreviews.Clear();
        currentPreviews.Clear();
        extensionPreviews.Clear();

        GameManager.Instance.OnCostAdd -= OnCostChanged;
    }

    // ī�� UI�� Ÿ�� ������ ����
    private void CreateTilePreview(D_TileCardData tileCardData)
    {
        // ���� Ÿ�� �̹��� ��� ����
        ClearTileImages();

        // Ÿ�� �����Ϳ��� �ּ�/�ִ� x, y ��ǥ ã��
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        // ���� ���� �����Ϳ��� ��ǥ ���� ã��
        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePosData.f_TilePos;
            minX = Mathf.Min(minX, (int)tilePos.x);
            maxX = Mathf.Max(maxX, (int)tilePos.x);
            minY = Mathf.Min(minY, (int)tilePos.y);
            maxY = Mathf.Max(maxY, (int)tilePos.y);
        }

        // �׸��� ũ�� ���
        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // tileImageLayout�� RectTransform
        RectTransform layoutRect = tileImageLayout.GetComponent<RectTransform>();

        // ��ü ���̾ƿ� ũ�� ���� (Ÿ�� ũ�� * �׸��� ũ�� + ����)
        float tileSize = 50f;
        float spacing = 1.5f; // 0.1 * tileSize
        layoutRect.sizeDelta = new Vector2(width * (tileSize + spacing) - spacing, height * (tileSize + spacing) - spacing);

        // ���� �����͸� �׸��� ��ġ�� ����
        Dictionary<Vector2Int, D_unitBuildData> unitPositionMap = new Dictionary<Vector2Int, D_unitBuildData>();

        foreach (var buildData in tileCardData.f_unitBuildData)
        {
            Vector2Int normalizedPos = new Vector2Int(
                (int)buildData.f_TilePosData.f_TilePos.x - minX,
                (int)buildData.f_TilePosData.f_TilePos.y - minY
            );
            unitPositionMap[normalizedPos] = buildData;
        }

        // ��ü �׸��带 ��ȸ�ϸ� �̹��� ���� (�� ĭ ����)
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Y�� ���� ���� (�Ʒ����� ���� �����ϴ� ��ǥ��)
                Vector2Int gridPos = new Vector2Int(x, height - 1 - y);

                // Ÿ�� ������ ����
                GameObject tileImageObj = Instantiate(tileImagePrefab, tileImageLayout.transform);

                // UnitTileObject ������Ʈ ��������
                UnitTileObject tileObject = tileImageObj.GetComponent<UnitTileObject>();

                if (tileObject != null)
                {
                    // ��ġ ����
                    float posX = (x - (width - 1) / 2f) * (tileSize + spacing);
                    float posY = ((height - 1) / 2f - y) * (tileSize + spacing);
                    tileObject.SetPosition(posX, posY);

                    // �̹��� ����Ʈ�� Ÿ�� �̹��� ������Ʈ �߰� (UnitTileObject�� unitImage ����)
                    Image tileImage = tileObject.backgroundImage;

                    if (tileImage != null)
                    {
                        tileImages.Add(tileImage);
                        activeImageIndices.Add(tileImages.Count - 1);
                    }

                    // �ش� ��ġ�� ���� �����Ͱ� �ִ��� Ȯ��
                    if (unitPositionMap.TryGetValue(gridPos, out var buildData))
                    {
                        // ������ null�̰ų� SkillAttackType�� None�̸� Base�� ó��
                        bool isBase = (buildData.f_unitData == null || buildData.f_unitData.f_SkillAttackType == SkillAttackType.None);

                        if (!isBase)
                        {
                            // ���� ��������Ʈ ��������
                            GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);
                            UnitController unitController = tempUnit.GetComponent<UnitController>();

                            //if (unitController != null && unitController.unitSprite != null)
                            //{
                            //    Sprite unitSprite = unitController.unitSprite.sprite;
                            //    tileObject.InitUnitImage(unitSprite, false);
                            //}

                            PoolingManager.Instance.ReturnObject(tempUnit);
                        }
                        else
                        {
                            // Base�� ǥ��
                            tileObject.InitUnitImage(null, true);
                        }
                    }
                    else
                    {
                        // �� Ÿ�� �ʱ�ȭ
                        tileObject.InitUnitImage(null, false);
                    }
                }
            }
        }
    }

    // ���� Ÿ�� �̹��� ��� �����ϴ� �޼���
    private void ClearTileImages()
    {
        activeImageIndices.Clear();

        // ����Ʈ�� �ִ� �̹��� ������Ʈ ����
        foreach (var image in tileImages)
        {
            if (image != null && image.gameObject != null)
            {
                Destroy(image.gameObject);
            }
        }

        // ����Ʈ �ʱ�ȭ
        tileImages.Clear();

        // Ȥ�� �����ִ� �ڽ� ������Ʈ�� ��� ����
        for (int i = tileImageLayout.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(tileImageLayout.transform.GetChild(i).gameObject);
        }
    }


    private void CancelDragging()
    {
        if (isDragging)
        {
            SetUIVisibility(true);
            UnitInstanceViewOut();
            isDragging = false;

            GameManager.Instance.CanclePrepareUseCost();
        }
    }

    private void CleanUp()
    {
        ClearTileImages(); // Ÿ�� �̹��� ����
        CancelPlacement();

        originalPreviews.Clear();
        currentPreviews.Clear();
        activeImageIndices.Clear();
        extensionPreviews.Clear();

        // �̺�Ʈ ���� ����
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;

    }


}