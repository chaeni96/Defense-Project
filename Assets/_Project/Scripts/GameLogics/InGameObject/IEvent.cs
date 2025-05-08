using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public interface IEvent
{
    IngameEventType GetEventType();

    //이벤트 발동 또는 적용하는 대상, 이벤트 발동 위치(TODO : 필요없으면 인자값에서 빼기)
    void StartEvent(GameObject gameObject, Vector3 position);

}


// 적 스폰 이벤트
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
            Vector2.zero,  // 보스 현재 위치
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


//아이템 드랍 이벤트

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

        // 먼저 아이템을 선택하여 인벤토리에 즉시 추가 (시각적 효과와 별개로 데이터 보존)
        D_ItemData selectedItem = SelectRandomItem();

        if (selectedItem != null)
        {
            // 인벤토리에 아이템 추가 (실제 데이터 저장)
            //InventoryManager.Instance.AddItem(selectedItem);

            // 시각적 효과를 위한 필드 드롭 아이템 생성
            var obj = ResourceManager.Instance.Instantiate("FieldDropItemObject");
            FieldDropItemObject item = obj.GetComponent<FieldDropItemObject>();
            item.transform.position = position;
            item.InitializeItem(selectedItem);
            item.LoadItemIcon(selectedItem.f_iconImage.f_addressableKey);
        }

    }


    // 확률에 따른 아이템 선택 메서드
    private D_ItemData SelectRandomItem()
    {
        if (data.f_dropItems == null || data.f_dropItems.Count == 0)
            return null;

        // 1. 먼저 드롭할 DropItemData를 선택 (여러 개의 DropItemData가 있을 수 있음)
        D_DropItemData dropItemData = data.f_dropItems[Random.Range(0, data.f_dropItems.Count)];

        if (dropItemData.f_itemList == null || dropItemData.f_itemList.Count == 0)
            return null;

        // 2. 확률에 따라 itemList에서 아이템 선택
        float totalChance = 0;

        // 모든 아이템의 확률 총합 계산
        foreach (var itemListEntry in dropItemData.f_itemList)
        {
            totalChance += itemListEntry.f_chance;
        }

        // 1~totalChance 사이의 랜덤 값 생성
        float randomValue = Random.Range(0, totalChance);

        // 누적 확률로 아이템 선택
        float cumulativeChance = 0;

        foreach (var itemListEntry in dropItemData.f_itemList)
        {
            cumulativeChance += itemListEntry.f_chance;

            // 랜덤 값이 누적 확률 이하라면 해당 아이템 선택
            if (randomValue <= cumulativeChance)
            {
                return itemListEntry.f_itemData;
            }
        }

        // 기본값으로 첫 번째 아이템 반환 (만약 모든 확률의 합이 0이거나 문제가 있는 경우)
        return dropItemData.f_itemList[0].f_itemData;
    }
}