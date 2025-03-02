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
    [SerializeField] private GameObject tileImageLayout; // TileImage_Layout ���ӿ�����Ʈ
    [SerializeField] private GameObject tileImagePrefab; // Tile_Image ������

  
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
        UpdateCostTextColor();
        CreateTilePreview(unitData);

        //cost ��� �̺�Ʈ ���� -> cost �߰��ɶ� tile Cost Text ������Ʈ
        GameManager.Instance.OnCostAdd += OnCostChanged;

        StageManager.Instance.OnWaveFinish += CancelDragging;

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
                // ���� ��ǥ
                Vector2 originalPos = tile.f_TilePos.f_TilePos;

                // Y�� ���� - (0,0)�� �������� �� (0,1)�� ���ʿ� ��ġ�ǵ���
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
            canPlace = TileMapManager.Instance.CanPlaceObject(tilePos, tileOffsets, originalPreviews);
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
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets, originalPreviews);

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
            
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitPoolinKey.f_PoolObjectAddressableKey, worldPos, (int)ObjectLayer.Player);

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

                foreach (var star in tileData.placedUnit.starObjects)
                {
                    star.SetActive(true);
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
                 currentPreview.GetStat(StatName.UnitStarLevel) == tileData.placedUnit.GetStat(StatName.UnitStarLevel) &&
                tileData.placedUnit.GetStat(StatName.UnitStarLevel) < 3)
            {
                foreach (var star in tileData.placedUnit.starObjects)
                {
                    star.SetActive(false);
                }

                Vector3 mergedPosition = TileMapManager.Instance.GetTileToWorldPosition(previewPosition);

                // ���� ������ ��Ȱ��ȭ
                currentPreview.gameObject.SetActive(false);

                // ���ο� �ռ� ���� ���� �� ����
                GameObject mergedPreview = PoolingManager.Instance.GetObject(currentPreview.unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey, mergedPosition, (int)ObjectLayer.Player);
                var mergedUnit = mergedPreview.GetComponent<UnitController>();
                mergedUnit.Initialize();
                mergedUnit.InitializeUnitInfo(currentPreview.unitData);
                

                int newStarLevel = (int)tileData.placedUnit.GetStat(StatName.UnitStarLevel) + 1;
                mergedUnit.UpGradeUnitLevel(newStarLevel);
                
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
                    
                }
            }
        }


        // �� ���� ���� ��������� ��ġ
        foreach (var pair in currentPreviews)
        {
            var unitInstance = pair.Value;

            unitInstance.DestroyPreviewUnit();
            UnitManager.Instance.RegisterUnit(unitInstance);
        }

        //Ÿ�� ��ġ �Ұ����·� ����, �ڽ�Ʈ ���
        TileMapManager.Instance.OccupyTiles(previousTilePosition, tileOffsets, currentPreviews);

        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = cardCost * -1, // ������ �Ҹ�
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
        // ���� Ÿ�� �̹��� ��� ����
        ClearTileImages();

        // Ÿ�� �����Ϳ��� �ּ�/�ִ� x, y ��ǥ ã��
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePos.f_TilePos;
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
        float tileSize = 100f;
        float spacing = 10f; // 0.1 * tileSize
        layoutRect.sizeDelta = new Vector2(width * (tileSize + spacing) - spacing, height * (tileSize + spacing) - spacing);

        // ���� �����͸� �׸��� ��ġ�� ����
        Dictionary<Vector2Int, D_unitBuildData> unitPositionMap = new Dictionary<Vector2Int, D_unitBuildData>();

        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2Int normalizedPos = new Vector2Int(
                (int)buildData.f_TilePos.f_TilePos.x - minX,
                (int)buildData.f_TilePos.f_TilePos.y - minY
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
                    Image tileImage = tileObject.unitImage;

                    if (tileImage != null)
                    {
                        tileImages.Add(tileImage);
                        activeImageIndices.Add(tileImages.Count - 1);
                    }

                    // �ش� ��ġ�� ���� �����Ͱ� �ִ��� Ȯ��
                    if (unitPositionMap.TryGetValue(gridPos, out var buildData))
                    {
                        // ���� ��������Ʈ ��������
                        GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_UnitPoolingKey.f_PoolObjectAddressableKey);
                        UnitController unitController = tempUnit.GetComponent<UnitController>();

                        if (unitController != null && unitController.unitSprite != null)
                        {
                            // SkillAttackType üũ
                            bool isBase = (buildData.f_unitData.f_SkillAttackType == SkillAttackType.None);

                            Sprite unitSprite = null;

                            // base�� �ƴ� ��쿡�� ��������Ʈ ����
                            if (!isBase)
                            {
                                unitSprite = unitController.unitSprite.sprite;
                            }
                            // Ÿ�� �ʱ�ȭ (base ���� ����)
                            tileObject.InitUnitImage(unitSprite, isBase);
                        }

                        PoolingManager.Instance.ReturnObject(tempUnit);
                    }
                    else
                    {
                        // �� Ÿ�� �ʱ�ȭ
                        tileObject.InitUnitImage(null, false);
                    }
                }

            }
        }

        Debug.Log($"Ÿ�� ������ ���� �Ϸ�: �� {tileShapeData.f_unitBuildData.Count}���� ����, �׸��� ũ�� {width}x{height}");
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
            CancelPlacement();
            isDragging = false;
        }
    }

    private void CleanUp()
    {
        ClearTileImages(); // Ÿ�� �̹��� ����

        originalPreviews.Clear();
        currentPreviews.Clear();
        activeImageIndices.Clear();

        // �̺�Ʈ ���� ����
        GameManager.Instance.OnCostAdd -= OnCostChanged;
        StageManager.Instance.OnWaveFinish -= CancelDragging;

    }


}