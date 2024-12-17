using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DragObject : StaticObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    //test용 변수
    public testGold test;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3 originalPos;



    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // 유효하지 않는 값으로 초기화
        originalPos = transform.position;
        originColor = spriteRenderer.color;

    }

    public override void Update()
    {
        base.Update();

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InitializeTileShape();

        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치를 타일맵 셀 좌표로 변환
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;

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


    public void OnPointerUp(PointerEventData eventData)
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0)); // 타일 색 초기화

        if (TileMapManager.Instance.AreTilesAvailable(previousTilePosition, relativeTiles))
        {
            // 배치 확정
            TileMapManager.Instance.SetTilesUnavailable(previousTilePosition, relativeTiles);
            transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);
            DecreaseGold(10);
            test.UpdateGoldText();

            //타워 생성코드
            CreatePlacedObject();

            Debug.Log("타워 배치 완료!");
        }
        else
        {
            // 배치 불가능한 경우 원래 위치로 복귀
            Debug.Log("배치 불가 지역!");
            transform.position = originalPos;
            spriteRenderer.color = originColor;
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


    private void CreatePlacedObject()
    {
        // 타워의 상대적 위치 데이터를 기반으로 프리팹을 여러 개 생성
        foreach (Vector3Int relativeTile in relativeTiles)
        {
            // 상대적 위치를 기준으로 절대 타일 좌표 계산
            Vector3Int tilePosition = previousTilePosition + relativeTile;

            // 프리팹 생성 및 위치 설정
            GameObject placedObjectInstance = ResourceManager.Instance.Instantiate(prefabKey);
            if (placedObjectInstance == null)
            {
                Debug.LogError("프리팹 생성 실패");
                continue;
            }

            // PlacedObject 스크립트 가져오기
            PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();
            if (placedObject != null)
            {
                placedObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);

                // 상대적 위치를 개별 오브젝트에 저장 (디버깅 용도)
                placedObject.previousTilePosition = tilePosition;
                placedObject.relativeTiles = new List<Vector3Int>() { Vector3Int.zero }; // 자기 자신만 차지한다고 설정
            }
            else
            {
                Debug.LogError("PlacedObject 스크립트가 프리팹에 없습니다.");
            }

            // 타일맵에 배치된 오브젝트 등록
            TileMapManager.Instance.SetTileUnavailable(tilePosition);
        }

        // 드래그 오브젝트 제거
        Destroy(gameObject);
    }

}
