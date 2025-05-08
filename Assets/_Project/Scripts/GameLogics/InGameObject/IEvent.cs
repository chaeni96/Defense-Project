using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public interface IEvent
{
    IngameEventType GetEventType();

    //�̺�Ʈ �ߵ� �Ǵ� �����ϴ� ���, �̺�Ʈ �ߵ� ��ġ(TODO : �ʿ������ ���ڰ����� ����)
    void StartEvent(GameObject gameObject, Vector3 position);

}


// �� ���� �̺�Ʈ
public class SpawnEnemyEvent : IEvent
{
    private D_SpawnEnemyEventData data;

    public SpawnEnemyEvent(D_SpawnEnemyEventData eventData)
    {
        data = eventData;
    }

    public IngameEventType GetEventType() => IngameEventType.SpawnEnemy;

    public void StartEvent(GameObject obj, Vector3 position)
    {
        Vector2 centerPos = TileMapManager.Instance.GetWorldToTilePosition(obj.transform.position);

        List<Vector2> directions = new List<Vector2>
        {
            Vector2.zero,  // ���� ���� ��ġ
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };

        List<Vector3> validPositions = new List<Vector3>();

        foreach (var dir in directions)
        {
            Vector2 checkPos = centerPos + dir;
            TileData tileData = TileMapManager.Instance.GetTileData(checkPos);

            if (tileData != null && tileData.isAvailable)
            {
                validPositions.Add(TileMapManager.Instance.GetTileToWorldPosition(checkPos));
            }
        }

        if (validPositions.Count > 0)
        {
            int enemiesPerPosition = data.f_spawnCount / validPositions.Count;
            int remainingEnemies = data.f_spawnCount % validPositions.Count;

            for (int i = 0; i < validPositions.Count; i++)
            {
                int spawnCount = enemiesPerPosition;
                if (remainingEnemies > 0)
                {
                    spawnCount++;
                    remainingEnemies--;
                }

                for (int j = 0; j < spawnCount; j++)
                {
                    EnemyManager.Instance.SpawnEnemy(data.f_enemy, validPositions[i]);
                }
            }
        }
    }
}


//������ ��� �̺�Ʈ

public class DropItemEvent : IEvent
{
    private D_DropItemEventData data;

    private FieldDropItemObject item;

    public IngameEventType GetEventType() => IngameEventType.DropItem;
    
    public DropItemEvent(D_DropItemEventData eventData)
    {
        this.data = eventData;   
    }

    public void StartEvent(GameObject gameObject, Vector3 position)
    {

        // ���� �������� �����Ͽ� �κ��丮�� ��� �߰� (�ð��� ȿ���� ������ ������ ����)
        D_ItemData selectedItem = SelectRandomItem();

        if (selectedItem != null)
        {
            // �κ��丮�� ������ �߰� (���� ������ ����)
            //InventoryManager.Instance.AddItem(selectedItem);

            // �ð��� ȿ���� ���� �ʵ� ��� ������ ����
            var obj = ResourceManager.Instance.Instantiate("FieldDropItemObject");
            FieldDropItemObject item = obj.GetComponent<FieldDropItemObject>();
            item.transform.position = position;
            item.InitializeItem(selectedItem);
            item.LoadItemIcon(selectedItem.f_iconImage.f_addressableKey);
        }

    }


    // Ȯ���� ���� ������ ���� �޼���
    private D_ItemData SelectRandomItem()
    {
        if (data.f_dropItems == null || data.f_dropItems.Count == 0)
            return null;

        // 1. ���� ����� DropItemData�� ���� (���� ���� DropItemData�� ���� �� ����)
        D_DropItemData dropItemData = data.f_dropItems[Random.Range(0, data.f_dropItems.Count)];

        if (dropItemData.f_itemList == null || dropItemData.f_itemList.Count == 0)
            return null;

        // 2. Ȯ���� ���� itemList���� ������ ����
        float totalChance = 0;

        // ��� �������� Ȯ�� ���� ���
        foreach (var itemListEntry in dropItemData.f_itemList)
        {
            totalChance += itemListEntry.f_chance;
        }

        // 1~totalChance ������ ���� �� ����
        float randomValue = Random.Range(0, totalChance);

        // ���� Ȯ���� ������ ����
        float cumulativeChance = 0;

        foreach (var itemListEntry in dropItemData.f_itemList)
        {
            cumulativeChance += itemListEntry.f_chance;

            // ���� ���� ���� Ȯ�� ���϶�� �ش� ������ ����
            if (randomValue <= cumulativeChance)
            {
                return itemListEntry.f_itemData;
            }
        }

        // �⺻������ ù ��° ������ ��ȯ (���� ��� Ȯ���� ���� 0�̰ų� ������ �ִ� ���)
        return dropItemData.f_itemList[0].f_itemData;
    }
}