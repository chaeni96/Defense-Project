using System.Collections.Generic;
using AutoBattle.Scripts.UI;
using AutoBattle.Scripts.UI.UIComponents;
using AutoBattle.Scripts.Utils;
using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using UnityEngine;

[UIInfo("RelicInventoryUI", "RelicInventoryUI", true)]

public class RelicInventoryUI : FloatingPopupBase
{
    [SerializeField] private RelicItemComponent relicItemComponent;

    [Header("Item Grid Parent")]
    [SerializeField] private Transform normalRelicItemParent;
    [SerializeField] private Transform rareRelicItemParent;
    [SerializeField] private Transform epicRelicItemParent;
    [SerializeField] private Transform legendaryRelicItemParent;
    [SerializeField] private Transform mythicRelicItemParent;
    
    private FullWindowLobbyDlg lobbyDlg;
    
    private Dictionary<BGId, RelicItemComponent> relicItemComponentDic = new();

    public override void InitializeUI()
    {
        base.InitializeUI();

        InitializeRelicItemUI();
    }

    public override void HideUI()
    {
        base.HideUI();
        SaveLoadManager.Instance.SaveData();
    }

    public void OnClickCancelBtn()
    {
        UIManager.Instance.CloseUI<RelicInventoryUI>();
    }

    public void OnClickGetRelicButton()
    {
        var result = D_RelicItemData.GetAllRelicItems().RandomRatePick(data => data.f_weight);

        if (result != null)
        {
            var relicItemExpData = D_RelicItemExpData.GetRelicItemExpData(result);
            
            if(relicItemExpData == null) return;
            
            result.f_exp += 1;

            if (result.f_exp >= relicItemExpData.f_maxExp)
            {
                result.f_level += 1;
                result.f_exp = 0;
            }

            UpdateRelicItemComponent(result.Id);
        }
    }

    private void InitializeRelicItemUI()
    {
        foreach (var componentPair in relicItemComponentDic)
        {
            Destroy(componentPair.Value.gameObject);
        }
        
        relicItemComponentDic.Clear();
        
        var list = D_RelicItemData.GetAllRelicItems();
        
        foreach (var item in list)
        {
            var parent = GetParentByGrade(item);
            var itemComponent = Instantiate(relicItemComponent, parent);
            itemComponent.SetData(
                new RelicItemDataParam(item.Id, item.f_name, item.f_level, item.f_exp, item.f_grade, item.f_description),
                OnClickRelicItem);
            
            relicItemComponentDic.Add(item.Id, itemComponent);
        }
        
        return;
        
        // ======================================================================
        Transform GetParentByGrade(D_RelicItemData data)
        {
            return data.f_grade switch
            {
                Grade.Normal => normalRelicItemParent,
                Grade.Rare => rareRelicItemParent,
                Grade.Epic => epicRelicItemParent,
                Grade.Legendary => legendaryRelicItemParent,
                Grade.Mythic => mythicRelicItemParent,
                _ => null
            };
        }
    }

    private void UpdateRelicItemComponent(BGId id)
    {
        if (relicItemComponentDic.TryGetValue(id, out var component))
        {
            var relicItemData = D_RelicItemData.GetEntity(id);
            component.UpdateData(new RelicItemDataParam(relicItemData.Id, relicItemData.f_name, relicItemData.f_level,
                relicItemData.f_exp, relicItemData.f_grade, relicItemData.f_description));
        }
    }

    private async void OnClickRelicItem(RelicItemDataParam param)
    {
        var popup = await UIManager.Instance.ShowUI<RelicItemInfoPopup>();
        popup.InitializeUI();
        popup.SetData(param);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            var allRelicItems = D_RelicItemData.GetAllRelicItems();

            foreach (var relicItemData in allRelicItems)
            {
                relicItemData.f_level = 0;
                relicItemData.f_exp = 0;
            }
            
            SaveLoadManager.Instance.SaveData();
        }
    }
}
