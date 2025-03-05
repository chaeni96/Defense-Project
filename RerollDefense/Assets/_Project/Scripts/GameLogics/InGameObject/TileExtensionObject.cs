using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileExtensionObject : MonoBehaviour
{
    public UnitController parentUnit; // 부모 유닛 참조
    public Vector2 offsetFromParent; // 부모로부터의 오프셋
    public SpriteRenderer spriteRenderer; 

    // 시각적 효과 설정 메서드 (머티리얼, 색상 등)
    public void SetVisualEffect(Material material)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.material = material;
        }
    }

    // 부모 유닛의 움직임에 따라 자신의 위치 업데이트
    public void UpdatePosition()
    {
        if (parentUnit != null)
        {
            Vector2 parentTilePos = parentUnit.tilePosition;
            Vector2 myTilePos = parentTilePos + offsetFromParent;

            Vector3 worldPos = TileMapManager.Instance.GetTileToWorldPosition(myTilePos);
            worldPos.z = transform.position.z; // 기존 z 값 유지
            transform.position = worldPos;
        }
    }
}
