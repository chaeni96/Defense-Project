using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : StaticObject
{

    //배치완료된 오브젝트에 필요한것 타워, 유닛의 공통적인 속성

    //차지하는 칸 -> staticObject에 있음
    //공격력, hp, 오브젝트 이름, 프리팹

    //제거 기능 필요

    public override void Initialize()
    {
        base.Initialize();


    }

    //TODO : 메서드 이름 변경하기, 데이터로 받아오기
    public void InitializeObj(Vector3Int baseTilePosition, List<Vector3Int> relativeTiles)
    {
        this.previousTilePosition = baseTilePosition;
        this.relativeTiles = relativeTiles;

        // 배치된 위치로 이동 (타일맵 중심 좌표 계산)
        Vector3 placementCenter = CalculatePlacementCenter(baseTilePosition, relativeTiles);
        transform.position = placementCenter;
    }

    public override void Update()
    {
        base.Update();
    }

    private Vector3 CalculatePlacementCenter(Vector3Int baseTilePosition, List<Vector3Int> relativeTiles)
    {
        Vector3 totalPosition = Vector3.zero;

        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int tilePosition = baseTilePosition + relativeTile;
            totalPosition += TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
        }

        return totalPosition / relativeTiles.Count; // 타일 중심 좌표 평균값 반환
    }
}
