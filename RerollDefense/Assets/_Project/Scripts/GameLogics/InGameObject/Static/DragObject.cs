using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DragObject : StaticObject, IPointerDownHandler, IDragHandler, IPointerUpHandler
{

    //test�� ����
    public testGold test;

    private SpriteRenderer spriteRenderer;
    private Color originColor;

    private Vector3 originalPos;



    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // ��ȿ���� �ʴ� ������ �ʱ�ȭ
        originalPos = transform.position;
        originColor = spriteRenderer.color;

    }

    public override void Update()
    {
        base.Update();

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InitializeTileShape();

        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ���콺 ��ġ�� Ÿ�ϸ� �� ��ǥ�� ��ȯ
        Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
        pointerPosition.z = 0;

        // ���� Ÿ��(Ŭ���� ��ġ)�� Ÿ�� ��ǥ ���
        Vector3Int baseTilePosition = TileMapManager.Instance.tileMap.WorldToCell(pointerPosition);

        // Ÿ���� �߽� �������� ��ġ ����
        Vector3 centerPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(baseTilePosition);
        transform.position = centerPosition;

        // �巡�� �� ���� ����
        Color newColor = spriteRenderer.color;
        newColor.a = 0.3f;
        spriteRenderer.color = newColor;

        // Ÿ�� ���� ����
        if (baseTilePosition != previousTilePosition)
        {
            TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f)); // ���� Ÿ�� ���� �ʱ�ȭ

            // �� Ÿ�Ϻ� ��ġ ���� ���ο� ���� ���� ����
            foreach (var relativeTile in relativeTiles)
            {
                Vector3Int checkPosition = baseTilePosition + relativeTile; // ��� ��ġ ���
                TileData tileData = TileMapManager.Instance.GetTileData(checkPosition);

                if (tileData == null || !tileData.isAvailable) // Ÿ�ϸ� �ܺ��̰ų� ��ġ �Ұ���
                {
                    TileMapManager.Instance.SetTileColors(
                        checkPosition,
                        new List<Vector3Int> { Vector3Int.zero },
                        new Color(1, 0, 0, 0.5f) // ������
                    );
                }
                else // Ÿ�� ��ġ ����
                {
                    TileMapManager.Instance.SetTileColors(
                        checkPosition,
                        new List<Vector3Int> { Vector3Int.zero },
                        new Color(0, 1, 0, 0.5f) // �ʷϻ�
                    );
                }
            }

            previousTilePosition = baseTilePosition;
        }
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
        //testGold = userData.f_Gold;

        //if (testGold >= amount)
        //{
        //    testGold -= amount;
        //    userData.f_Gold = testGold;

        //    // ������ ����
        //    SaveLoadManager.Instance.SaveData();
        //}
        //else
        //{
        //    Debug.Log("��尡 �����մϴ�!");
        //}
    }


    private void CreatePlacedObject()
    {
        // Ÿ���� ����� ��ġ �����͸� ������� �������� ���� �� ����
        foreach (Vector3Int relativeTile in relativeTiles)
        {
            // ����� ��ġ�� �������� ���� Ÿ�� ��ǥ ���
            Vector3Int tilePosition = previousTilePosition + relativeTile;

            // ������ ���� �� ��ġ ����
            GameObject placedObjectInstance = ResourceManager.Instance.Instantiate(prefabKey);
            if (placedObjectInstance == null)
            {
                Debug.LogError("������ ���� ����");
                continue;
            }

            // PlacedObject ��ũ��Ʈ ��������
            PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();
            if (placedObject != null)
            {
                placedObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);

                // ����� ��ġ�� ���� ������Ʈ�� ���� (����� �뵵)
                placedObject.previousTilePosition = tilePosition;
                placedObject.relativeTiles = new List<Vector3Int>() { Vector3Int.zero }; // �ڱ� �ڽŸ� �����Ѵٰ� ����
            }
            else
            {
                Debug.LogError("PlacedObject ��ũ��Ʈ�� �����տ� �����ϴ�.");
            }

            // Ÿ�ϸʿ� ��ġ�� ������Ʈ ���
            TileMapManager.Instance.SetTileUnavailable(tilePosition);
        }

        // �巡�� ������Ʈ ����
        Destroy(gameObject);
    }

}
