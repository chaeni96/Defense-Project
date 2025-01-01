using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public static TileMapManager _instance;

    public Tilemap tileMap;

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
    public void InitializeManager(Tilemap gaameTileMap)
    {

        if (tileMap == null)
        {
            tileMap = gaameTileMap;
        }

        tileMapDatas.Clear();
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMapDatas[position] = new TileData();
            }
        }
        SetAllTilesColor(new Color(1, 1, 1, 0));
    }

    public void SetTileData(Vector3Int position, bool isAvailable, string uniqueID = "")
    {
        if (tileMapDatas.TryGetValue(position, out TileData tileData))
        {
            tileData.isAvailable = isAvailable;
            tileData.tileUniqueID = uniqueID;
            Debug.Log($"Tile at {position} updated: isAvailable = {isAvailable}, uniqueID = {uniqueID}");
        }
        else
        {
            Debug.LogWarning($"Tile at {position} does not exist in TileMapManager.");
        }
    }

    // Ư�� ��ġ�� Ÿ�� ������ ��������
    public TileData GetTileData(Vector3Int position)
    {
        return tileMapDatas.TryGetValue(position, out TileData data) ? data : null;
    }

    // ������ ��ġ�� ������Ʈ ��ġ���� ����
    public bool CanPlaceObjectAt(Vector3Int basePosition, List<Vector3Int> relativeTiles)
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

    // Ÿ�� �������·� ����
    public void OccupyTile(Vector3Int basePosition, List<Vector3Int> relativeTiles, string uniqueID)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int updatePosition = basePosition + relativeTile;

            if (tileMapDatas.ContainsKey(updatePosition))
            {
                tileMapDatas[updatePosition].isAvailable = false;
                tileMapDatas[updatePosition].tileUniqueID = uniqueID;
            }
        }
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


    
}
