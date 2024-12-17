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
    //드래그오브젝트에 설치해야할 키값이 필요함
    public string tileShapeName; //데이터베이스에서 불러올 이름

    public string prefabKey;


    public override void Initialize()
    {
        base.Initialize();
    }

    protected void InitializeTileShape()
    {
        // D_TileShpeData에서 tileShapeType에 해당하는 데이터를 가져옴
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            Debug.Log($"TileShapeData 발견: {tileShapeData.f_name}");

            relativeTiles = new List<Vector3Int>();

            // f_unitBuildData에 있는 위치 데이터를 반복해서 가져옴
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                // Vector2 데이터를 Vector3Int로 변환
                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tile.f_position.x),
                    Mathf.RoundToInt(tile.f_position.y),
                    0 // z축은 항상 0으로 설정
                );

                relativeTiles.Add(relativeTile);
                Debug.Log($"추가된 타일 좌표: {relativeTile}");
            }
        }
        else
        {
            Debug.LogError($"TileShapeData를 찾을 수 없습니다. TileShapeType: {tileShapeName}");
        }

    }


    public override void Update()
    {
        base.Update();

    }

}
