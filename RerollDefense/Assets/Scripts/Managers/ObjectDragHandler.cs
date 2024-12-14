using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class ObjectDragHandler : MonoBehaviour
{
    private Vector3Int previousTilePosition; // 이전 타일 위치 추적
    private SpriteRenderer spriteRenderer; // 드래그 중인 오브젝트의 SpriteRenderer

    private Vector3 originalPos;
    private Color originColor;

    private int testGold;

    D_UserData userData;

    public testGold test;

 
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = Vector3Int.zero; // 초기화

        originalPos = transform.position;
        originColor = spriteRenderer.color;

    }

    void Start()
    {
        userData = D_UserData.GetEntity(0);

        if(userData != null )
        {
            testGold = userData.f_Gold;
            Debug.Log($"현재 골드 : {testGold}");
        }

    }

    void OnMouseDown()
    {
        TileManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }
    void OnMouseDrag()
    {
        // 마우스 위치를 타일맵의 셀 좌표로 변환
        Vector3 mousePosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0; //2D라서 Z축 고정해야됨
        Vector3Int tilePosition = TileManager.Instance.tileMap.WorldToCell(mousePosition);

        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;    

        // 이전 타일과 현재 타일이 다를 경우 색상 갱신
        if (tilePosition != previousTilePosition)
        {
            // 이전 타일 색상 초기화
            TileManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
            // 현재 타일 색상 설정
            
            if (TileManager.Instance.IsTileAvailable(tilePosition))
            {
                TileManager.Instance.SetTileColor(tilePosition, new Color(0, 1, 0, 0.5f)); // 초록색 반투명
            }
            else
            {
                TileManager.Instance.SetTileColor(tilePosition, new Color(1, 0, 0, 0.5f)); // 빨간색 반투명
            }

            // 현재 타일 위치를 이전 타일로 업데이트
            previousTilePosition = tilePosition;
        }

        // 드래그 중인 오브젝트의 위치 갱신
        transform.position = TileManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
    }

    void OnMouseUp()
    {
        // 드래그 종료 시 타일 색 초기화
        TileManager.Instance.ResetTileColors(new Color(1, 1, 1, 0));

        // 배치 가능 여부에 따라 타워 배치 또는 원래 위치로 되돌림
        if (TileManager.Instance.IsTileAvailable(previousTilePosition))
        {
            // 배치 가능: 타워를 타일 중심에 고정하고 타일을 배치 불가능으로 설정
            transform.position = TileManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);
            TileManager.Instance.SetTileUnavailable(previousTilePosition);
            DecreaseGold(10);
            test.UpdateGoldText();
            Debug.Log("타워 배치 완료!");
        }
        else
        {
            Debug.Log("배치 불가능한 위치입니다.");

            transform.position = originalPos;
            spriteRenderer.color = originColor;
            //원래 포지션으로 돌아와야함
        }
    }


    //test용 골드 감소

    public void DecreaseGold(int amount)
    {
        if(testGold >= amount)
        {
            testGold -= amount;

            userData.f_Gold = testGold;

            //데이터 저장
            SaveLoadManager.Instance.SaveData();

        }
    }

}
