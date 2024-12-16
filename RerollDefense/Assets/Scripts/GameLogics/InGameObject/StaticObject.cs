using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObject : BasicObject
{
    //����(��ġ ������) ������Ʈ

    [HideInInspector]
    public Vector3Int previousTilePosition; // ���� Ÿ�� ��ġ
    [HideInInspector]
    public List<Vector3Int> relativeTiles; // Ÿ�� ��ġ ũ��


    //TODO : �׽�Ʈ��, ������Ʈ ������ ����ؾߵ�
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

            Debug.Log($"TileShapeData �߰�: ShapeID={tileShapeData.f_ShapeID}, Name={tileShapeData.f_ShapeName}");

            relativeTiles = new List<Vector3Int>();

            foreach(var tile in tileShapeData.f_TilePostion)
            {

                Vector3Int relativeTile = new Vector3Int((int)tile.x, (int)tile.y, (int)tile.z);

                relativeTiles.Add(new Vector3Int((int)tile.x, (int)tile.y, (int)tile.z));
                Debug.Log($"�߰��� Ÿ�� ��ǥ: {relativeTile}");

            }
        }
        else
        {
            Debug.LogError("TileShapeData ����");
        }

    }


    public override void Update()
    {
        base.Update();

    }

}
