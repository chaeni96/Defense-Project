using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public bool isAvailable { get; set; } = true; // 배치 가능 여부
    public bool IsOccupied { get; set; } = false; // 점유 여부
    public PlacedObject OccupyingObject { get; set; } // 점유한 오브젝트

    public TileData(bool isAvailable = true)
    {
        this.isAvailable = isAvailable;
    }
}
