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
    private List<Vector3Int> tileOffsets;
    private Vector3Int previousTilePosition;
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
        UpdateCostTextColor();
        CreateTilePreview(unitData);

        //cost ��� �̺�Ʈ ���� -> cost �߰��ɶ� tile Cost Text ������Ʈ
        GameManager.Instance.OnCostAdd += OnCostChanged;
    }

    // Ÿ�� ��� �����͸� ������� ������ ��ġ �ʱ�ȭ
    private void InitializeTileShape()
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            tileOffsets = new List<Vector3Int>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                Vector2 tilePos = tile.f_TilePos.f_TilePos;
                Vector3Int tileOffset = new Vector3Int(
                    Mathf.RoundToInt(tilePos.x),
                    Mathf.RoundToInt(tilePos.y),
                    0
                );
                tileOffsets.Add(tileOffset);
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
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;



        // Ÿ�� ��ġ ���
        Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        Vector3 centerPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);

        CreatePreviewInstances(centerPosition);
        UpdatePreviewInstancesPosition(tilePosition);  // ������ ��ġ ��� ������Ʈ
    }

    // �巡�� ��: ������ ��ġ ������Ʈ �� ��ġ ���� ���� ǥ��
    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;


        hasDragged = true;  // �巡�װ� �߻������� ǥ��

        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        if (baseTilePosition != previousTilePosition)
        {
            //TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));
            canPlace = TileMapManager.Instance.CanPlaceObject(baseTilePosition, tileOffsets);

            //canPlace�� ���� ��ġ ���ɺҰ��� ����
            UpdatePreviewInstancesPosition(baseTilePosition);
            previousTilePosition = baseTilePosition;
        }
    }

    // �巡�� ����: ��ġ ���� ���� Ȯ�� �� ���� ��ġ �Ǵ� ���
    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isDragging) return;

        //��ġ Ÿ�� ����ȭ
        //TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        //��ġ �������� üũ
        canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, tileOffsets);

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
            GameObject previewInstance = PoolingManager.Instance.GetObject(unitBuildData.f_unitData.f_unitPrefabKey,pointerPosition);

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
    private void UpdatePreviewInstancesPosition(Vector3Int basePosition)
    {
        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector3Int previewPosition = basePosition + tileOffsets[i];
            previewInstances[i].transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previewPosition);
            previewInstances[i].SetPreviewMaterial(canPlace);
            
        }
    }

    // ���� ��ġ: �����並 ���� �������� ��ȯ�ϰ� ���� ���� ������Ʈ
    private void PlaceUnits()
    {
        foreach (var unitInstance in previewInstances)
        {
            unitInstance.DestroyPreviewUnit();

            UnitManager.Instance.RegisterUnit(unitInstance);
           
        }

        //Ÿ�� ��ġ �Ұ����·� ����, �ڽ�Ʈ ���
        TileMapManager.Instance.OccupyTile(previousTilePosition, tileOffsets);
        GameManager.Instance.UseCost(cardCost);
        //enemy path ������Ʈ
        EnemyManager.Instance.UpdateEnemiesPath();

        // ���� ī�� ���� -> �̺�Ʈ ���ؼ�
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
                GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_unitPrefabKey);

                UnitController unitController = tempUnit.GetComponent<UnitController>();

                if (unitController != null && unitController.unitSprite != null)
                {
                    tileImages[index].gameObject.SetActive(true);

                    if (buildData.f_unitData.f_SkillAttackType != SkillAttackType.None)
                    {
                        tileImages[index].sprite = unitController.unitSprite.sprite;
                        activeImageIndices.Add(index);  // Ȱ��ȭ�� �ε��� ����
                    }
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