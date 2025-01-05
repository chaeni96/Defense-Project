using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    //타일 배치 관련 메서드만

    public static TileMapManager _instance;

    public Tilemap tileMap;

    private D_ObstacleTileMapData obstacleData;
    private D_MapData mapData;
    private Transform tileMapGrid;

    [SerializeField] private Vector3Int startTilePosition;
    [SerializeField] private Vector3Int endTilePosition;


    // 각 타일의 상태 정보를 저장하는 딕셔너리
    private Dictionary<Vector3Int, TileData> tileMapDatas = new Dictionary<Vector3Int, TileData>();

    public static TileMapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TileMapManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("TileMapManager");
                    _instance = singleton.AddComponent<TileMapManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    //타일맵 초기화
    public void InitializeManager(Tilemap gameMap, D_ObstacleTileMapData obstacleMapData, Transform grid)
    {
        tileMap = gameMap;
        obstacleData = obstacleMapData;
        tileMapGrid = grid;
        tileMapDatas.Clear();

    }
    public void InitializeManager(Tilemap gameMap, D_MapData mapData, Transform grid)
    {
        tileMap = gameMap;
        this.mapData = mapData;
        tileMapGrid = grid;
        tileMapDatas.Clear();

    }

    //endTile에 playerCamp 설치
    public void InitializeTiles(Vector2 startTile, Vector2 endTile)
    {
        //startTile, endTile 지정
        startTilePosition = new Vector3Int(Mathf.FloorToInt(startTile.x), Mathf.FloorToInt(startTile.y), 0);
        endTilePosition = new Vector3Int(Mathf.FloorToInt(endTile.x), Mathf.FloorToInt(endTile.y), 0);

        //endTile에 playerCamp 설치
        GameObject playerCamp = ResourceManager.Instance.Instantiate("PlayerCamp");
        playerCamp.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(endTilePosition);

        //tileMap 설치
        //InstallTileMap(obstacleData);
        InstallTileMap(mapData);

        SetAllTilesColor(new Color(1, 1, 1, 0));
    }

    public void InstallTileMap(D_ObstacleTileMapData obstacleMap)
    {
        // 기본적으로 모든 타일을 먼저 사용 가능한 상태로 초기화
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMapDatas[position] = new TileData { isAvailable = true };
            }
        }

        // 이후 장애물 로직 처리
        if (obstacleMap != null)
        {
            GameObject obstacleMapPrefab = ResourceManager.Instance.Instantiate(obstacleData.f_ObstacleAddressableKey, tileMapGrid);
            Tilemap obstacleTileMap = obstacleMapPrefab.GetComponent<Tilemap>();

            var obstacleType = obstacleMap.f_ObstacleTileType;

            switch (obstacleType)
            {
                case ObstacleTileType.Basic:
                    foreach (var position in tileMap.cellBounds.allPositionsWithin)
                    {
                        if (tileMap.HasTile(position) && obstacleTileMap.HasTile(position))
                        {
                            tileMapDatas[position].isAvailable = false;
                        }
                    }
                    break;
            }
        }
    }
    public void InstallTileMap(D_MapData mapData)
    {
        // 기본적으로 모든 타일을 먼저 사용 가능한 상태로 초기화
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMapDatas[position] = new TileData { isAvailable = true };
            }
        }
        foreach(var tileData in mapData.f_specialTiles)
        {
            //타일 데이터 가져와서 장애물 설치
            var newObjectCellPos = new Vector3Int(tileData.f_cellXPos, tileData.f_cellYPos, 0);
            var newObjectPos = tileMap.GetCellCenterWorld(newObjectCellPos);
            var newSpecialObject = PoolingManager.Instance.GetObject(tileData.f_specialObject.f_name, newObjectPos);
            var objectController = newSpecialObject.GetComponent<UnitController>();
            UnitManager.Instance.RegisterUnit(objectController);
            SetTileData(newObjectCellPos, false);
        }
    }
    public Vector3Int GetStartTilePosition() => startTilePosition; //시작 타일 가져오기
    public Vector3Int GetEndTilePosition() => endTilePosition; //끝 타일 가져오기


    //타일 데이터 넣어주기
    public void SetTileData(Vector3Int position, bool isAvailable)
    {
        if (tileMapDatas.TryGetValue(position, out TileData tileData))
        {

            // 해당 위치의 타일 데이터가 이미 존재하는 경우
            tileData.isAvailable = isAvailable;
        }
        else
        {
            // 해당 위치에 TileData가 없다면 새로 생성
            tileMapDatas[position] = new TileData
            {
                isAvailable = isAvailable
            };
        }
    }

    // 특정 위치의 타일 데이터 가져오기
    public TileData GetTileData(Vector3Int position)
    {
        return tileMapDatas.TryGetValue(position, out TileData data) ? data : null;
    }

    // 타일 점유상태로 변경
    // 오브젝트를 실제 배치할때 사용
    // 상대적인 타일 위치들을 기준위치에 더해서 처리
    public void OccupyTile(Vector3Int basePosition, List<Vector3Int> tileOffsets)
    {
        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int updatePosition = basePosition + tileOffset;
            SetTileData(updatePosition, false);
        }
    }

    //드래그 도중 오브젝트 배치 가능한지 체크하는 메서드
    public bool CanPlaceObject(Vector3Int basePosition, List<Vector3Int> tileOffsets)
    {
        // 1. 기본 검사 : 시작/끝타일과 겹치는지, tileMap안에 있는지 체크
        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int checkPosition = basePosition + tileOffset;

            if (checkPosition == startTilePosition || checkPosition == endTilePosition || !IsTileAvailable(checkPosition))
            {
                return false;
            }
        }

        // 2. 유닛 배치하기 전에 그 위치에 임시 배치했을때, 모든 enemy들이 목적지까지 도달할수있는지 체크
        
        List<Vector3Int> occupiedTiles = new List<Vector3Int>();

        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int checkPos = basePosition + tileOffset;
            occupiedTiles.Add(checkPos);
            SetTileData(checkPos, false);  // 임시로 모든 타일 점유상태 변경
        }

        bool canPlace = true;

        HashSet<Vector3Int> pointsToCheck = new HashSet<Vector3Int>();

        // 3. 시작 지점 추가
        pointsToCheck.Add(startTilePosition);

        // 4. 현재 존재하는 적들의 위치 추가
        if (EnemyManager.Instance != null)
        {
            var enemies = EnemyManager.Instance.GetAllEnemys();

            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Vector3Int enemyTilePos = tileMap.WorldToCell(enemy.transform.position);
                    pointsToCheck.Add(enemyTilePos);
                }
            }
        }

        // 5. 모든 시작점에서 끝점까지의 경로 확인
        foreach (var startPoint in pointsToCheck)
        {
            if (!PathFindingManager.Instance.HasValidPath(startPoint, endTilePosition))
            {
                canPlace = false;
                break;
            }
        }

        // 6. 타일 상태 복원
        foreach (var position in occupiedTiles)
        {
            SetTileData(position, true);
        }

        return canPlace;
    }

    // 타일 사용 가능 여부 확인 메서드
    private bool IsTileAvailable(Vector3Int position)
    {
        var tileData = GetTileData(position);
        return tileData != null && tileData.isAvailable;
    }

    // 특정 타일 색상 변경 
    public void SetTileColors(Vector3Int basePosition, List<Vector3Int> tileOffsets, Color color)
    {
        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int position = basePosition + tileOffset;
            if (tileMap.HasTile(position))
            {
                tileMap.SetTileFlags(position, TileFlags.None);
                tileMap.SetColor(position, color);
            }
        }
    }


    // 모든 타일 색상 변경
    public void SetAllTilesColor(Color color)
    {
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMap.SetTileFlags(position, TileFlags.None);
                tileMap.SetColor(position, color);
            }
        }
    }

    //시작 타일, 도착타일 pos
    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0)) // 마우스 왼쪽 버튼 클릭
    //    {
    //        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        Vector3Int tilePosition = tileMap.WorldToCell(worldPosition);

    //        if (tileMap.HasTile(tilePosition))
    //        {
    //            Debug.Log($"Clicked Tile Position: {tilePosition}");
    //        }
    //        else
    //        {
    //            Debug.Log("No tile found at clicked position.");
    //        }
    //    }
    //}
}
