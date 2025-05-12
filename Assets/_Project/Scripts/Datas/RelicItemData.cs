using System.Collections.Generic;
using UnityEngine;

public partial class D_RelicItemData
{
    public static List<D_RelicItemData> GetAllRelicItems() 
    {
        return D_RelicItemData.FindEntities(data => true);
    }
    
    public static List<D_RelicItemData> GetOwnedRelicItems() 
    {
        List<D_RelicItemData> relicItems = new List<D_RelicItemData>();
        
        D_RelicItemData.ForEachEntity(data =>
        {
            var relicItem = D_U_RelicData.GetEntity(data.Id);
            if (relicItem != null && relicItem.f_level > 0)
            {
                relicItems.Add(data);
            }
        });

        return relicItems;
    }

    public static D_RelicItemData GetNextRelicItemData(D_RelicItemData currentRelicItem)
    {
        if (currentRelicItem.Index == GetAllRelicItems().Count - 1)
        {
            return GetEntity(0); // ���� �ε����� �������̶�� 0�� ��ƼƼ ��ȯ
        }
        
        var nextRelicItem = GetEntity(currentRelicItem.Index + 1);

        // �����ΰ� �߸��� �����.. �߻��ؼ��� �ȵǴ�..
        if (nextRelicItem == null)
        {
            Debug.LogError("Next relic item is null");
            return null;
        }

        return nextRelicItem;
    }
    
    public static D_RelicItemData GetPreviousRelicItemData(D_RelicItemData currentRelicItem)
    {
        if (currentRelicItem.Index == 0)
        {
            return GetEntity(GetAllRelicItems().Count - 1); // ���� �ε����� 0�̶�� ������ ��ƼƼ ��ȯ
        }
        
        var previousRelicItem = GetEntity(currentRelicItem.Index - 1);
        
        // �����ΰ� �߸��� �����.. �߻��ؼ��� �ȵǴ�..
        if (previousRelicItem == null)
        {
            Debug.LogError("Previous relic item is null");
            return null;
        }

        return previousRelicItem;
    }
}
