using AutoBattle.Scripts.DataController;
using AutoBattle.Scripts.UI;
using AutoBattle.Scripts.UI.UIComponents;
using AutoBattle.Scripts.Utils;
using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using TMPro;
using UnityEngine;

[UIInfo("RelicInventoryUI", "RelicInventoryUI", false)]
public class RelicInventoryUI : FloatingPopupBase, IRelicStateChangeSubscriber, ICurrencyChangedSubscriber
{
    private const int RELIC_SUMMON_COST = 10;
    
    [SerializeField] private RelicItemComponent relicItemComponent;

    [Header("Item Grid Parent")]
    [SerializeField] private Transform normalRelicItemParent;
    [SerializeField] private Transform rareRelicItemParent;
    [SerializeField] private Transform epicRelicItemParent;
    [SerializeField] private Transform legendaryRelicItemParent;
    [SerializeField] private Transform mythicRelicItemParent;
    
    [Header("Equipped Relic Slots")]
    [SerializeField] private RelicEquipSlotComponent[] relicEquipSlots;
    
    [Header("Currency Info")]
    [SerializeField] private TMP_Text gemAmountText;
    
    private FullWindowLobbyDlg lobbyDlg;
    
    // ���� ���� ��忡�� ���õ� ���� ID
    private BGId selectedRelicForEquip = BGId.Empty;
    
    public override void InitializeUI()
    {
        base.InitializeUI();

        InitializeRelicItemUI();
        InitializeEquipSlots();
        InitializeCurrencyUI();
    }

    public override void HideUI()
    {
        base.HideUI();
        
        CurrencyDataController.Instance.Unsubscribe(this);
        
        SaveLoadManager.Instance.SaveData();
    }

    public void OnClickCancelBtn()
    {
        UIManager.Instance.CloseUI<RelicInventoryUI>();
    }

    public void OnClickGetRelicButton()
    {
        var currencyData = CurrencyDataController.Instance.GetCurrencyData(CurrencyType.Gem);

        if (currencyData.f_amount < RELIC_SUMMON_COST) return;
        
        // 10�� �Ҹ�
        CurrencyDataController.Instance.AddCurrency(CurrencyType.Gem, -RELIC_SUMMON_COST);
        
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
    
        // �� ���� �ʱ�ȭ
        for (int i = 0; i < relicEquipSlots.Length; i++)
        {
            if (relicEquipSlots[i] != null)
            {
                relicEquipSlots[i].Initialize(i);
                RelicDataController.Instance.AddSubscriber(relicEquipSlots[i]);
            }
        }
    }

    private void InitializeCurrencyUI()
    {
        CurrencyDataController.Instance.Subscribe(CurrencyType.Gem, this);
        
        var currencyData = CurrencyDataController.Instance.GetCurrencyData(CurrencyType.Gem);
        gemAmountText.text = currencyData.f_amount.ToString();
    }
    
    // ���� ��� Ȱ��ȭ
    private void ActivateEquipMode(BGId relicId)
    {
        selectedRelicForEquip = relicId;
        
        // ��� ���� ������ �ε������� Ȱ��ȭ
        foreach (var slot in relicEquipSlots)
        {
            if (slot != null)
            {
                slot.ActivateIndicator(true);
            }
        }
    }

    // ���� ��� ��Ȱ��ȭ
    public void DeactivateEquipMode()
    {
        selectedRelicForEquip = BGId.Empty;
        
        // ��� ���� ������ �ε������� ��Ȱ��ȭ
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
    
    // ���� ���� ó��
    public void EquipRelicToSlot(int slotIndex)
    {
        if (selectedRelicForEquip == BGId.Empty)
            return;
    
        // ���� ���� ó��
        RelicDataController.Instance.EquipRelic(selectedRelicForEquip, slotIndex);
    
        // ���� ��� ��Ȱ��ȭ
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
    
    public void OnCurrencyChanged(CurrencyChangedEvent currencyChangedEvent)
    {
        if(currencyChangedEvent.currencyType != CurrencyType.Gem) return;
        
        InitializeCurrencyUI();
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
