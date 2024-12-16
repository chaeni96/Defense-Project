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


    //TODO : 테스트용, 오브젝트 데이터 사용해야됨
    public BGDatabaseEnum.TileShapeType tileShapeType;
    public string prefabKey;


    public override void Initialize()
    {
        base.Initialize();
        InitializeTileShape();
    }

    private void InitializeTileShape()
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_ShapeID == tileShapeType);

        if(tileShapeData != null)
        {

            Debug.Log($"TileShapeData 발견: ShapeID={tileShapeData.f_ShapeID}, Name={tileShapeData.f_ShapeName}");

            relativeTiles = new List<Vector3Int>();

            foreach(var tile in tileShapeData.f_TilePostion)
            {

                Vector3Int relativeTile = new Vector3Int((int)tile.x, (int)tile.y, (int)tile.z);

                relativeTiles.Add(new Vector3Int((int)tile.x, (int)tile.y, (int)tile.z));
                Debug.Log($"추가된 타일 좌표: {relativeTile}");

            }
        }
        else
        {
            Debug.LogError("TileShapeData 없음");
        }

    }


    public override void Update()
    {
        base.Update();

    }

}
