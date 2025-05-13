using AutoBattle.Scripts.UI;
using AutoBattle.Scripts.UI.UIComponents;
using AutoBattle.Scripts.Utils;
using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using BGDatabaseEnum.DataController;
using UnityEngine;

[UIInfo("RelicInventoryUI", "RelicInventoryUI", false)]
public class RelicInventoryUI : FloatingPopupBase, IRelicStateChangeSubscriber
{
    [SerializeField] private RelicItemComponent relicItemComponent;

    [Header("Item Grid Parent")]
    [SerializeField] private Transform normalRelicItemParent;
    [SerializeField] private Transform rareRelicItemParent;
    [SerializeField] private Transform epicRelicItemParent;
    [SerializeField] private Transform legendaryRelicItemParent;
    [SerializeField] private Transform mythicRelicItemParent;
    
    [Header("Equipped Relic Slots")]
    [SerializeField] private RelicEquipSlotComponent[] relicEquipSlots;
    
    private FullWindowLobbyDlg lobbyDlg;
    
    // 현재 장착 모드에서 선택된 유물 ID
    private BGId selectedRelicForEquip = BGId.Empty;
    
    public override void InitializeUI()
    {
        base.InitializeUI();

        InitializeRelicItemUI();
        InitializeEquipSlots();
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
            RelicDataController.Instance.AddRelicItem(result);
        }
    }

    private void InitializeRelicItemUI()
    {
        var list = D_RelicItemData.GetAllRelicItems();
        
        foreach (var item in list)
        {
            var parent = GetParentByGrade(item);
            var itemComponent = Instantiate(relicItemComponent, parent);

            itemComponent.SetData(
                new RelicItemDataParam(item.Id, item.f_name, item.f_grade, item.f_description),
                OnClickRelicItem);
            
            RelicDataController.Instance.AddSubscriber(itemComponent);
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
    
    private void InitializeEquipSlots()
    {
        if (relicEquipSlots == null || relicEquipSlots.Length == 0)
        {
            Debug.LogWarning("Relic equip slots are not set up!");
            return;
        }
    
        // 각 슬롯 초기화
        for (int i = 0; i < relicEquipSlots.Length; i++)
        {
            if (relicEquipSlots[i] != null)
            {
                relicEquipSlots[i].Initialize(i);
                RelicDataController.Instance.AddSubscriber(relicEquipSlots[i]);
            }
        }
    }
    
    // 장착 모드 활성화
    private void ActivateEquipMode(BGId relicId)
    {
        selectedRelicForEquip = relicId;
        
        // 모든 장착 슬롯의 인디케이터 활성화
        foreach (var slot in relicEquipSlots)
        {
            if (slot != null)
            {
                slot.ActivateIndicator(true);
            }
        }
    }

    // 장착 모드 비활성화
    public void DeactivateEquipMode()
    {
        selectedRelicForEquip = BGId.Empty;
        
        // 모든 장착 슬롯의 인디케이터 비활성화
        foreach (var slot in relicEquipSlots)
        {
            if (slot != null)
            {
                slot.ActivateIndicator(false);
            }
        }
    }

    private async void OnClickRelicItem(RelicItemDataParam param)
    {
        var popup = await UIManager.Instance.ShowUI<RelicItemInfoPopup>();
        popup.SetData(param);
        popup.SetOnClickEquipButtonAction(ActivateEquipMode);
    }
    
    // 유물 장착 처리
    public void EquipRelicToSlot(int slotIndex)
    {
        if (selectedRelicForEquip == BGId.Empty)
            return;
    
        // 유물 장착 처리
        RelicDataController.Instance.EquipRelic(selectedRelicForEquip, slotIndex);
    
        // 장착 모드 비활성화
        DeactivateEquipMode();
    }

    public void OnRelicStateChange(RelicStateChangeEvent relicStateChangeEvent)
    {
        switch (relicStateChangeEvent.EventType)
        {
            case RelicStateEventType.Equip:
                break;
            case RelicStateEventType.UnEquip:
                break;
        }
    }
    
#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            SaveLoadManager.Instance.SaveData();
        }
    }
#endif
}
