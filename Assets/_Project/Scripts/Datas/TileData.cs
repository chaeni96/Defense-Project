using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    //타일의 좌표
    public float tilePosX;
    public float tilePosY;


    //배치 가능여부
    public bool isAvailable;

    //배치된 유닛
    public UnitController placedUnit;


    //생성자 -> 초기화, 배치 가능상태, 유닛 없음
    public TileData(Vector2 tilePosition)
    {
        tilePosX = tilePosition.x;
        tilePosY = tilePosition.y;
        isAvailable = true;
        placedUnit = null;
    }

}
