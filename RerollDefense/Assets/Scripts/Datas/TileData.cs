using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public bool isAvailable { get; set; } = true; // ��ġ ���� ����
    public bool IsOccupied { get; set; } = false; // ���� ����
    public PlacedObject OccupyingObject { get; set; } // ������ ������Ʈ

    public TileData(bool isAvailable = true)
    {
        this.isAvailable = isAvailable;
    }
}
