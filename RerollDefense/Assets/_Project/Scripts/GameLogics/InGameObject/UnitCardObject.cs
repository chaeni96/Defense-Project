using BGDatabaseEnum;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ���� Ÿ�� UI ��Ҹ� �����ϰ� �巡�� �� ������� �ʿ� ������ ��ġ�ϴ� Ŭ����
public class UnitCardObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{ 
    //UI����� 9���� Ÿ�� �̹��� �����ϴ� ����Ʈ
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private TMP_Text cardCostText;

    //������ ���ֵ��� ����, �ռ� ���� �Ǵ�, �巡�� ���� ���� ���� �� ������Ʈ

    //TileData�� ������ ����
    private Dictionary<int, UnitController> originalPreviews = new Dictionary<int, UnitController>();

    //���� ȭ�鿡 �������� ������ ����, �ռ� ���ο� ���� ���� ������ ��ü, ��ġ ����, ���͸��� ������Ʈ
    private Dictionary<int, UnitController> currentPreviews = new Dictionary<int, UnitController>(); 

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
        CleanUp();

        tileShapeName = unitData.f_name;
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();

        InitializeTileShape();
        CreateTilePreview(unitData);
        UpdateCostTextColor();

        //cost ��� �̺�Ʈ ���� -> cost �߰��ɶ� tile Cost Text ������Ʈ
        GameManager.Instance.OnCostAdd += OnCostChanged;

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
        cardCostText.color = GameManager.Instance.GetSystemStat(StatName.Cost) >= cardCost ? Color.white : Color.red;
    }

    // �巡�� ����: �ڽ�Ʈ üũ �� ������ �ν��Ͻ� ����
    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < cardCost) return;

        isDragging = true;
        canPlace = false;
        hasDragged = false;  // �巡�� ���� �� false�� �ʱ�ȭ
        SetUIVisibility(false);  // UI �����

        // ���콺 Ŀ���� ���� ��ǥ ���

        Vector3 worldPos = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        worldPos.z = 0;

        Vector2 tilePos = TileMapManager.Instance.GetWorldToTilePosition(worldPos);
        CreatePreviewInstances(tilePos);
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
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, currentPreviews);
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
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, currentPreviews);

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
    private void CreatePreviewInstances(Vector3 tilePos)
    {
        
        //������ worldPos
        Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(tilePos);

        var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);

        for (int i = 0; i < tileOffsets.Count; i++)
        {
            var unitBuildData = tileShapeData.f_unitBuildData[i];
            var unitPoolinKey = unitBuildData.f_unitData.f_UnitPoolingKey;
            
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitPoolinKey.f_PoolObjectAddressableKey, worldPos);

            UnitController previewUnit = previewInstance.GetComponent<UnitController>();

            if (previewUnit != null)
            {
                previewUnit.Initialize();
                previewUnit.InitializeUnitInfo(unitBuildData.f_unitData, tilePos);
                originalPreviews[i] = previewUnit;
                currentPreviews[i] = previewUnit;
            }
        }
    }



    // ������ ��ġ ������Ʈ: ���� ���콺 ��ġ�� ���� ������ ��ġ ����, ��ġ�Ұ��� ���� ���׸��� ����
    private void UpdatePreviewInstancesPosition(Vector2 basePosition)
    {

        //�巡�� ���� �ռ��� ������ �ִٸ� ���� ���·� �����ؾߵ�

        // �ռ��� ������ �ִٸ� ���� ���·� ����
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            if (currentPreviews[i] != originalPreviews[i])
            {
                // �ش� ��ġ�� ���� ��ġ�� ������ �� �ٽ� Ȱ��ȭ
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);
                if (tileData?.placedUnit != null)
                {
                    tileData.placedUnit.unitStarObject.SetActive(true);
                }

                PoolingManager.Instance.ReturnObject(currentPreviews[i].gameObject);
                currentPreviews[i] = originalPreviews[i];
                currentPreviews[i].gameObject.SetActive(true);
            }
        }

        //���ʴ�� Ÿ�� �о����
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 previewPosition = basePosition + tileOffsets[i];

            var tileData = TileMapManager.Instance.GetTileData(previewPosition);

            var currentPreview = currentPreviews[i];

            // Ÿ�Ͽ� ������ �ְ� �ռ��� ������ ���(����Ÿ���� �Ȱ��� ���� ���׷��̵��� ������ �ִ°��)
            if (tileData?.placedUnit != null &&
                currentPreview.unitType == tileData.placedUnit.unitType &&
                tileData.placedUnit.unitData.f_NextLevelUnit != null)
            {
                //���� ��ġ�� ���� �� ��Ȱ��ȭ
                tileData.placedUnit.unitStarObject.SetActive(false);

                var nextLevelUnitData = tileData.placedUnit.unitData.f_NextLevelUnit;
                var poolingKey = nextLevelUnitData.f_UnitPoolingKey.f_PoolObjectAddressableKey;

                Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);

                // ���� ������ ��Ȱ��ȭ
                currentPreview.gameObject.SetActive(false);

                // ���ο� �ռ� ���� ���� �� ����
                GameObject mergedPreview = PoolingManager.Instance.GetObject(poolingKey, mergedPosition);
                var mergedUnit = mergedPreview.GetComponent<UnitController>();
                mergedUnit.Initialize();
                mergedUnit.InitializeUnitInfo(nextLevelUnitData, previewPosition);
                mergedUnit.SetPreviewMaterial(canPlace);
                mergedUnit.unitSprite.transform.DOPunchScale(Vector3.one * 0.8f, 0.3f, 4, 1);
                // ���� �����並 �ռ��� ������ ��ü
                currentPreviews[i] = mergedUnit;

            }
            else
            {
           


                currentPreview.gameObject.SetActive(true);
                currentPreview.transform.position = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);
                currentPreview.SetPreviewMaterial(canPlace);
            }
        }
    }


    // ���� ��ġ: �����並 ���� �������� ��ȯ�ϰ� ���� ���� ������Ʈ
    private void PlaceUnits()
    {
        // ���� �ռ��� �Ͼ ��ġ�� ���� ���ֵ鸸 ����
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            // ���� �����䰡 ������ �ٸ��ٸ� �ռ��� �Ͼ ��ġ
            if (currentPreviews[i] != originalPreviews[i])
            {
                Vector2 position = previousTilePosition + tileOffsets[i];
                var tileData = TileMapManager.Instance.GetTileData(position);

                if (tileData?.placedUnit != null)
                {
                    UnitManager.Instance.UnregisterUnit(tileData.placedUnit);
                    PoolingManager.Instance.ReturnObject(tileData.placedUnit.gameObject);
                    var newTileData = new TileData(position);
                    TileMapManager.Instance.SetTileData(newTileData);
                }
            }
        }


        // �� ���� ���� ��������� ��ġ
        foreach (var pair in currentPreviews)
        {
            var unitInstance = pair.Value;

            unitInstance.DestroyPreviewUnit();
            Vector3Int pos = TileMapManager.Instance.tileMap.WorldToCell(unitInstance.transform.position);
            UnitManager.Instance.RegisterUnit(unitInstance);
        }

        //Ÿ�� ��ġ �Ұ����·� ����, �ڽ�Ʈ ���
        TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);


        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            stat = StatName.Cost,
            value = cardCost * -1,
            multiply = 1f
        });
        
        //enemy path ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();

        // ���� ī�� ���� -> �̺�Ʈ ���ؼ�
        OnCardUsed?.Invoke(transform.parent.gameObject);

        originalPreviews.Clear();
        currentPreviews.Clear();
        Destroy(gameObject);
    }

    // ��ġ ��ҽ� ȣ�� �޼���: ������ �ν��Ͻ� ����
    private void CancelPlacement()
    {
        foreach (var instance in currentPreviews.Values)
        {
            PoolingManager.Instance.ReturnObject(instance.gameObject);
        }
        originalPreviews.Clear();
        currentPreviews.Clear();

        GameManager.Instance.OnCostAdd -= OnCostChanged;
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


    private void CleanUp()
    {
        originalPreviews.Clear();
        currentPreviews.Clear();
        activeImageIndices.Clear();

        // �̺�Ʈ ���� ����
        GameManager.Instance.OnCostAdd -= OnCostChanged;
    }


}