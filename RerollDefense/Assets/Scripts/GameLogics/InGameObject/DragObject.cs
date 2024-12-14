using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DragObject : StaticObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    //test�� ����
    public testGold test;
    private int testGold;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3 originalPos;

    D_UserData userData;

    //test�� awake
    private void Awake()
    {
        Initialize();
    }


    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // ��ȿ���� �ʴ� ������ �ʱ�ȭ
        originalPos = transform.position;
        originColor = spriteRenderer.color;

        //test
        // Ÿ���� ��� Ÿ�� ũ�� �ʱ�ȭ (��: 1x2 ũ��)
        relativeTiles = new List<Vector3Int>
        {
            new Vector3Int(0, 0, 0), // ���� Ÿ��
            new Vector3Int(0, 1, 0)  // �Ʒ� Ÿ��
        };

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

    public override void Update()
    {
        base.Update();

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
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f)); // ���� Ÿ�� �ʱ�ȭ

            // �� Ÿ�Ϻ� ��ġ ���� ���ο� ���� ���� ����
            foreach (var relativeTile in relativeTiles)
            {
                Vector3Int checkPosition = tilePosition + relativeTile;

                if (TileMapManager.Instance.GetTileData(checkPosition)?.isAvailable == true)
                {
                    TileMapManager.Instance.SetTileColors(checkPosition, new List<Vector3Int> { Vector3Int.zero }, new Color(0, 1, 0, 0.5f)); // �ʷϻ�
                }
                else
                {
                    TileMapManager.Instance.SetTileColors(checkPosition, new List<Vector3Int> { Vector3Int.zero }, new Color(1, 0, 0, 0.5f)); // ������
                }
            }

            previousTilePosition = tilePosition;
        }

        // ������Ʈ ��ġ ����
        transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
    }


    public void OnPointerUp(PointerEventData eventData)
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0)); // Ÿ�� �� �ʱ�ȭ

        if (TileMapManager.Instance.AreTilesAvailable(previousTilePosition, relativeTiles))
        {
            // ��ġ Ȯ��
            TileMapManager.Instance.SetTilesUnavailable(previousTilePosition, relativeTiles);
            transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);
            DecreaseGold(10);
            test.UpdateGoldText();

            //Ÿ�� �����ڵ�
            CreatePlacedObject();

            Debug.Log("Ÿ�� ��ġ �Ϸ�!");
        }
        else
        {
            // ��ġ �Ұ����� ��� ���� ��ġ�� ����
            Debug.Log("��ġ �Ұ� ����!");
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
        else
        {
            Debug.Log("��尡 �����մϴ�!");
        }
    }


    private void CreatePlacedObject()
    {
        //Ÿ���� ���� �ٸ� ������ �����ؾߵǴµ� ��巹�������ؾ���
        // PlacedObject ������ ����
        GameObject placedObjectPrefab = Resources.Load<GameObject>("Prefabs/Tower/1x2TowerObject"); // PlacedObject ������ �ε�
        GameObject placedObjectInstance = Instantiate(placedObjectPrefab);

        // PlacedObject ��ũ��Ʈ ��������
        PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();

        // PlacedObject �ʱ�ȭ
        placedObject.InitializeObj(previousTilePosition, relativeTiles);


        // ���� DragObject ��Ȱ��ȭ �Ǵ� ����
        Destroy(gameObject);
    }

}
