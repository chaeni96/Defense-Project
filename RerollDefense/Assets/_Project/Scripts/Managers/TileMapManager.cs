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
        InstallTileMap(obstacleData);

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
    public void OccupyTile(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int updatePosition = basePosition + relativeTile;
            SetTileData(updatePosition, false);
        }
    }

    //�巡�� ���� ������Ʈ ��ġ �������� üũ�ϴ� �޼���
    //����Ÿ�ϰ� ��Ÿ�� ��ġ��ȵ�
    //���Ͱ� �����ִ� ��� ���� ������ �ȵ�
    public bool CanPlaceObject(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        // �⺻ Ÿ�� ���� ���� Ȯ��
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPosition = basePosition + relativeTile;
            TileData tileData = GetTileData(checkPosition);

            if (tileData == null || !tileData.isAvailable)
                return false;
        }

        //����Ÿ�� ��Ÿ�� Ȯ��
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPos = basePosition + relativeTile;
            if (checkPos == startTilePosition || checkPos == endTilePosition)
                return false;
        }

        // �ӽ÷� Ÿ�� ������ ����� ��ųʸ�
        Dictionary<Vector3Int, TileData> originalTileData = new Dictionary<Vector3Int, TileData>();

        // ��ġ�Ϸ��� Ÿ�ϵ��� ���� ���� ���� �� �ӽ÷� ���� ���·� ����
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

        // ���� �������� �� ���������� ��� üũ
        List<Vector3> tempPath = PathFindingManager.Instance.FindPath(startTilePosition, endTilePosition);

        if (tempPath.Count == 0)
        {
            canPlace = false;
        }

        // ���� �����ϴ� ��� ���ʹ̵��� ��ġ���� �� ���������� ��� üũ
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

        // Ÿ�� ���� ����
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


    // Ư�� Ÿ�� ���� ���� 
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
