using BGDatabaseEnum;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ���� Ÿ�� UI ��Ҹ� �����ϰ� �巡�� �� ������� �ʿ� ������ ��ġ�ϴ� Ŭ����
public class UnitTileObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{


    //UI����� 9���� Ÿ�� �̹��� �����ϴ� ����Ʈ
    [SerializeField] private List<Image> tileImages = new List<Image>();

    [SerializeField] private TMP_Text cardCostText;

    // �巡�� �߿� ������ ������ ���ֵ��� ����
    private List<UnitController> previewInstances = new List<UnitController>();

    // ������ (0,0)���κ����� �����°���(����Ÿ���� ����)
    private List<Vector2> tileOffsets;
    private Vector2 previousTilePosition;
    private string tileShapeName;
    private int cardCost;
    private bool isDragging = false;
    private bool hasDragged = false;  // ���� �巡�� ���θ� üũ�ϴ� ���� �߰�
    private bool canPlace = false;

    // Ȱ��ȭ�Ǿ��� �̹����� �ε����� ������ ����Ʈ �߰�
    private List<int> activeImageIndices = new List<int>();

    // ī�尡 ���Ǿ��� �� �߻��ϴ� �̺�Ʈ (���� ī�嵦�� �Ű������� ����)
    public static event System.Action<GameObject> OnCardUsed;


    // ī�� �ʱ�ȭ: Ÿ�� ����, �ڽ�Ʈ, ������ ����
    public void InitializeCardInform(D_TileShpeData unitData)
    {
        tileShapeName = unitData.f_name;
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        CreateTilePreview(unitData);
        //cost ��� �̺�Ʈ ���� -> cost �߰��ɶ� tile Cost Text ������Ʈ
        GameManager.Instance.OnCostAdd += OnCostChanged;

        UpdateCostTextColor();
    }

