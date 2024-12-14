using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlacedObject : StaticObject
{

    //��ġ�Ϸ�� ������Ʈ�� �ʿ��Ѱ� Ÿ��, ������ �������� �Ӽ�

    //�����ϴ� ĭ -> staticObject�� ����
    //���ݷ�, hp, ������Ʈ �̸�, ������

    //���� ��� �ʿ�

    public override void Initialize()
    {
        base.Initialize();


    }

    //TODO : �޼��� �̸� �����ϱ�, �����ͷ� �޾ƿ���
    public void InitializeObj(Vector3Int baseTilePosition, List<Vector3Int> relativeTiles)
    {
        this.previousTilePosition = baseTilePosition;
        this.relativeTiles = relativeTiles;

        // ��ġ�� ��ġ�� �̵� (Ÿ�ϸ� �߽� ��ǥ ���)
        Vector3 placementCenter = CalculatePlacementCenter(baseTilePosition, relativeTiles);
        transform.position = placementCenter;
    }

    public override void Update()
    {
        base.Update();
    }

    private Vector3 CalculatePlacementCenter(Vector3Int baseTilePosition, List<Vector3Int> relativeTiles)
    {
        Vector3 totalPosition = Vector3.zero;

        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int tilePosition = baseTilePosition + relativeTile;
            totalPosition += TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
        }

        return totalPosition / relativeTiles.Count; // Ÿ�� �߽� ��ǥ ��հ� ��ȯ
    }
}
