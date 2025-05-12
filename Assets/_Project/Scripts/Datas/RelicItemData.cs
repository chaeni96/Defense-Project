using System.Collections.Generic;
using BansheeGz.BGDatabase;
using UnityEngine;

public partial class D_RelicItemData
{
    public static List<D_RelicItemData> GetAllRelicItems() 
    {
        return D_RelicItemData.FindEntities(data => true);
    }
    
    public static List<StatStorage> GetRelicEffectStats(BGId relicId)
    {
        var relicItem = GetEntity(relicId);
        if (relicItem == null)
        {
            Debug.LogError($"Relic item with ID {relicId} not found.");
            return null;
        }

        var relicEffectStats = new List<StatStorage>();
        
        foreach (var effect in relicItem.f_relicEffectStats)
        {
            var statStorage = new StatStorage()
            {
                statName = effect.f_statName,
                value = effect.f_statValue,
                multiply = effect.f_statMultiply,
            };
            
            relicEffectStats.Add(statStorage);
        }

        return relicEffectStats;
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
            return GetEntity(0); // 현재 인덱스가 마지막이라면 0번 엔티티 반환
        }
        
        var nextRelicItem = GetEntity(currentRelicItem.Index + 1);

        // 무엇인가 잘못된 경우임.. 발생해서는 안되는..
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
            return GetEntity(GetAllRelicItems().Count - 1); // 현재 인덱스가 0이라면 마지막 엔티티 반환
        }
        
        var previousRelicItem = GetEntity(currentRelicItem.Index - 1);
        
        // 무엇인가 잘못된 경우임.. 발생해서는 안되는..
        if (previousRelicItem == null)
        {
            Debug.LogError("Previous relic item is null");
            return null;
        }

        return previousRelicItem;
    }
}
