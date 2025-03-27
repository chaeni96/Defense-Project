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

        // 부모 유닛의 이벤트 구독
        if (parentUnit != null)
        {
            parentUnit.OnPositionChanged += HandlePositionChanged;
            parentUnit.OnMaterialChanged += HandleMaterialChanged;
            parentUnit.OnUnitDeleted += HandleUnitDeleted;

            // 부모 유닛에 자신을 등록
            parentUnit.AddExtensionTile(offset, this);
        }
    }

    // 위치 변경 처리
    private void HandlePositionChanged(Vector3 parentPosition)
    {
        Vector2 baseTilePos = TileMapManager.Instance.GetWorldToTilePosition(parentPosition);
        Vector2 myTilePos = baseTilePos + offsetFromParent;
        Vector3 myWorldPos = TileMapManager.Instance.GetTileToWorldPosition(myTilePos);
        myWorldPos.z = parentPosition.z; // 같은 z 위치 유지

        transform.position = myWorldPos;
    }

    // 머테리얼 변경 처리
    private void HandleMaterialChanged(Material newMaterial)
    {
        if (tileSprite != null)
        {
            tileSprite.material = newMaterial;

            // 부모 유닛의 소팅 오더를 기반으로 자신의 소팅 오더 설정
            if (parentUnit.unitSprite != null)
            {
                tileSprite.sortingOrder = parentUnit.unitSprite.sortingOrder - 1;
            }
        }
    }

    // 유닛 삭제 처리
    private void HandleUnitDeleted()
    {
        // 이벤트 구독 해제
        if (parentUnit != null)
        {
            parentUnit.OnPositionChanged -= HandlePositionChanged;
            parentUnit.OnMaterialChanged -= HandleMaterialChanged;
            parentUnit.OnUnitDeleted -= HandleUnitDeleted;
        }

        // 풀링 시스템에 반환
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    // 객체가 비활성화될 때 이벤트 구독 해제
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
