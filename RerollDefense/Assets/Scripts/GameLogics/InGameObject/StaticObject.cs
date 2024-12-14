using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObject : BasicObject
{
    //고정(배치 가능한) 오브젝트

    [HideInInspector]
    public Vector3Int previousTilePosition; // 이전 타일 위치
    [HideInInspector]
    public List<Vector3Int> relativeTiles; // 타일 배치 크기

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update()
    {
        base.Update();

    }

}
