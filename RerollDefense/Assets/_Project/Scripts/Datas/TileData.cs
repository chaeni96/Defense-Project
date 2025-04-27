using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    //Ÿ���� ��ǥ
    public float tilePosX;
    public float tilePosY;


    //��ġ ���ɿ���
    public bool isAvailable;

    //��ġ�� ����
    public UnitController placedUnit;


    //������ -> �ʱ�ȭ, ��ġ ���ɻ���, ���� ����
    public TileData(Vector2 tilePosition)
    {
        tilePosX = tilePosition.x;
        tilePosY = tilePosition.y;
        isAvailable = true;
        placedUnit = null;
    }

}
