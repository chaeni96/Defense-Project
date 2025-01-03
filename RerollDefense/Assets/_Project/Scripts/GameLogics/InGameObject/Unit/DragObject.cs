using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;
using System.Xml;
using UnityEngine.Rendering.Universal;


//�巡�� �� ��ġ ���� ���
//������ �ν��Ͻ� ���� �� ����
//Ÿ�� ��ġ ���� ���� üũ
//������Ʈ ��ġ

public class DragObject : BasicObject
{

    public bool isPlaced { get; private set; } = false;

    [SerializeField] private SpriteRenderer spriteRenderer;
    private List<Vector3Int> relativeTiles;
    private Vector3Int previousTilePosition;
    private Vector3 originalPos;
    private Color originColor;
    private string tileShapeName;

    private List<GameObject> previewInstances = new List<GameObject>();

    public override void Initialize()
    {
        base.Initialize();
        //spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1);
        originalPos = transform.position;
        originColor = spriteRenderer.color;
        isPlaced = false;
    }
    

    //������ ī�� ������ ���� Ÿ�� �ʱ�ȭ
    private void InitializeTileShape(string tileDataKey)
    {
        tileShapeName = tileDataKey;

        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);


        if (tileShapeData != null)
        {
            relativeTiles = new List<Vector3Int>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                D_TilePosData position = tile.f_TilePos;
                Vector2 tilePos = position.f_TilePos;


                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tilePos.x),
                    Mathf.RoundToInt(tilePos.y),
                    0
                );
                relativeTiles.Add(relativeTile);
            }
        }
    }

    //�巡�� ������Ʈ Ŭ��������
    public void OnClickObject(string tileDataKey, Vector3 pointerPosition)
    {
        //tile���� �ʱ�ȭ
        InitializeTileShape(tileDataKey);

        //������ Ÿ��
        CreatePreviewInstances(pointerPosition);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        }

    }

    //�巡����
    public void OnPointerDrag(Vector3 pointerPosition)
    {
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);
        transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(baseTilePosition);

        if (baseTilePosition != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));

            //�巡�� �߿��� ��ġ �������� üũ�ؾߵ�
            bool canPlace = TileMapManager.Instance.CanPlaceObject(baseTilePosition, relativeTiles);

            UpdatePreviewInstancesPosition(baseTilePosition, canPlace);
            UpdateTileColors(baseTilePosition, canPlace);

            previousTilePosition = baseTilePosition;
        }
    }

    //�巡���Ҷ��� ��ġ�� ������Ʈ ������ �����ֱ�
    private void CreatePreviewInstances(Vector3 pointerPosition)
    {
        // �� ��� Ÿ�� ��ġ�� ������ �ν��Ͻ� ����

        List<Vector3Int> tilePositions = GetTilePositions(previousTilePosition);

        for (int i = 0; i < tilePositions.Count; i++)
        {
            var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);
            var unitBuildData = tileShapeData.f_unitBuildData[i];

            GameObject previewObjectInstance = PoolingManager.Instance.GetObject(
                unitBuildData.f_unitData.f_unitPrefabKey,
                pointerPosition
            );

            UnitController previewObject = previewObjectInstance.GetComponent<UnitController>();

            if (previewObject != null)
            {
                previewObject.InitializeUnitStat(unitBuildData);
                previewObject.ShowPreviewUnit();
                previewInstances.Add(previewObjectInstance);
            }
        }
    }

    private void UpdatePreviewInstancesPosition(Vector3Int basePosition, bool canPlace)
    {
        // �� ��� Ÿ�� ��ġ�� ������ �ν��Ͻ� �̵�
        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector3Int previewPosition = basePosition + relativeTiles[i];
            previewInstances[i].transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previewPosition);
        }
    }


    public void CheckPlacedObject()
    {
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        bool canPlace = TileMapManager.Instance.CanPlaceObject(previousTilePosition, relativeTiles);

        if (canPlace)
        {
            PlaceObject();
        }
        else
        {
            CancelPlacement();
        }
    }

    private void PlaceObject()
    {
        // ������ �ν��Ͻ����� �������� ���� ������Ʈ�� ��ȯ
        foreach (var previewInstance in previewInstances)
        {
            SpriteRenderer instanceSpriteRenderer = previewInstance.GetComponent<SpriteRenderer>();
            if (instanceSpriteRenderer != null)
            {
                instanceSpriteRenderer.color = Color.white;
            }
        }

        TileMapManager.Instance.OccupyTile(previousTilePosition, relativeTiles);
        CreatePlacedUnits();

        EnemyManager.Instance.UpdateEnemiesPath();
        isPlaced = true;

        ClearPreviewInstances();
    }

    private void CancelPlacement()
    {
        ClearPreviewInstances();

        transform.position = originalPos;
        spriteRenderer.color = originColor;
        isPlaced = false;
    }

    private void ClearPreviewInstances()
    {
        foreach (var previewInstance in previewInstances)
        {
            Destroy(previewInstance);
        }
        previewInstances.Clear();
    }

    private void CreatePlacedUnits()
    {
        List<Vector3Int> tilePositions = GetTilePositions(previousTilePosition);
        var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);

        for (int i = 0; i < tilePositions.Count; i++)
        {
            var unitBuildData = tileShapeData.f_unitBuildData[i];
            GameObject placedObjectInstance = PoolingManager.Instance.GetObject(
                unitBuildData.f_unitData.f_unitPrefabKey,
                TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePositions[i])
            );

            UnitController unitObject = placedObjectInstance.GetComponent<UnitController>();

            if (unitObject != null)
            {
                unitObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePositions[i]);
                unitObject.InitializeUnitStat(unitBuildData);
                UnitManager.Instance.RegisterUnit(unitObject);
            }
        }

        Destroy(gameObject);
    }

    private List<Vector3Int> GetTilePositions(Vector3Int basePosition)
    {
        return relativeTiles.ConvertAll(relativeTile => basePosition + relativeTile);
    }

    private void UpdateTileColors(Vector3Int basePosition, bool canPlace)
    {
        Color tileColor = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);

        foreach (var position in GetTilePositions(basePosition))
        {
            TileMapManager.Instance.SetTileColors(position, new List<Vector3Int> { Vector3Int.zero }, tileColor);
        }
    }


}