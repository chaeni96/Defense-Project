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

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update()
    {
        base.Update();

    }

}
