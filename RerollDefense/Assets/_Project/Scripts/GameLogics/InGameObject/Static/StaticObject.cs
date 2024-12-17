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
    //�巡�׿�����Ʈ�� ��ġ�ؾ��� Ű���� �ʿ���
    public string tileShapeName; //�����ͺ��̽����� �ҷ��� �̸�

    public string prefabKey;


    public override void Initialize()
    {
        base.Initialize();
    }

    protected void InitializeTileShape()
    {
        // D_TileShpeData���� tileShapeType�� �ش��ϴ� �����͸� ������
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            Debug.Log($"TileShapeData �߰�: {tileShapeData.f_name}");

            relativeTiles = new List<Vector3Int>();

            // f_unitBuildData�� �ִ� ��ġ �����͸� �ݺ��ؼ� ������
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                // Vector2 �����͸� Vector3Int�� ��ȯ
                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tile.f_position.x),
                    Mathf.RoundToInt(tile.f_position.y),
                    0 // z���� �׻� 0���� ����
                );

                relativeTiles.Add(relativeTile);
                Debug.Log($"�߰��� Ÿ�� ��ǥ: {relativeTile}");
            }
        }
        else
        {
            Debug.LogError($"TileShapeData�� ã�� �� �����ϴ�. TileShapeType: {tileShapeName}");
        }

    }


    public override void Update()
    {
        base.Update();

    }

}