    // Ÿ�� ��� �����͸� ������� ������ ��ġ �ʱ�ȭ
    private void InitializeTileShape()
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            tileOffsets = new List<Vector2>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                tileOffsets.Add(tile.f_TilePos.f_TilePos);
            }
        }
    }

    //unitCard �巡�������� �Ⱥ����ߵ�
    private void SetUIVisibility(bool visible)
    {
        foreach (int index in activeImageIndices)
        {
            tileImages[index].gameObject.SetActive(visible);
        }
        cardCostText.gameObject.SetActive(visible);
    }

    // �ڽ�Ʈ ����� ȣ��� �޼���
    private void OnCostChanged()
    {
        UpdateCostTextColor();
    }

    //cost �� -> ���� ���� : �Ͼ��, �Ұ��� : ������
    private void UpdateCostTextColor()
    {
        cardCostText.color = GameManager.Instance.CurrentCost >= cardCost ? Color.white : Color.red;
    }

    // �巡�� ����: �ڽ�Ʈ üũ �� ������ �ν��Ͻ� ����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.CurrentCost < cardCost) return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;  // �巡�� ���� �� false�� �ʱ�ȭ
        SetUIVisibility(false);  // UI �����

        // ���콺 Ŀ���� ���� ��ǥ ���

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        Vector3 centerPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);
        CreatePreviewInstances(centerPos);
        UpdatePreviewInstancesPosition(tilePos);
    }

    // �巡�� ��: ������ ��ġ ������Ʈ �� ��ġ ���� ���� ǥ��
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;


        hasDragged = true;  // �巡�װ� �߻������� ǥ��

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);

        if (tilePos != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));

            //canPlace�� ���� ��ġ ���ɺҰ��� ����
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, previewInstances);
            UpdatePreviewInstancesPosition(tilePos);
            previousTilePosition = tilePos;
        }
        
    }

    // �巡�� ����: ��ġ ���� ���� Ȯ�� �� ���� ��ġ �Ǵ� ���
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //��ġ Ÿ�� ����ȭ
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        //��ġ �������� üũ
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, previewInstances);

        if (hasDragged && canPlace)
        {
            PlaceUnits();
        }
        else
        {
            SetUIVisibility(true);  // ��ġ ���н� UI �ٽ� ���̰�
            CancelPlacement();
        }

        isDragging = false;
    }

    // ������ �ν��Ͻ� ����: �� Ÿ�� ��ġ�� ���� ������ ���� ����
    private void CreatePreviewInstances(Vector3 pointerPosition)
    {
        var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);

        for (int i = 0; i < tileOffsets.Count; i++)
        {
            var unitBuildData = tileShapeData.f_unitBuildData[i];
            var unitPoolinKey = unitBuildData.f_unitData.f_UnitPoolingKey;
            
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitPoolinKey.f_PoolObjectAddressableKey, pointerPosition);

            UnitController previewUnit = previewInstance.GetComponent<UnitController>();

            if (previewUnit != null)
            {
                previewUnit.Initialize();
                previewUnit.InitializeUnitData(unitBuildData.f_unitData);
                previewInstances.Add(previewUnit);
            }
        }
    }



    // ������ ��ġ ������Ʈ: ���� ���콺 ��ġ�� ���� ������ ��ġ ����, ��ġ�Ұ��� ���� ���׸��� ����
    private UnitController temporaryMergedUnit; // �ӽ� �ռ� ������ �����ϴ� ����

    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {
        // ���� �ռ� ���� ��ȯ
        if (temporaryMergedUnit != null)
        {
            PoolingManager.Instance.ReturnObject(temporaryMergedUnit.gameObject);
            temporaryMergedUnit = null;
        }

        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector2 previewPosition = basePosition + tileOffsets[i];
            var tileData = TileMapManager.Instance.GetTileData(previewPosition);
            var previewUnit = previewInstances[i];

            // �ռ� ������ ��� ó��
            if (tileData?.placedUnit != null)
            {
                var placedUnit = tileData.placedUnit;

                if (previewUnit.upgradeUnitType == placedUnit.upgradeUnitType &&
                    previewUnit.upgradeUnitType != UpgradeUnitType.None)
                {
                    var unitData = D_UnitData.GetEntityByKeyUpgradeUnitKey(previewUnit.upgradeUnitType);
                    var poolingKey = unitData.f_UpgradePoolingKey;
                    var objectPool = poolingKey.f_PoolObjectAddressableKey;

                    // ��Ȯ�� Ÿ�� ��ġ�� �̵�
                    Vector3 mergedUnitPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);

                    GameObject newPreview = PoolingManager.Instance.GetObject(objectPool, mergedUnitPosition);
                    temporaryMergedUnit = newPreview.GetComponent<UnitController>();

                    if (temporaryMergedUnit != null)
                    {
                        temporaryMergedUnit.Initialize();
                        temporaryMergedUnit.InitializeUnitData(unitData);
                        temporaryMergedUnit.SetPreviewMaterial(canPlace);

                    }

                    // ���� ������ ���� ����
                    previewUnit.gameObject.SetActive(false);
                    continue;
                }
            }

            // �⺻ ������ ���� ������Ʈ
            previewUnit.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
            previewUnit.SetPreviewMaterial(canPlace);
            previewUnit.gameObject.SetActive(true); // �������� ���� �ٽ� Ȱ��ȭ


        }
    }


    // ���� ��ġ: �����並 ���� �������� ��ȯ�ϰ� ���� ���� ������Ʈ
    private void PlaceUnits()
    {
        // �ռ��� ������ �ִ� ���
        if (temporaryMergedUnit != null)
        {
            Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(temporaryMergedUnit.transform.position);
            Vector2 unitTilePos = new Vector2(tilePosition.x, tilePosition.y);

            // ���� ��ġ�� ���� ���� (�ռ� ���)
            var tileData = TileMapManager.Instance.GetTileData(unitTilePos);
            if (tileData?.placedUnit != null)
            {
                var placedUnit = tileData.placedUnit;
                UnitManager.Instance.UnregisterUnit(placedUnit);
                PoolingManager.Instance.ReturnObject(placedUnit.gameObject);
            }

            // �ռ��� ���� ���
            temporaryMergedUnit.DestroyPreviewUnit(); // ��Ƽ���� ����
            temporaryMergedUnit.InitializeTilePos(unitTilePos);
            UnitManager.Instance.RegisterUnit(temporaryMergedUnit);

            // Ÿ�� ������ ������Ʈ
            TileMapManager.Instance.SetTileData(new TileData(unitTilePos)
            {
                isAvailable = false,
                placedUnit = temporaryMergedUnit
            });

            temporaryMergedUnit = null; // ���� �ʱ�ȭ
        }

        // ������ �⺻ ���� ��ġ ó��
        foreach (var previewUnit in previewInstances)
        {
            Vector3Int cellPosition = TileMapManager.Instance.tileMap.WorldToCell(previewUnit.transform.position);
            Vector2 tilePosition = new Vector2(cellPosition.x, cellPosition.y);

            // Ÿ�� ������ ��������
            var tileData = TileMapManager.Instance.GetTileData(tilePosition);

            // �ش� Ÿ�Ͽ� �ٸ� ������ ������ ��ŵ (�̹� ó���� �ռ��� ����)
            if (tileData?.placedUnit != null) continue;

            // �⺻ ���� ���
            previewUnit.DestroyPreviewUnit(); // ��Ƽ���� ����
            previewUnit.InitializeTilePos(tilePosition);
            UnitManager.Instance.RegisterUnit(previewUnit);

            // Ÿ�� ������ ������Ʈ
            TileMapManager.Instance.SetTileData(new TileData(tilePosition)
            {
                isAvailable = false,
                placedUnit = previewUnit
            });
        }

        // �ڽ�Ʈ �Ҹ� ó�� �� UI ����
        GameManager.Instance.UseCost(cardCost);
        EnemyManager.Instance.UpdateEnemiesPath();

        // �̺�Ʈ�� ���� ī�� ��� �˸�
        OnCardUsed?.Invoke(transform.parent.gameObject);

        previewInstances.Clear();
        Destroy(gameObject);
    }


    // ��ġ ���: ������ �ν��Ͻ� ����
    private void CancelPlacement()
    {
        foreach (var instance in previewInstances)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }
        previewInstances.Clear();
    }

    // ī�� UI�� Ÿ�� ������ ����
    private void CreateTilePreview(D_TileShpeData tileShapeData)
    {
        foreach (var image in tileImages)
        {
            image.gameObject.SetActive(false);
        }

        activeImageIndices.Clear();  // ���� �ε��� �ʱ�ȭ


        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePos.f_TilePos;

            // 3x3 �׸��忡�� �߾��� (0,0)���� ���� �ε��� ���
            int x = Mathf.RoundToInt(tilePos.x) + 1;
            int y = Mathf.RoundToInt(-tilePos.y) + 1;
            int index = y * 3 + x;

            if (index >= 0 && index < tileImages.Count)
            {
                GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);

                UnitController unitController = tempUnit.GetComponent<UnitController>();

                if (unitController != null && unitController.unitSprite != null)
                {
                    tileImages[index].gameObject.SetActive(true);

                    if (buildData.f_unitData.f_SkillAttackType != SkillAttackType.None)
                    {
                        tileImages[index].sprite = unitController.unitSprite.sprite;
                    }
                    
                    activeImageIndices.Add(index);  // Ȱ��ȭ�� �ε��� ����
                }

                PoolingManager.Instance.ReturnObject(tempUnit);
            }
        }
    }

    // ���ҽ� ����
    private void OnDestroy()
    {
        if (isDragging)
        {
            CancelPlacement();
        }

        GameManager.Instance.OnCostAdd -= OnCostChanged;

    }

}