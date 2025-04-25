using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// 타일 타입 정의
public enum TileType
{
    None,
    Attackable, // 공격 가능한 타일 (적이 지나가면서 공격함)
    Blocked,     // 완전히 막힌 타일 (적이 돌아가야함)
}

public class TileData
{
    //타일의 좌표
    public float tilePosX;
    public float tilePosY;

    // 적이 지나갈 수 있는지 여부
    public bool isPassable;

    //배치 가능여부
    public bool isAvailable;

    //배치된 유닛
    public UnitController placedUnit;

    public TileType tileType;

    //생성자 -> 초기화, 배치 가능상태, 유닛 없음
    public TileData(Vector2 tilePosition,TileType type)
    {
        tilePosX = tilePosition.x;
        tilePosY = tilePosition.y;
        isAvailable = true;
        placedUnit = null;
        isPassable = true;
        tileType = type;
    }

    public void PlaceUnit(UnitController unit, bool blockPath)
    {
        placedUnit = unit;
        isAvailable = false;
        isPassable = !blockPath; // blockPath가 true면 적이 지나갈 수 없음
    }


}
