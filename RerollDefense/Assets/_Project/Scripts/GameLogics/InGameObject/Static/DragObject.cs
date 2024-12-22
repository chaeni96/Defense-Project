using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class DragObject : StaticObject
{
    public bool isPlaced { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private Color originColor;
    private Vector3 originalPos;

    private string tileShapeName; //데이터베이스에서 불러올 이름


    //TODO : 테스트용, 오브젝트 데이터 사용해야됨
    //드래그오브젝트에 설치해야할 키값이 필요함
    public testGold test;


    public string prefabKey;

    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // 유효하지 않는 값으로 초기화
        originalPos = transform.position;
        originColor = spriteRenderer.color;
        isPlaced = false;
    }

    public void SetUpUnitData(string tileDataKey)
    {
        tileShapeName = tileDataKey;

    }

    public override void Update()
    {
        base.Update();

    }

    private void InitializeTileShape()
    {
        // D_TileShpeData에서 tileShapeType에 해당하는 데이터를 가져옴
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            Debug.Log($"TileShapeData 발견: {tileShapeData.f_name}");

            relativeTiles = new List<Vector3Int>();

            // f_unitBuildData에 있는 위치 데이터를 반복해서 가져옴
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                // Vector2 데이터를 Vector3Int로 변환
                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tile.f_position.x),
                    Mathf.RoundToInt(tile.f_position.y),
                    0 // z축은 항상 0으로 설정
                );

                relativeTiles.Add(relativeTile);
                Debug.Log($"추가된 타일 좌표: {relativeTile}");
            }
        }
        else
        {
            Debug.LogError($"TileShapeData를 찾을 수 없습니다. TileShapeType: {tileShapeName}");
        }

    }

    public void TESTOnPointerDown()
    {
        InitializeTileShape();

        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnPointerDrag(Vector3 pointerPosition)
    {
       

        // 기준 타일(클릭한 위치)의 타일 좌표 계산
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        // 타일의 중심 기준으로 위치 조정
        Vector3 centerPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(baseTilePosition);
        transform.position = centerPosition;

        // 드래그 중 투명도 조정
        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;

        // 타일 색상 갱신
        if (baseTilePosition != previousTilePosition)
        {
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f)); // 이전 타일 색상 초기화

            // 각 타일별 배치 가능 여부에 따라 색상 설정
            foreach (var relativeTile in relativeTiles)
            {
                Vector3Int checkPosition = baseTilePosition + relativeTile; // 상대 위치 계산
                TileData tileData = TileMapManager.Instance.GetTileData(checkPosition);

                if (tileData == null || !tileData.isAvailable) // 타일맵 외부이거나 배치 불가능
                {
                    TileMapManager.Instance.SetTileColors(
                        checkPosition,
                        new List<Vector3Int> { Vector3Int.zero },
                        new Color(1, 0, 0, 0.5f) // 빨간색
                    );
                }
                else // 타일 배치 가능
                {
                    TileMapManager.Instance.SetTileColors(
                        checkPosition,
                        new List<Vector3Int> { Vector3Int.zero },
                        new Color(0, 1, 0, 0.5f) // 초록색
                    );
                }
            }

            previousTilePosition = baseTilePosition;
        }
    }


    public void TESTOnPointerUp()
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0)); // 타일 색 초기화

        if (TileMapManager.Instance.AreTilesAvailable(previousTilePosition, relativeTiles))
        {
            // 배치 확정
            string tileUniqueID = Guid.NewGuid().ToString();    

            TileMapManager.Instance.SetTilesUnavailable(previousTilePosition, relativeTiles, tileUniqueID);
            transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);

            //타워 생성코드
            CreatePlacedObject(tileUniqueID);

            //TODO : 골드 차감 코드
            isPlaced = true;
            Debug.Log("타워 배치 완료!");
        }
        else
        {
            // 배치 불가능한 경우 원래 위치로 복귀
            Debug.Log("배치 불가 지역!");
            transform.position = originalPos;
            spriteRenderer.color = originColor;
            isPlaced = false;

            //원래 위치로 돌아갔을때 카드가 보여야됨
        }
    }

    public void DecreaseGold(int amount)
    {
        //testGold = userData.f_Gold;

        //if (testGold >= amount)
        //{
        //    testGold -= amount;
        //    userData.f_Gold = testGold;

        //    // 데이터 저장
        //    SaveLoadManager.Instance.SaveData();
        //}
        //else
        //{
        //    Debug.Log("골드가 부족합니다!");
        //}
    }


    private void CreatePlacedObject(string uniqueID)
    {
        // 타워의 상대적 위치 데이터를 기반으로 프리팹을 여러 개 생성
        for (int i = 0; i < relativeTiles.Count; i++)
        {

            Vector3Int relativeTile = relativeTiles[i];
            // 상대적 위치를 기준으로 절대 타일 좌표 계산
            Vector3Int tilePosition = previousTilePosition + relativeTile;

            // 프리팹 생성 및 위치 설정
            GameObject placedObjectInstance = ResourceManager.Instance.Instantiate(prefabKey);

            // PlacedObject 스크립트 가져오기
            PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();

            if (placedObject != null)
            {
                placedObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
                placedObject.RegistereTileID(uniqueID);
                placedObject.InitializeUnitStat(tileShapeName, i);
            }

            // 타일맵에 배치된 오브젝트 등록
            TileMapManager.Instance.SetTileUnavailable(tilePosition);
        }

        // 드래그 오브젝트 제거
        Destroy(gameObject);
    }

}
