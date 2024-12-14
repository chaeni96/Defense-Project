using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileData 
{
    //타일에 상태 저장하기 위한 데이터 클래스

    //데이터에 들어갈만한거 일단 설치여부만
    public bool isAvailable { get; set; }

    public TileData(bool isAvailable = true)
    {
        this.isAvailable = isAvailable;
    }
}
