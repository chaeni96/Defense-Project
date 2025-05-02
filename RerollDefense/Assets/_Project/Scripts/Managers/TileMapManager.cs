using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TileMapManager : MonoBehaviour
{
    //타일 배치 관련 메서드만

    public static TileMapManager _instance;

    public Tilemap tileMap;

    private D_MapData mapData;

    // 각 타일의 상태 정보를 저장하는 딕셔너리, 좌표를 키값으로
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

    //타일맵 초기화
 
    public void InitializeManager(Tilemap gameMap, D_MapData mapData, Transform grid)
    {
        // 기존 데이터 정리
        CleanUp();

        // 새로 초기화
        tileMapDatas = new Dictionary<Vector2, TileData>();
        tileMap = gameMap;
        this.mapData = mapData;
    }

    // Unity 좌표를 사용자 정의 좌표로 변환 (왼쪽 상단이 0,0)
    public Vector2 ConvertToCustomCoordinates(Vector3Int unityPosition)
    {
        BoundsInt bounds = tileMap.cellBounds;
        // 왼쪽 상단 좌표는 (minX, maxY)
        int customX = unityPosition.x - bounds.min.x;
        int customY = bounds.max.y - 1 - unityPosition.y;
        return new Vector2(customX, customY);
    }

    // 사용자 정의 좌표를 Unity 좌표로 변환
    public Vector3Int ConvertToUnityCoordinates(Vector2 customPosition)
    {
        BoundsInt bounds = tileMap.cellBounds;
        int unityX = (int)customPosition.x + bounds.min.x;
        int unityY = bounds.max.y - 1 - (int)customPosition.y;
        return new Vector3Int(unityX, unityY, 0);
    }

    //endTile에 playerCamp 설치
    public void InitializeTiles()
    {
        //tileMap 설치
        InstallTileMap(mapData);

        SetAllTilesColor(new Color(1, 1, 1, 0));
    }

    public void InstallTileMap(D_MapData mapData)
    {
        // 기본적으로 모든 타일을 먼저 사용 가능한 상태로 초기화
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                Vector2 customPos = ConvertToCustomCoordinates(position);
                var tileData = new TileData(customPos);
                SetTileData(tileData);
            }
        }
    }

    //타일 좌표 월드 좌표 변환
    public Vector3 GetTileToWorldPosition(Vector2 pos)
    {
        Vector3Int unityCoord = ConvertToUnityCoordinates(pos);
        return tileMap.GetCellCenterWorld(unityCoord);
    }

    //월드 좌표를 타일 좌표로 변환
    public Vector2 GetWorldToTilePosition(Vector3 worldPosition)
    {
        Vector3Int unityCell = tileMap.WorldToCell(worldPosition);
        return ConvertToCustomCoordinates(unityCell);
    }

    //타일 데이터 넣어주기
    public void SetTileData(TileData tileData)
    {
        tileMapDatas[new Vector2(tileData.tilePosX, tileData.tilePosY)] = tileData;
    }

    // 특정 위치의 타일 데이터 가져오기, 좌표값으로 얻어오기
    public TileData GetTileData(Vector2 position)
    {
        return tileMapDatas.TryGetValue((position), out var data) ? data : null;
    }

    // 타일 점유상태로 변경
    // 오브젝트를 실제 배치할때 사용
    // 상대적인 타일 위치들을 기준위치에 더해서 처리
    public void OccupyTiles(Vector2 basePosition, List<Vector2> tileOffsets, Dictionary<int, UnitController> units)
    {
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 position = basePosition + tileOffsets[i];

            // 각 유닛의 tilePosition 직접 설정
            units[i].tilePosition = position;
            //units[i].CheckAttackAvailability();

            // 각 프리뷰 유닛에 맞는 타일 데이터 설정
            var tileData = new TileData(position)
            {
                isAvailable = false,
                placedUnit = units[i],
            };

            SetTileData(tileData);
        }
    }


    //드래그 도중 오브젝트 배치 가능한지 체크하는 메서드
    public bool CanPlaceObject(Vector2 basePosition, List<Vector2> tileOffsets, Dictionary<int, UnitController> previewUnits)
    {

        // 기본 검사 : 시작/끝타일과 겹치는지, tileMap안에 있는지 체크
        for (int i = 0; i < tileOffsets.Count; i++)
        {
            Vector2 checkPosition = basePosition + tileOffsets[i];
            var tileData = GetTileData(checkPosition);

            // 사용자 정의 좌표를 Unity 좌표로 변환하여 타일 존재 여부 확인
            Vector3Int unityPos = ConvertToUnityCoordinates(checkPosition);

 
            if (tileData == null || !tileMap.HasTile(unityPos))
            {
                return false;
            }

            // 배치된 유닛이 있는 경우 타입 비교
            if (tileData?.placedUnit != null)
            {

                var placedUnit = tileData.placedUnit;
                var previewUnit = previewUnits[i];

                // 합성 가능 조건 - 같은 타입, 같은 레벨, 최대 3성 미만
                bool canMerge = (previewUnit.unitType == placedUnit.unitType) &&
                               (previewUnit.GetStat(StatName.UnitStarLevel) == placedUnit.GetStat(StatName.UnitStarLevel)) &&
                               (placedUnit.GetStat(StatName.UnitStarLevel) < 5);

                // 합성 불가능하면 배치 불가
                if (!canMerge)
                {
                    return false;
                }
            }
        }
            
        return true;
    }


    // 특정 타일 색상 변경 
    public void SetTileColors(int baseX, int baseY, List<(int x, int y)> offsets, Color color)
    {
        foreach (var offset in offsets)
        {
            int x = baseX + offset.x;
            int y = baseY + offset.y;

            Vector3Int position = ConvertToUnityCoordinates(new Vector2(x, y));

            if (tileMap.HasTile(position))
            {
                tileMap.SetTileFlags(position, TileFlags.None);
                tileMap.SetColor(position, color);
            }
        }
    }
    // 타일에서 제거
    public void ReleaseTile(Vector2 tilePos)
    {
        TileData tileData = GetTileData(tilePos);
        if (tileData != null)
        {
            tileData.isAvailable = true;
            tileData.placedUnit = null;
            SetTileData(tileData);
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

    private void CleanUp()
    {
        // Dictionary 정리
        if (tileMapDatas != null)
        {
            tileMapDatas.Clear();
        }

        // 참조 데이터 정리
        tileMap = null;
        mapData = null;
    }

    //시작 타일, 도착타일 pos
    //private void Update()
    //{
    //    if (Input.GetMouseButtonDown(0) && tileMap != null) // 마우스 왼쪽 버튼 클릭
    //    {
    //        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    //        Vector3Int unityPosition = tileMap.WorldToCell(worldPosition);

    //        if (tileMap.HasTile(unityPosition))
    //        {
    //            // Unity 좌표계 위치
    //            Debug.Log($"Unity 좌표: ({unityPosition.x}, {unityPosition.y})");

    //            // 사용자 정의 좌ㅠㅛ(왼쪽 상단 0,0)
    //            Vector2 customPos = ConvertToCustomCoordinates(unityPosition);
    //            Debug.Log($"사용자 정의 좌표: ({customPos.x}, {customPos.y})");

    //            // 타일 데이터 확인
    //            TileData tileData = GetTileData(customPos);
    //            if (tileData != null)
    //            {
    //                Debug.Log($"타일 데이터: isAvailable={tileData.isAvailable}, hasUnit={tileData.placedUnit != null}");
    //            }
    //            else
    //            {
    //                Debug.Log("해당 위치에 타일 데이터가 없음");
    //            }
    //        }
    //        else
    //        {
    //            Debug.Log("클릭한 위치에 타일이없음 ");
    //        }
    //    }
    //}
}
