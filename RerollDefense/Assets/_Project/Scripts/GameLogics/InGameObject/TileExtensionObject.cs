using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileExtensionObject : MonoBehaviour
{
    public UnitController parentUnit; // �θ� ���� ����
    public Vector2 offsetFromParent; // �θ�κ����� ������
    public SpriteRenderer spriteRenderer; 

    // �ð��� ȿ�� ���� �޼��� (��Ƽ����, ���� ��)
    public void SetVisualEffect(Material material)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.material = material;
        }
    }

    // �θ� ������ �����ӿ� ���� �ڽ��� ��ġ ������Ʈ
    public void UpdatePosition()
    {
        if (parentUnit != null)
        {
            Vector2 parentTilePos = parentUnit.tilePosition;
            Vector2 myTilePos = parentTilePos + offsetFromParent;

            Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(myTilePos);
            worldPos.z = transform.position.z; // ���� z �� ����
            transform.position = worldPos;
        }
    }
}
