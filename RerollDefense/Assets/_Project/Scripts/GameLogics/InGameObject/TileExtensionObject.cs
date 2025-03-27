using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileExtensionObject : MonoBehaviour
{
    public SpriteRenderer tileSprite;

    private MultiTileUnitController parentUnit;
    public Vector2 offsetFromParent;


    public void Initialize(MultiTileUnitController parent, Vector2 offset)
    {
        parentUnit = parent;
        offsetFromParent = offset;

        // �θ� ������ �̺�Ʈ ����
        if (parentUnit != null)
        {
            parentUnit.OnPositionChanged += HandlePositionChanged;
            parentUnit.OnMaterialChanged += HandleMaterialChanged;
            parentUnit.OnUnitDeleted += HandleUnitDeleted;

            // �θ� ���ֿ� �ڽ��� ���
            parentUnit.AddExtensionTile(offset, this);
        }
    }

    // ��ġ ���� ó��
    private void HandlePositionChanged(Vector3 parentPosition)
    {
        Vector2 baseTilePos = TileMapManager.Instance.GetWorldToTilePosition(parentPosition);
        Vector2 myTilePos = baseTilePos + offsetFromParent;
        Vector3 myWorldPos = TileMapManager.Instance.GetTileToWorldPosition(myTilePos);
        myWorldPos.z = parentPosition.z; // ���� z ��ġ ����

        transform.position = myWorldPos;
    }

    // ���׸��� ���� ó��
    private void HandleMaterialChanged(Material newMaterial)
    {
        if (tileSprite != null)
        {
            tileSprite.material = newMaterial;

            // �θ� ������ ���� ������ ������� �ڽ��� ���� ���� ����
            if (parentUnit.unitSprite != null)
            {
                tileSprite.sortingOrder = parentUnit.unitSprite.sortingOrder - 1;
            }
        }
    }

    // ���� ���� ó��
    private void HandleUnitDeleted()
    {
        // �̺�Ʈ ���� ����
        if (parentUnit != null)
        {
            parentUnit.OnPositionChanged -= HandlePositionChanged;
            parentUnit.OnMaterialChanged -= HandleMaterialChanged;
            parentUnit.OnUnitDeleted -= HandleUnitDeleted;
        }

        // Ǯ�� �ý��ۿ� ��ȯ
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    // ��ü�� ��Ȱ��ȭ�� �� �̺�Ʈ ���� ����
    private void OnDisable()
    {
        if (parentUnit != null)
        {
            parentUnit.OnPositionChanged -= HandlePositionChanged;
            parentUnit.OnMaterialChanged -= HandleMaterialChanged;
            parentUnit.OnUnitDeleted -= HandleUnitDeleted;
        }
    }
}
