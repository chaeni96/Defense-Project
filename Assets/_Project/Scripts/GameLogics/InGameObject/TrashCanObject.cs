using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCanObject : MonoBehaviour
{
    [SerializeField] private Vector3 initPosition = new Vector3(0f, 0f, 0f); // 기본 위치
    [SerializeField] private Collider2D myCollider; // 인스펙터에서 바인딩

    public void InitializeTrashCan()
    {
        // 시작 시 비활성화
        gameObject.SetActive(false);

        // 콜라이더가 할당되지 않았다면 자동으로 찾기
        if (myCollider == null)
        {
            myCollider = GetComponent<Collider2D>();
        }
    }

    // 쓰레기통 활성화 및 위치 설정
    public void Show()
    {
        transform.position = initPosition;
        gameObject.SetActive(true);
    }

    // 쓰레기통 비활성화
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // 특정 위치가 쓰레기통 영역 안에 있는지 확인
    public bool IsPositionOver(Vector3 worldPosition)
    {
        //비활도이어있거나 콜라이더 없으면 안됨
        if (!gameObject.activeSelf || myCollider == null)
            return false;

        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        return myCollider.OverlapPoint(position2D);
    }

 
}
