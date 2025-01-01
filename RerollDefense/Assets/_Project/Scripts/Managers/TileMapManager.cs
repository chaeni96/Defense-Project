using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{
    public static TileMapManager _instance;

    public Tilemap tileMap;

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

    // 특정 위치의 타일 데이터 가져오기
    public TileData GetTileData(Vector3Int position)
    {
        return tileMapDatas.TryGetValue(position, out TileData data) ? data : null;
    }

    // 지정된 위치에 오브젝트 배치가능 여부
    public bool CanPlaceObjectAt(Vector3Int basePosition, List<Vector3Int> relativeTiles)
    {
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPosition = basePosition + relativeTile;
            TileData tileData = GetTileData(checkPosition);

            if (tileData == null || !tileData.isAvailable)
            {
                return false; // 하나라도 불가능하면 전체 불가능
            }
        }
        return true; // 모두 가능하면 true
    }

    // 타일 점유상태로 변경
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


    
}
