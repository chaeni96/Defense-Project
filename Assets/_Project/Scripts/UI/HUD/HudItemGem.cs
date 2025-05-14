using AutoBattle.Scripts.DataController;
using TMPro;
using UnityEngine;

namespace AutoBattle.Scripts.UI.HUD
{
    public class HudItemGem : HudItemBase, ICurrencyChangedSubscriber
    {
        [SerializeField] private TMP_Text amountText;
        
        public void OnCurrencyChanged(CurrencyChangedEvent currencyChangedEvent)
        {
            if(currencyChangedEvent.currencyType != CurrencyType.Gem) return;
            
            if (amountText)
            {
                amountText.text = currencyChangedEvent.afterValue.ToString();
            }
        }
        
        protected override void OnInitialize()
        {
            CurrencyDataController.Instance.Subscribe(CurrencyType.Gem, this);
        }

        protected override void OnClickHudItemEvent()
        {
            CurrencyDataController.Instance.AddCurrency(CurrencyType.Gem, 10);
        }

        protected override void OnRemoveEvent()
        {
            CurrencyDataController.Instance.Unsubscribe(this);
        }
    }
}