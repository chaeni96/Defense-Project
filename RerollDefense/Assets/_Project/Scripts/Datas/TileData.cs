using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData
{
    public bool isAvailable { get; set; } = true; // 배치 가능 여부
    public string tileUniqueID { get; set; } //여러칸을 차지하는 유닛일때 쓸 ID

}
