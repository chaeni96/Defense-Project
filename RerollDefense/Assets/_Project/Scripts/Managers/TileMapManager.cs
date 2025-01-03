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
        InstallTileMap(obstacleData);

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
    public void OccupyTile(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int updatePosition = basePosition + relativeTile;
            SetTileData(updatePosition, false);
        }
    }

    //드래그 도중 오브젝트 배치 가능한지 체크하는 메서드
    //시작타일과 끝타일 겹치면안됨
    //몬스터가 갈수있는 모든 길을 막으면 안됨
    public bool CanPlaceObject(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        // 기본 타일 점유 상태 확인
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPosition = basePosition + relativeTile;
            TileData tileData = GetTileData(checkPosition);

            if (tileData == null || !tileData.isAvailable)
                return false;
        }

        //시작타일 끝타일 확인
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPos = basePosition + relativeTile;
            if (checkPos == startTilePosition || checkPos == endTilePosition)
                return false;
        }

        // 임시로 타일 데이터 저장용 딕셔너리
        Dictionary<Vector3Int, TileData> originalTileData = new Dictionary<Vector3Int, TileData>();

        // 설치하려는 타일들의 현재 상태 저장 및 임시로 점유 상태로 변경
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPos = basePosition + relativeTile;
            TileData tileData = GetTileData(checkPos);
            if (tileData != null)
            {
                originalTileData[checkPos] = new TileData
                {
                    isAvailable = tileData.isAvailable,
                };
                tileData.isAvailable = false;
            }
        }

        bool canPlace = true;

        // 시작 지점에서 끝 지점까지의 경로 체크
        List<Vector3> tempPath = PathFindingManager.Instance.FindPath(startTilePosition, endTilePosition);

        if (tempPath.Count == 0)
        {
            canPlace = false;
        }

        // 현재 존재하는 모든 에너미들의 위치에서 끝 지점까지의 경로 체크
        if (canPlace && EnemyManager.Instance != null)
        {
            var enemies = EnemyManager.Instance.GetAllEnemys();
            foreach (var enemy in enemies)
            {
                if (enemy != null)
                {
                    Vector3Int enemyTilePos = TileMapManager.Instance.tileMap.WorldToCell(enemy.transform.position);

                    tempPath = PathFindingManager.Instance.FindPath(enemyTilePos, endTilePosition);

                    if (tempPath.Count == 0)
                    {
                        canPlace = false;
                        break;
                    }
                }
            }
        }

        // 타일 상태 복원
        foreach (var kvp in originalTileData)
        {
            TileData tileData = GetTileData(kvp.Key);
            if (tileData != null)
            {
                tileData.isAvailable = kvp.Value.isAvailable;
            }
        }

        return canPlace;
    }


    // 특정 타일 색상 변경 
    public void SetTileColors(Vector3Int basePosition, List<Vector3Int> relativeTiles, Color color)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int position = basePosition + relativeTile;
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
