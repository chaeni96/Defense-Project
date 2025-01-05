using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    //Ÿ�� ��ġ ���� �޼��常

    public static TileMapManager _instance;

    public Tilemap tileMap;

    private D_ObstacleTileMapData obstacleData;
    private D_MapData mapData;
    private Transform tileMapGrid;

    [SerializeField] private Vector3Int startTilePosition;
    [SerializeField] private Vector3Int endTilePosition;


    // �� Ÿ���� ���� ������ �����ϴ� ��ųʸ�
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

    //Ÿ�ϸ� �ʱ�ȭ
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

    //endTile�� playerCamp ��ġ
    public void InitializeTiles(Vector2 startTile, Vector2 endTile)
    {
        //startTile, endTile ����
        startTilePosition = new Vector3Int(Mathf.FloorToInt(startTile.x), Mathf.FloorToInt(startTile.y), 0);
        endTilePosition = new Vector3Int(Mathf.FloorToInt(endTile.x), Mathf.FloorToInt(endTile.y), 0);

        //endTile�� playerCamp ��ġ
        GameObject playerCamp = ResourceManager.Instance.Instantiate("PlayerCamp");
        playerCamp.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(endTilePosition);

        //tileMap ��ġ
        //InstallTileMap(obstacleData);
        InstallTileMap(mapData);

        SetAllTilesColor(new Color(1, 1, 1, 0));
    }

    public void InstallTileMap(D_ObstacleTileMapData obstacleMap)
    {
        // �⺻������ ��� Ÿ���� ���� ��� ������ ���·� �ʱ�ȭ
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMapDatas[position] = new TileData { isAvailable = true };
            }
        }

        // ���� ��ֹ� ���� ó��
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
        // �⺻������ ��� Ÿ���� ���� ��� ������ ���·� �ʱ�ȭ
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMapDatas[position] = new TileData { isAvailable = true };
            }
        }
        foreach(var tileData in mapData.f_specialTiles)
        {
            //Ÿ�� ������ �����ͼ� ��ֹ� ��ġ
            var newObjectCellPos = new Vector3Int(tileData.f_cellXPos, tileData.f_cellYPos, 0);
            var newObjectPos = tileMap.GetCellCenterWorld(newObjectCellPos);
            var newSpecialObject = PoolingManager.Instance.GetObject(tileData.f_specialObject.f_name, newObjectPos);
            var objectController = newSpecialObject.GetComponent<UnitController>();
            UnitManager.Instance.RegisterUnit(objectController);
            SetTileData(newObjectCellPos, false);
        }
    }
    public Vector3Int GetStartTilePosition() => startTilePosition; //���� Ÿ�� ��������
    public Vector3Int GetEndTilePosition() => endTilePosition; //�� Ÿ�� ��������


    //Ÿ�� ������ �־��ֱ�
    public void SetTileData(Vector3Int position, bool isAvailable)
    {
        if (tileMapDatas.TryGetValue(position, out TileData tileData))
        {

            // �ش� ��ġ�� Ÿ�� �����Ͱ� �̹� �����ϴ� ���
            tileData.isAvailable = isAvailable;
        }
        else
        {
            // �ش� ��ġ�� TileData�� ���ٸ� ���� ����
            tileMapDatas[position] = new TileData
            {
                isAvailable = isAvailable
            };
        }
    }

    // Ư�� ��ġ�� Ÿ�� ������ ��������
    public TileData GetTileData(Vector3Int position)
    {
        return tileMapDatas.TryGetValue(position, out TileData data) ? data : null;
    }

    // Ÿ�� �������·� ����
    // ������Ʈ�� ���� ��ġ�Ҷ� ���
    // ������� Ÿ�� ��ġ���� ������ġ�� ���ؼ� ó��
    public void OccupyTile(Vector3Int basePosition, List<Vector3Int> tileOffsets)
    {
        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int updatePosition = basePosition + tileOffset;
            SetTileData(updatePosition, false);
        }
    }

    //�巡�� ���� ������Ʈ ��ġ �������� üũ�ϴ� �޼���
    public bool CanPlaceObject(Vector3Int basePosition, List<Vector3Int> tileOffsets)
    {
        // 1. �⺻ �˻� : ����/��Ÿ�ϰ� ��ġ����, tileMap�ȿ� �ִ��� üũ
        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int checkPosition = basePosition + tileOffset;

            if (checkPosition == startTilePosition || checkPosition == endTilePosition || !IsTileAvailable(checkPosition))
            {
                return false;
            }
        }

        // 2. ���� ��ġ�ϱ� ���� �� ��ġ�� �ӽ� ��ġ������, ��� enemy���� ���������� �����Ҽ��ִ��� üũ
        
        List<Vector3Int> occupiedTiles = new List<Vector3Int>();

        foreach (var tileOffset in tileOffsets)
        {
            Vector3Int checkPos = basePosition + tileOffset;
            occupiedTiles.Add(checkPos);
            SetTileData(checkPos, false);  // �ӽ÷� ��� Ÿ�� �������� ����
        }

        bool canPlace = true;

        HashSet<Vector3Int> pointsToCheck = new HashSet<Vector3Int>();

        // 3. ���� ���� �߰�
        pointsToCheck.Add(startTilePosition);

        // 4. ���� �����ϴ� ������ ��ġ �߰�
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

        // 5. ��� ���������� ���������� ��� Ȯ��
        foreach (var startPoint in pointsToCheck)
        {
            if (!PathFindingManager.Instance.HasValidPath(startPoint, endTilePosition))
            {
                canPlace = false;
                break;
            }
        }

        // 6. Ÿ�� ���� ����
        foreach (var position in occupiedTiles)
        {
            SetTileData(position, true);
        }

        return canPlace;
    }

    // Ÿ�� ��� ���� ���� Ȯ�� �޼���
    private bool IsTileAvailable(Vector3Int position)
    {
        var tileData = GetTileData(position);
        return tileData != null && tileData.isAvailable;
    }

    // Ư�� Ÿ�� ���� ���� 
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


    // ��� Ÿ�� ���� ����
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

    //���� Ÿ��, ����Ÿ�� pos
    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0)) // ���콺 ���� ��ư Ŭ��
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
