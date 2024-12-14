using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class TestTowerObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public testGold test;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3Int previousTilePosition; // ���� Ÿ�� ��ġ ����
    private Vector3 originalPos;

    private int testGold;

    D_UserData userData;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // ��ȿ���� �ʴ� ������ �ʱ�ȭ
        originalPos = transform.position;
        originColor = spriteRenderer.color;
    }

    void Start()
    {
        userData = D_UserData.GetEntity(0);

        if (userData != null)
        {
            testGold = userData.f_Gold;
            Debug.Log($"���� ��� : {testGold}");
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ���콺 ��ġ�� Ÿ�ϸ� �� ��ǥ�� ��ȯ
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;
        Vector3Int tilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        // �巡�� �� ���� ����
        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;

        // Ÿ�� ���� ����
        if (tilePosition != previousTilePosition)
        {
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));

            if (TileMapManager.Instance.IsTileAvailable(tilePosition))
            {
                TileMapManager.Instance.SetTileColor(tilePosition, new Color(0, 1, 0, 0.5f)); // �ʷϻ�
            }
            else
            {
                TileMapManager.Instance.SetTileColor(tilePosition, new Color(1, 0, 0, 0.5f)); // ������
            }

            previousTilePosition = tilePosition;
        }

        // ������Ʈ ��ġ ����
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
            Debug.Log("Ÿ�� ��ġ �Ϸ�!");
        }
        else
        {
            Debug.Log("��ġ �Ұ����� ��ġ�Դϴ�.");
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

            // ������ ����
            SaveLoadManager.Instance.SaveData();
        }
    }
}
