using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DragObject : StaticObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    //test용 변수
    public testGold test;
    private int testGold;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3 originalPos;

    D_UserData userData;

    //test용 awake
    private void Awake()
    {
        Initialize();
    }


    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // 유효하지 않는 값으로 초기화
        originalPos = transform.position;
        originColor = spriteRenderer.color;

        //test
        // 타워의 상대 타일 크기 초기화 (예: 1x2 크기)
        relativeTiles = new List<Vector3Int>
        {
            new Vector3Int(0, 0, 0), // 기준 타일
            new Vector3Int(0, 1, 0)  // 아래 타일
        };

    }

    void Start()
    {
        userData = D_UserData.GetEntity(0);

        if (userData != null)
        {
            testGold = userData.f_Gold;
            Debug.Log($"현재 골드 : {testGold}");
        }
    }

    public override void Update()
    {
        base.Update();

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치를 타일맵 셀 좌표로 변환
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;
        Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        // 드래그 중 투명도 조정
        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;

        // 타일 색상 갱신
        if (tilePosition != previousTilePosition)
        {
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f)); // 이전 타일 초기화

            // 각 타일별 배치 가능 여부에 따라 색상 설정
            foreach (var relativeTile in relativeTiles)
            {
                Vector3Int checkPosition = tilePosition + relativeTile;

                if (TileMapManager.Instance.GetTileData(checkPosition)?.isAvailable == true)
                {
                    TileMapManager.Instance.SetTileColors(checkPosition, new List<Vector3Int> { Vector3Int.zero }, new Color(0, 1, 0, 0.5f)); // 초록색
                }
                else
                {
                    TileMapManager.Instance.SetTileColors(checkPosition, new List<Vector3Int> { Vector3Int.zero }, new Color(1, 0, 0, 0.5f)); // 빨간색
                }
            }

            previousTilePosition = tilePosition;
        }

        // 오브젝트 위치 갱신
        transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
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
        testGold = userData.f_Gold;

        if (testGold >= amount)
        {
            testGold -= amount;
            userData.f_Gold = testGold;

            // 데이터 저장
            SaveLoadManager.Instance.SaveData();
        }
        else
        {
            Debug.Log("골드가 부족합니다!");
        }
    }


    private void CreatePlacedObject()
    {
        //타워에 따라 다른 프리팹 생성해야되는데 어드레서블사용해야함
        // PlacedObject 프리팹 생성
        GameObject placedObjectPrefab = Resources.Load<GameObject>("Prefabs/Tower/1x2TowerObject"); // PlacedObject 프리팹 로드
        GameObject placedObjectInstance = Instantiate(placedObjectPrefab);

        // PlacedObject 스크립트 가져오기
        PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();

        // PlacedObject 초기화
        placedObject.InitializeObj(previousTilePosition, relativeTiles);


        // 현재 DragObject 비활성화 또는 삭제
        Destroy(gameObject);
    }

}
