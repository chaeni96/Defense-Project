using DG.Tweening;
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
            parentUnit.OnMovingPositionChanged += HandleMovingPositionChanged;
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

    //���� �ִ� ������
    private void HandleMovingPositionChanged(Vector3 parentPosition, float duration)
    {
        Vector2 baseTilePos = TileMapManager.Instance.GetWorldToTilePosition(parentPosition);
        Vector2 myTilePos = baseTilePos + offsetFromParent;
        Vector3 myWorldPos = TileMapManager.Instance.GetTileToWorldPosition(myTilePos);
        myWorldPos.z = parentPosition.z; // ���� z ��ġ ����

        // DOTween�� ����Ͽ� �ε巯�� �̵�
        transform.DOMove(myWorldPos, duration).SetEase(Ease.OutBack);
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
            parentUnit.OnMovingPositionChanged -= HandleMovingPositionChanged;

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
            parentUnit.OnMovingPositionChanged -= HandleMovingPositionChanged;
        }
    }
}
