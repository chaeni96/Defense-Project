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

    //시작타일 끝타일 좌표값 알아내기위한 테스트용 업데이트
    //void Update()
    //{
    //    // 마우스 클릭 시 타일맵 좌표 출력
    //    if (Input.GetMouseButtonDown(0)) // 좌클릭
    //    {
    //        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        Vector3Int tilePosition = tileMap.WorldToCell(mouseWorldPosition);

    //        Debug.Log($"Tile Position: {tilePosition}");
    //    }
    //}


    // 타일맵 초기화
    public void setTileMap()
    {
        // 타일 데이터 초기화, 모든 타일 배치 가능 상태로 설정
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileDataMap[position] = new TileData();
            }
        }

        ResetTileColors(new Color(1, 1, 1, 0));
    }

    // 특정 위치의 타일 데이터 가져오기
    public TileData GetTileData(Vector3Int position)
    {
        return tileDataMap.ContainsKey(position) ? tileDataMap[position] : null;
    }

    // 다중 타일 배치 가능 여부 확인
    public bool AreTilesAvailable(Vector3Int basePosition, List<Vector3Int> relativeTiles)
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

    // 다중 타일을 배치 불가능 상태로 설정
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

    // 다중 타일 색상 설정 (예: 배치 가능/불가능 시각화)
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


    // 모든 타일 색상 초기화
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

    // 특정 타일 배치 불가능 상태로 설정 (단일 타일용)
    public void SetTileUnavailable(Vector3Int position)
    {
        if (tileDataMap.ContainsKey(position))
        {
            tileDataMap[position].isAvailable = false;
        }
    }


    // 타일에 배치된 오브젝트를 등록
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
