using AutoBattle.Scripts.UI.UIComponents;
using BGDatabaseEnum;
using TMPro;
using UnityEngine;

[UIInfo("RelicInventoryUI", "RelicInventoryUI", false)]

public class RelicInventoryUI : FloatingPopupBase
{
    [SerializeField] private RelicItemComponent relicItemComponent;

    [Header("Item Grid Parent")]
    [SerializeField] private Transform normalRelicItemParent;
    [SerializeField] private Transform rareRelicItemParent;
    [SerializeField] private Transform epicRelicItemParent;
    [SerializeField] private Transform legendaryRelicItemParent;
    [SerializeField] private Transform mythicRelicItemParent;
    
    [Header("Relic Description")]
    [SerializeField] private TMP_Text currentSelectedRelicName;
    [SerializeField] private TMP_Text currentSelectedRelicDescription;
    
    private FullWindowLobbyDlg lobbyDlg;

    public override void InitializeUI()
    {
        base.InitializeUI();

        InitializeRelicItemUI();
    }

    public void InitLobbyDlg(FullWindowLobbyDlg lobby)
    {
        lobbyDlg = lobby;
    }

    public override void HideUI()
    {
        base.HideUI();
    }

    public void OnClickCancelBtn()
    {
        UIManager.Instance.CloseUI<RelicInventoryUI>();
        lobbyDlg.SwitchToCampPanel();
    }

    private void InitializeRelicItemUI()
    {
        var list = D_RelicItemData.GetAllRelicItems();
        
        foreach (var item in list)
        {
            var parent = GetParentByGrade(item);
            var itemComponent = Instantiate(relicItemComponent, parent);
            itemComponent.SetData(
                new RelicItemDataParam(item.f_name, item.f_level, item.f_exp, item.f_grade, item.f_description),
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

    private void OnClickRelicItem(RelicItemDataParam param)
    {
        currentSelectedRelicName.text = $"º±≈√ : {param.RelicName}";
        currentSelectedRelicDescription.text = param.RelicDescription;
    }
}
