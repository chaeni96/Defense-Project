using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TileMapManager : MonoBehaviour
{
    //Ÿ�� ��ġ ���� �޼��常

    public static TileMapManager _instance;

    public Tilemap tileMap;

    private D_MapData mapData;
    private Transform tileMapGrid;

    [SerializeField] private Vector2 startTilePos;
    [SerializeField] private Vector2 endTilePos;


    // �� Ÿ���� ���� ������ �����ϴ� ��ųʸ�, ��ǥ�� Ű������
    private Dictionary<Vector2, TileData> tileMapDatas;

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
 
    public void InitializeManager(Tilemap gameMap, D_MapData mapData, Transform grid)
    {
        // ���� ������ ����
        CleanUp();

        // ���� �ʱ�ȭ
        tileMapDatas = new Dictionary<Vector2, TileData>();
        tileMap = gameMap;
        this.mapData = mapData;
        tileMapGrid = grid;

    }

    //endTile�� playerCamp ��ġ
    public void InitializeTiles(Vector2 startTile, Vector2 endTile)
    {
        //startTile, endTile ����
        startTilePos = startTile;
        endTilePos = endTile;

        //tileMap ��ġ
        InstallTileMap(mapData);

        SetAllTilesColor(new Color(1, 1, 1, 0));
    }

    public void InstallTileMap(D_MapData mapData)
    {
        // �⺻������ ��� Ÿ���� ���� ��� ������ ���·� �ʱ�ȭ
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                var tileData = new TileData(new Vector2(position.x, position.y));
                SetTileData(tileData);
            }
        }
        foreach(var specialTile in mapData.f_specialTiles)
        {
            //Ÿ�� ������ �����ͼ� ��ֹ� ��ġ
      
            Vector2 position = specialTile.f_cellPosition;

            Vector3 newObjectPos = GetTileToWorldPosition(position);

            var newSpecialObject = PoolingManager.Instance.GetObject(specialTile.f_specialObject.f_UnitPoolingKey.f_PoolObjectAddressableKey, newObjectPos);

            var objectController = newSpecialObject.GetComponent<UnitController>();
            objectController.InitializeUnitInfo(specialTile.f_specialObject, position);
            UnitManager.Instance.RegisterUnit(objectController);


            // Ÿ�� ������ ������Ʈ
            var tileData = new TileData(position)
            {
                isAvailable = false,
                placedUnit = objectController
            };

            SetTileData(tileData);
        }
    }

    //Ÿ�� ��ǥ ���� ��ǥ ��ȯ
    public Vector3 GetTileToWorldPosition(Vector2 pos)
    {
        return tileMap.GetCellCenterWorld(new Vector3Int((int)pos.x, (int)pos.y, 0));
    }

    //���� ��ǥ�� Ÿ�� ��ǥ�� ��ȯ
    public Vector2 GetWorldToTilePosition(Vector3 worldPosition)
    {
        Vector3Int cell = tileMap.WorldToCell(worldPosition);
        return new Vector2(cell.x, cell.y);
    }

    //Ÿ�� ������ �־��ֱ�
    public void SetTileData(TileData tileData)
    {
        tileMapDatas[new Vector2(tileData.tilePosX, tileData.tilePosY)] = tileData;
    }

    // Ư�� ��ġ�� Ÿ�� ������ ��������, ��ǥ������ ������
    public TileData GetTileData(Vector2 position)
    {
        return tileMapDatas.TryGetValue((position), out var data) ? data : null;
    }

    // Ÿ�� �������·� ����
    // ������Ʈ�� ���� ��ġ�Ҷ� ���
    // ������� Ÿ�� ��ġ���� ������ġ�� ���ؼ� ó��
    public void OccupyTiles(Vector2 basePosition, List<Vector2> tileOffsets, Dictionary<int, UnitController> units)
    {
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 position = basePosition + tileOffsets[i];

            // �� ������ ���ֿ� �´� Ÿ�� ������ ����
            var tileData = new TileData(position)
            {
                isAvailable = false,
                placedUnit = units[i]
            };

            SetTileData(tileData);
        }
    }


    //�巡�� ���� ������Ʈ ��ġ �������� üũ�ϴ� �޼���
    public bool CanPlaceObject(Vector2 basePosition, List<Vector2> tileOffsets, Dictionary<int, UnitController> previewUnits)
    {

        // �⺻ �˻� : ����/��Ÿ�ϰ� ��ġ����, tileMap�ȿ� �ִ��� üũ
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 checkPosition = basePosition + tileOffsets[i];
            var tileData = GetTileData(checkPosition);

            // ����/�� Ÿ�� üũ
            if (checkPosition == startTilePos || checkPosition == endTilePos)
            {
                return false;
            }

            if (tileData == null || !tileMap.HasTile(new Vector3Int((int)checkPosition.x, (int)checkPosition.y, 0)))
            {
                return false;
            }

            // ��ġ�� ������ �ִ� ��� Ÿ�� ��
            if (tileData?.placedUnit != null)
            {
                var placedUnit = tileData.placedUnit;
                var previewUnit = previewUnits[i];

                // ���׷��̵� Ÿ���� �ٸ��� ��ġ �Ұ�
                if (previewUnit.upgradeUnitType != placedUnit.upgradeUnitType || placedUnit.unitData.f_NextLevelUnit == null)
                {
                    return false;
                }
            }
        }

        // ���� ��ġ�ϱ� ���� �� ��ġ�� �ӽ� ��ġ������, ��� enemy���� ���������� �����Ҽ��ִ��� üũ

        Dictionary<Vector2, bool> originalStates = new Dictionary<Vector2, bool>();

        // ���� ��ġ�� Ÿ�ϵ��� �ӽ÷� ����
        foreach (var offset in tileOffsets)
        {
            Vector2 checkPos = basePosition + offset;
            var tileData = GetTileData(checkPos);
            if (tileData != null)
            {
                originalStates[checkPos] = tileData.isAvailable;
                tileData.isAvailable = false;
                SetTileData(tileData);
            }
        }

        bool canPlace = true;

        try
        {
            // ���� �����ϴ� ������ ��ġ üũ -> ���������� ��� ���� ���� ��ġ���� ��� Ȯ��
            HashSet<Vector2> checkPoints = new HashSet<Vector2> { startTilePos };

            if (EnemyManager.Instance != null)
            {
                var enemies = EnemyManager.Instance.GetAllEnemys();
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        Vector2 enemyPos = GetWorldToTilePosition(enemy.transform.position);
                        // ���� ���� ��ġ�Ϸ��� Ÿ�� ���� �ִٸ� �ش� ��ġ�� üũ���� ����
                        if (!tileOffsets.Any(offset => enemyPos == basePosition + offset))
                        {
                            checkPoints.Add(enemyPos);
                        }
                    }
                }
            }

            // ��� üũ����Ʈ���� ��� Ȯ��
            foreach (var point in checkPoints)
            {
                if (!PathFindingManager.Instance.HasValidPath(point, endTilePos))
                {
                    canPlace = false;
                    break;
                }
            }
        }
        finally
        {
            // ���� ���·� ����
            foreach (var kvp in originalStates)
            {
                var tileData = GetTileData(new Vector2(kvp.Key.x, kvp.Key.y));
                if (tileData != null)
                {
                    tileData.isAvailable = kvp.Value;
                    SetTileData(tileData);
                }
            }
        }
            
        return canPlace;
    }


    // Ư�� Ÿ�� ���� ���� 
    public void SetTileColors(int baseX, int baseY, List<(int x, int y)> offsets, Color color)
    {
        foreach (var offset in offsets)
        {
            int x = baseX + offset.x;
            int y = baseY + offset.y;

            Vector3Int position = new Vector3Int(x, y, 0);
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

    public Vector2 GetStartPosition() => startTilePos;
    public Vector2 GetEndPosition() => endTilePos;

    private void CleanUp()
    {
        // Dictionary ����
        if (tileMapDatas != null)
        {
            tileMapDatas.Clear();
        }

        // ���� ������ ����
        tileMap = null;
        mapData = null;
        tileMapGrid = null;

        // ����/�� Ÿ�� ��ġ �ʱ�ȭ
        startTilePos = Vector2.zero;
        endTilePos = Vector2.zero;
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
