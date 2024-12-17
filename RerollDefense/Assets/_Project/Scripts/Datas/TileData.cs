using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public bool isAvailable { get; set; } = true; // ��ġ ���� ����
    public bool isOccupied { get; set; } = false; // ���� ����
    public PlacedObject occupyingObject { get; set; } // ������ ������Ʈ
    public string tileUniqueID { get; set; }

    public TileData(bool isAvailable = true)
    {
        this.isAvailable = isAvailable;
    }
}
