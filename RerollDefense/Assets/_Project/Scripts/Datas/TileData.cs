using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// Ÿ�� Ÿ�� ����
public enum TileType
{
    None,
    Attackable, // ���� ������ Ÿ�� (���� �������鼭 ������)
    Blocked,     // ������ ���� Ÿ�� (���� ���ư�����)
}

public class TileData
{
    //Ÿ���� ��ǥ
    public float tilePosX;
    public float tilePosY;

    // ���� ������ �� �ִ��� ����
    public bool isPassable;

    //��ġ ���ɿ���
    public bool isAvailable;

    //��ġ�� ����
    public UnitController placedUnit;

    public TileType tileType;

    //������ -> �ʱ�ȭ, ��ġ ���ɻ���, ���� ����
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
        isPassable = !blockPath; // blockPath�� true�� ���� ������ �� ����
    }


}
