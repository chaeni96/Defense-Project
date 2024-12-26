using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class DragObject : StaticObject
{
    public bool isPlaced { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private List<Vector3Int> relativeTiles;
    private Vector3Int previousTilePosition;
    private Vector3 originalPos;
    private Color originColor;
    private string tileShapeName;
    public string prefabKey;

    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1);
        originalPos = transform.position;
        originColor = spriteRenderer.color;
        isPlaced = false;
    }

    public override void Update()
    {
        base.Update();
    }


    //드래그 오브젝트 클릭했을시
    public void OnClickObject(string tileDataKey)
    {
        InitializeTileShape(tileDataKey);
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
            spriteRenderer.color = originColor;  // 드래그 중인 오브젝트의 원래 색상 유지
            
            //드래그 중에도 설치 가능한지 체크해야됨
            bool canPlace = CanPlaceAtPosition(baseTilePosition);

            UpdateTileColors(baseTilePosition, canPlace);

            previousTilePosition = baseTilePosition;
        }
    }

    public void CheckPlacedObject()
    {
        TileMapManager.Instance.SetAllTilesColor(new Color(1, 1, 1, 0));

        //배치 가능한지 확인
        if (CanPlaceAtPosition(previousTilePosition))
        {
            string tileUniqueID = Guid.NewGuid().ToString();
            TileMapManager.Instance.OccupyTile(previousTilePosition, relativeTiles, tileUniqueID);

            //유닛오브젝트 설치
            CreatePlacedObject(tileUniqueID);

            //enemy 경로 업데이트
            PathFindingManager.Instance.UpdateCurrentPath();

            isPlaced = true;
            Debug.Log("유닛 배치 완료!");
        }
        else
        {
            transform.position = originalPos;
            spriteRenderer.color = originColor;
            isPlaced = false;
            Debug.Log("유닛 불가 지역!");
        }
    }

    //유닛오브젝트 생성
    private void CreatePlacedObject(string uniqueID)
    {
        foreach (var position in GetTilePositions(previousTilePosition))
        {
            GameObject placedObjectInstance = ResourceManager.Instance.Instantiate(prefabKey);
            PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();

            if (placedObject != null)
            {
                placedObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(position);
                placedObject.RegistereTileID(uniqueID);
                placedObject.InitializeUnitStat(tileShapeName, relativeTiles.IndexOf(position - previousTilePosition));
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