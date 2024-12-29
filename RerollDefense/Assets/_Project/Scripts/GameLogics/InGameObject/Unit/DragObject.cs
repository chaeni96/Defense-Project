using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;
using UnityEngine.UIElements;
using System.Xml;

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

    //드래그 오브젝트 클릭했을시
    public void OnClickObject(string tileDataKey, Vector3 pointerPosition)
    {
        InitializeTileShape(tileDataKey);
        CreatePreviewInstances(transform.position, pointerPosition);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        }

    }

    //선택한 카드 종류에 따른 타일 초기화
    private void InitializeTileShape(string tileDataKey)
    {
        tileShapeName = tileDataKey;

        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);
        if (tileShapeData != null)
        {
            relativeTiles = new List<Vector3Int>();
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tile.f_position.x),
                    Mathf.RoundToInt(tile.f_position.y),
                    0
                );
                relativeTiles.Add(relativeTile);
            }
        }
    }

    //드래그중
    public void OnPointerDrag(Vector3 pointerPosition)
    {
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);
        transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(baseTilePosition);

        if (baseTilePosition != previousTilePosition)
        {
            TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0.1f));
            
            //드래그 중에도 설치 가능한지 체크해야됨
            bool canPlace = CanPlaceAtPosition(baseTilePosition);

            UpdatePreviewInstancesPosition(baseTilePosition, canPlace);

            UpdateTileColors(baseTilePosition, canPlace);

            previousTilePosition = baseTilePosition;
        }
    }

    //드래그할때도 설치할 오브젝트 프리뷰 보여주기
    private void CreatePreviewInstances(Vector3 baseWorldPosition, Vector3 pointerPosition)
    {
        // 각 상대 타일 위치에 프리뷰 인스턴스 생성

        foreach (var position in GetTilePositions(previousTilePosition))
        {

            var currentUnitIndex = relativeTiles.IndexOf(position - previousTilePosition);
            var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);
            var unitBuildData = tileShapeData.f_unitBuildData[currentUnitIndex];

            GameObject previewObjectInstance = PoolingManager.Instance.GetObject(unitBuildData.f_unitData.f_unitPrefabKey, TileMapManager.Instance.tileMap.GetCellCenterWorld(position));

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
        // 각 상대 타일 위치에 프리뷰 인스턴스 이동
        for (int i = 0; i < previewInstances.Count; i++)
        {
            Vector3Int previewPosition = basePosition + relativeTiles[i];
            previewInstances[i].transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previewPosition);
        }
    }


    public void CheckPlacedObject()
    {
        // 타일 색상 초기화
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        if (CanPlaceAtPosition(previousTilePosition))
        {
            // 프리뷰 인스턴스들을 불투명한 실제 오브젝트로 변환
            foreach (var previewInstance in previewInstances)
            {
                SpriteRenderer spriteRenderer = previewInstance.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
            }

            // 기존 배치 로직 유지
            string tileUniqueID = Guid.NewGuid().ToString();
            TileMapManager.Instance.OccupyTile(previousTilePosition, relativeTiles, tileUniqueID);
            CreatePlacedObject(tileUniqueID);
            EnemyManager.Instance.UpdateEnemiesPath();
            isPlaced = true;


            // 프리뷰 인스턴스 리스트 초기화
            foreach (var previewInstance in previewInstances)
            {
                Destroy(previewInstance);
            }
            previewInstances.Clear();
        }
        else
        {
            // 배치 불가능한 경우 프리뷰 인스턴스들 제거
            foreach (var previewInstance in previewInstances)
            {
                Destroy(previewInstance);
            }
            previewInstances.Clear();

            transform.position = originalPos;
            spriteRenderer.color = originColor;
            isPlaced = false;
        }
    }

    //유닛오브젝트 생성
    private void CreatePlacedObject(string uniqueID)
    {
        foreach (var position in GetTilePositions(previousTilePosition))
        {

            var currentUnitIndex = relativeTiles.IndexOf(position - previousTilePosition);
            var tileShapeData = D_TileShpeData.GetEntity(tileShapeName);
            var unitBuildData = tileShapeData.f_unitBuildData[currentUnitIndex];
            GameObject placedObjectInstance = PoolingManager.Instance.GetObject(unitBuildData.f_unitData.f_unitPrefabKey, TileMapManager.Instance.tileMap.GetCellCenterWorld(position));

            UnitController unitObject = placedObjectInstance.GetComponent<UnitController>();

            if (unitObject != null)
            {
                unitObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(position);
                unitObject.RegistereTileID(uniqueID, tileShapeData);
                unitObject.InitializeUnitStat(unitBuildData);
                UnitManager.Instance.RegisterUnit(unitObject);
            }
        }
        Destroy(gameObject);
    }

    //기준타일 + 상대타일 좌표 가져와야됨 = 다중타일 체크
    private List<Vector3Int> GetTilePositions(Vector3Int basePosition)
    {
        List<Vector3Int> positions = new List<Vector3Int>();
        foreach (var relativeTile in relativeTiles)
        {
            positions.Add(basePosition + relativeTile);
        }
        return positions;
    }

    private bool CanPlaceAtPosition(Vector3Int basePosition)
    {
        if (!TileMapManager.Instance.CanPlaceObjectAt(basePosition, relativeTiles))
            return false;

        List<Vector3Int> positions = GetTilePositions(basePosition);
        return PathFindingManager.Instance.CanPlaceObstacle(positions);
    }

    private void UpdateTileColors(Vector3Int basePosition, bool canPlace)
    {
        //가능 : 초록색, 불가능 : 빨간색
        Color tileColor = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
        foreach (var position in GetTilePositions(basePosition))
        {
            TileMapManager.Instance.SetTileColors(position, new List<Vector3Int> { Vector3Int.zero },tileColor);
        }
    }

  

}