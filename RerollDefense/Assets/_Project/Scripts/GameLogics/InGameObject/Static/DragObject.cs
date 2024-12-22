using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

public class DragObject : StaticObject
{
    public bool isPlaced { get; private set; } = false;

    private SpriteRenderer spriteRenderer;
    private Color originColor;
    private Vector3 originalPos;

    private string tileShapeName; //�����ͺ��̽����� �ҷ��� �̸�


    //TODO : �׽�Ʈ��, ������Ʈ ������ ����ؾߵ�
    //�巡�׿�����Ʈ�� ��ġ�ؾ��� Ű���� �ʿ���
    public testGold test;


    public string prefabKey;

    public override void Initialize()
    {
        base.Initialize();
        spriteRenderer = GetComponent<SpriteRenderer>();
        previousTilePosition = new Vector3Int(-1, -1, -1); // ��ȿ���� �ʴ� ������ �ʱ�ȭ
        originalPos = transform.position;
        originColor = spriteRenderer.color;
        isPlaced = false;
    }

    public void SetUpUnitData(string tileDataKey)
    {
        tileShapeName = tileDataKey;

    }

    public override void Update()
    {
        base.Update();

    }

    private void InitializeTileShape()
    {
        // D_TileShpeData���� tileShapeType�� �ش��ϴ� �����͸� ������
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            Debug.Log($"TileShapeData �߰�: {tileShapeData.f_name}");

            relativeTiles = new List<Vector3Int>();

            // f_unitBuildData�� �ִ� ��ġ �����͸� �ݺ��ؼ� ������
            foreach (var tile in tileShapeData.f_unitBuildData)
            {
                // Vector2 �����͸� Vector3Int�� ��ȯ
                Vector3Int relativeTile = new Vector3Int(
                    Mathf.RoundToInt(tile.f_position.x),
                    Mathf.RoundToInt(tile.f_position.y),
                    0 // z���� �׻� 0���� ����
                );

                relativeTiles.Add(relativeTile);
                Debug.Log($"�߰��� Ÿ�� ��ǥ: {relativeTile}");
            }
        }
        else
        {
            Debug.LogError($"TileShapeData�� ã�� �� �����ϴ�. TileShapeType: {tileShapeName}");
        }

    }

    public void TESTOnPointerDown()
    {
        InitializeTileShape();

        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0.1f));
    }

    public void OnPointerDrag(Vector3 pointerPosition)
    {
       

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


    public void TESTOnPointerUp()
    {
        TileMapManager.Instance.ResetTileColors(new Color(1, 1, 1, 0)); // Ÿ�� �� �ʱ�ȭ

        if (TileMapManager.Instance.AreTilesAvailable(previousTilePosition, relativeTiles))
        {
            // ��ġ Ȯ��
            string tileUniqueID = Guid.NewGuid().ToString();    

            TileMapManager.Instance.SetTilesUnavailable(previousTilePosition, relativeTiles, tileUniqueID);
            transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(previousTilePosition);

            //Ÿ�� �����ڵ�
            CreatePlacedObject(tileUniqueID);

            //TODO : ��� ���� �ڵ�
            isPlaced = true;
            Debug.Log("Ÿ�� ��ġ �Ϸ�!");
        }
        else
        {
            // ��ġ �Ұ����� ��� ���� ��ġ�� ����
            Debug.Log("��ġ �Ұ� ����!");
            transform.position = originalPos;
            spriteRenderer.color = originColor;
            isPlaced = false;

            //���� ��ġ�� ���ư����� ī�尡 �����ߵ�
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


    private void CreatePlacedObject(string uniqueID)
    {
        // Ÿ���� ����� ��ġ �����͸� ������� �������� ���� �� ����
        for (int i = 0; i < relativeTiles.Count; i++)
        {

            Vector3Int relativeTile = relativeTiles[i];
            // ����� ��ġ�� �������� ���� Ÿ�� ��ǥ ���
            Vector3Int tilePosition = previousTilePosition + relativeTile;

            // ������ ���� �� ��ġ ����
            GameObject placedObjectInstance = ResourceManager.Instance.Instantiate(prefabKey);

            // PlacedObject ��ũ��Ʈ ��������
            PlacedObject placedObject = placedObjectInstance.GetComponent<PlacedObject>();

            if (placedObject != null)
            {
                placedObject.transform.position = TileMapManager.Instance.tileMap.GetCellCenterWorld(tilePosition);
                placedObject.RegistereTileID(uniqueID);
                placedObject.InitializeUnitStat(tileShapeName, i);
            }

            // Ÿ�ϸʿ� ��ġ�� ������Ʈ ���
            TileMapManager.Instance.SetTileUnavailable(tilePosition);
        }

        // �巡�� ������Ʈ ����
        Destroy(gameObject);
    }

}
