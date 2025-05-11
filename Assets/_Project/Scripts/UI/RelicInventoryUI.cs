using AutoBattle.Scripts.UI;
using AutoBattle.Scripts.UI.UIComponents;
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

    public override void InitializeUI()
    {
        base.InitializeUI();

        InitializeRelicItemUI();
    }

    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnClickCancelBtn()
    {
        UIManager.Instance.CloseUI<RelicInventoryUI>();
    }

    private void InitializeRelicItemUI()
    {
        var list = D_RelicItemData.GetAllRelicItems();
        
        foreach (var item in list)
        {
            var parent = GetParentByGrade(item);
            var itemComponent = Instantiate(relicItemComponent, parent);
            itemComponent.SetData(
                new RelicItemDataParam(item.Id, item.f_name, item.f_level, item.f_exp, item.f_grade, item.f_description),
                OnClickRelicItem);
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

    private async void OnClickRelicItem(RelicItemDataParam param)
    {
        var popup = await UIManager.Instance.ShowUI<RelicItemInfoPopup>();
        popup.InitializeUI();
        popup.SetData(param);
    }
}
