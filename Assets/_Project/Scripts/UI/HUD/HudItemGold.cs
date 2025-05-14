using AutoBattle.Scripts.DataController;
using TMPro;
using UnityEngine;

namespace AutoBattle.Scripts.UI.HUD
{
    public class HudItemGold : HudItemBase, ICurrencyChangedSubscriber
    {
        [SerializeField] private TMP_Text amountText;
        
        public void OnCurrencyChanged(CurrencyChangedEvent currencyChangedEvent)
        {
            if(currencyChangedEvent.currencyType != CurrencyType.Gold) return;
            
            if (amountText)
            {
                amountText.text = currencyChangedEvent.afterValue.ToString();
            }
        }

        protected override void OnInitialize()
        {
            CurrencyDataController.Instance.Subscribe(CurrencyType.Gold, this);
        }

        protected override void OnClickHudItemEvent()
        {
            CurrencyDataController.Instance.AddCurrency(CurrencyType.Gold, 100);
        }

        protected override void OnRemoveEvent()
        {
            CurrencyDataController.Instance.Unsubscribe(this);
        }
    }
}