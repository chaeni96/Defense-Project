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
        // 멀티타일 유닛 확인
        bool isMultiTileUnit = false;
        MultiTileUnitController multiTileUnit = null;

        if (previewUnits.Count == 1 && previewUnits.ContainsKey(0) && previewUnits[0].isMultiUnit)
        {
            isMultiTileUnit = true;
            multiTileUnit = previewUnits[0] as MultiTileUnitController;
        }

        // 멀티타일 유닛의 경우 특별 처리
        if (isMultiTileUnit && multiTileUnit != null)
        {
            // 멀티타일 유닛용 배치 가능 여부 확인
            return CheckMultiTilePlacement(basePosition, multiTileUnit);
        }

        // 일반 유닛 처리 (기존 로직)
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
                // 인덱스가 Dictionary에 존재하는지 확인
                if (!previewUnits.ContainsKey(i))
                {
                    return false;
                }

                var placedUnit = tileData.placedUnit;
                var previewUnit = previewUnits[i];

                // 합성 가능 조건 - 같은 타입, 같은 레벨, 최대 5성 미만
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

    // 멀티타일 유닛의 배치 가능 여부 확인 (별도 메서드)
    private bool CheckMultiTilePlacement(Vector2 basePosition, MultiTileUnitController multiTileUnit)
    {
        // 원래 위치의 타일들 임시 저장
        List<TileData> originalTiles = new List<TileData>();

        // 현재 멀티타일 유닛이 점유 중인 타일 임시로 사용 가능하게 설정
        foreach (var offset in multiTileUnit.multiTilesOffset)
        {
            Vector2 originalPos = multiTileUnit.tilePosition + offset;
            TileData tileData = GetTileData(originalPos);

            if (tileData != null && tileData.placedUnit == multiTileUnit)
            {
                // 원래 상태 저장
                originalTiles.Add(new TileData(originalPos)
                {
                    isAvailable = tileData.isAvailable,
                    placedUnit = tileData.placedUnit
                });

                // 임시로 사용 가능하게 설정
                tileData.isAvailable = true;
                tileData.placedUnit = null;
                SetTileData(tileData);
            }
        }

        // 새 위치에 배치 가능한지 확인
        bool canPlace = true;
        foreach (var offset in multiTileUnit.multiTilesOffset)
        {
            Vector2 newPos = basePosition + offset;
            TileData targetTile = GetTileData(newPos);

            if (targetTile == null || !targetTile.isAvailable)
            {
                canPlace = false;
                break;
            }
        }

        // 원래 타일 상태 복원
        foreach (var tile in originalTiles)
        {
            TileData existingTile = GetTileData(new Vector2(tile.tilePosX, tile.tilePosY));
            if (existingTile != null)
            {
                existingTile.isAvailable = tile.isAvailable;
                existingTile.placedUnit = tile.placedUnit;
                SetTileData(existingTile);
            }
        }

        return canPlace;
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

    // 단일 타일 색상 변경
    public void SetTileColor(Vector2 tilePos, Color color)
    {
        Vector3Int position = ConvertToUnityCoordinates(tilePos);

        if (tileMap.HasTile(position))
        {
            tileMap.SetTileFlags(position, TileFlags.None);
            tileMap.SetColor(position, color);
        }
    }

    // 타일맵 색상 초기화 (원래 색상으로 돌리기)
    public void ResetTileColors()
    {
        foreach (var position in tileMap.cellBounds.allPositionsWithin)
        {
            if (tileMap.HasTile(position))
            {
                tileMap.SetTileFlags(position, TileFlags.None);
                tileMap.SetColor(position, Color.white); // 기본 색상으로 초기화
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
