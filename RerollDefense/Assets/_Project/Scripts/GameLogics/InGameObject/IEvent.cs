using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public interface IEvent
{
    EventType GetEventType();

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

    public EventType GetEventType() => EventType.SpawnEnemy;

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

        // �� ī��Ʈ ���� ����
        StageManager.Instance.NotifyEnemyIncrease(data.f_spawnCount);
    }
}


//������ ��� �̺�Ʈ

public class DropItemEvent : IEvent
{
    private D_DropItemEventData data;

    private FieldDropItemObject item;

    public EventType GetEventType() => EventType.DropItem;
    
    public DropItemEvent(D_DropItemEventData eventData)
    {
        this.data = eventData;   
    }

    public void StartEvent(GameObject gameObject, Vector3 position)
    {

        var obj = ResourceManager.Instance.Instantiate("FieldDropItemObject");
        //������ �����ؼ� ����ϱ�
        item = obj.GetComponent<FieldDropItemObject>();
        item.LoadItemIcon(data.f_ITemData.f_itemIcon.f_addressableKey);
        item.transform.position = position;
        item.InitializeItem(data.f_ITemData);

        //������ Ÿ�Կ� ���� �κ��� ������ ���� �÷����ϴ��� �����ؾߵ�




    }
}