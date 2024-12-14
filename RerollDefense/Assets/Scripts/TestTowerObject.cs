using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class TestTowerObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public testGold test;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3Int previousTilePosition; // 이전 타일 위치 추적
    private Vector3 originalPos;

    private int testGold;

    D_UserData userData;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // 유효하지 않는 값으로 초기화
        originalPos = transform.position;
        originColor = spriteRenderer.color;
    }

    void Start()
    {
        userData = D_UserData.GetEntity(0);

        if (userData != null)
        {
            testGold = userData.f_Gold;
            Debug.Log($"현재 골드 : {testGold}");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 마우스 위치를 타일맵 셀 좌표로 변환
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;
        Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        // 드래그 중 투명도 조정
        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;

        // 타일 색상 갱신
        if (tilePosition != previousTilePosition)
        {
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));

            if (TileMapManager.Instance.IsTileAvailable(tilePosition))
            {
                TileMapManager.Instance.SetTileColor(tilePosition, new Color(0, 1, 0, 0.5f)); // 초록색
            }
            else
            {
                TileMapManager.Instance.SetTileColor(tilePosition, new Color(1, 0, 0, 0.5f)); // 빨간색
            }

            previousTilePosition = tilePosition;
        }

        // 오브젝트 위치 갱신
        transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
    }

    public void OnPointerUp(PointerEventData eventData)
    {

        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0));

        if (TileMapManager.Instance.IsTileAvailable(previousTilePosition))
        {
            transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);
            TileMapManager.Instance.SetTileUnavailable(previousTilePosition);
            DecreaseGold(10);
            test.UpdateGoldText();
            Debug.Log("타워 배치 완료!");
        }
        else
        {
            Debug.Log("배치 불가능한 위치입니다.");
            transform.position = originalPos;
            spriteRenderer.color = originColor;
        }
    }

    public void DecreaseGold(int amount)
    {
        testGold = userData.f_Gold;

        if (testGold >= amount)
        {
            testGold -= amount;
            userData.f_Gold = testGold;

            // 데이터 저장
            SaveLoadManager.Instance.SaveData();
        }
    }
}
