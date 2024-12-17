using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public static TileMapManager _instance;

    public Tilemap tileMap;

    private Dictionary<Vector3Int, TileData> tileDataMap = new Dictionary<Vector3Int, TileData>();

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

        setTileMap();
    }

    //����Ÿ�� ��Ÿ�� ��ǥ�� �˾Ƴ������� �׽�Ʈ�� ������Ʈ
    //void Update()
    //{
    //    // ���콺 Ŭ�� �� Ÿ�ϸ� ��ǥ ���
    //    if (Input.GetMouseButtonDown(0)) // ��Ŭ��
    //    {
    //        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        Vector3Int tilePosition = tileMap.WorldToCell(mouseWorldPosition);

    //        Debug.Log($"Tile Position: {tilePosition}");
    //    }
    //}


    // Ÿ�ϸ� �ʱ�ȭ
    public void setTileMap()
    {
        // Ÿ�� ������ �ʱ�ȭ, ��� Ÿ�� ��ġ ���� ���·� ����
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileDataMap[position] = new TileData();
            }
        }

        ResetTileColors(new Color(1, 1, 1, 0));
    }

    // Ư�� ��ġ�� Ÿ�� ������ ��������
    public TileData GetTileData(Vector3Int position)
    {
        return tileDataMap.ContainsKey(position) ? tileDataMap[position] : null;
    }

    // ���� Ÿ�� ��ġ ���� ���� Ȯ��
    public bool AreTilesAvailable(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPosition = basePosition + relativeTile;
            TileData tileData = GetTileData(checkPosition);

            if (tileData == null || !tileData.isAvailable)
            {
                return false; // �ϳ��� �Ұ����ϸ� ��ü �Ұ���
            }
        }
        return true; // ��� �����ϸ� true
    }

    // ���� Ÿ���� ��ġ �Ұ��� ���·� ����
    public void SetTilesUnavailable(Vector3Int basePosition, List<Vector3Int> relativeTiles, string uniqueID)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int updatePosition = basePosition + relativeTile;

            if (tileDataMap.ContainsKey(updatePosition))
            {
                tileDataMap[updatePosition].isAvailable = false;
                tileDataMap[updatePosition].tileUniqueID = uniqueID;
            }
        }
    }

    // ���� Ÿ�� ���� ���� (��: ��ġ ����/�Ұ��� �ð�ȭ)
    public void SetTileColors(Vector3Int basePosition, List<Vector3Int> relativeTiles, Color color)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int tilePosition = basePosition + relativeTile;

            if (tileMap.HasTile(tilePosition))
            {
                tileMap.SetTileFlags(tilePosition, TileFlags.None);
                tileMap.SetColor(tilePosition, color);
            }
        }
    }


    // ��� Ÿ�� ���� �ʱ�ȭ
    public void ResetTileColors(Color color)
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

    // Ư�� Ÿ�� ��ġ �Ұ��� ���·� ���� (���� Ÿ�Ͽ�)
    public void SetTileUnavailable(Vector3Int position)
    {
        if (tileDataMap.ContainsKey(position))
        {
            tileDataMap[position].isAvailable = false;
        }
    }


    // Ÿ�Ͽ� ��ġ�� ������Ʈ�� ���
    public void RegisterObjectOnTiles(Vector3Int basePosition, List<Vector3Int> relativeTiles, PlacedObject placedObject)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int tilePosition = basePosition + relativeTile;

            if (tileDataMap.ContainsKey(tilePosition))
            {
                tileDataMap[tilePosition].isOccupied = true;
                tileDataMap[tilePosition].occupyingObject = placedObject;
            }
        }
    }
}
