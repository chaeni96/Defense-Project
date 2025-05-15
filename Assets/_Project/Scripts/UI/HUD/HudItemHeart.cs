using System.Collections;
using AutoBattle.Scripts.DataController;
using AutoBattle.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace AutoBattle.Scripts.UI.HUD
{
    public class HudItemHeart : HudItemBase, ICurrencyChangedSubscriber
    {
        private const int MAX_HEART_AMOUNT = 5;
        
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text refreshTimeText;
        
        public void OnCurrencyChanged(CurrencyChangedEvent currencyChangedEvent)
        {
            if(currencyChangedEvent.currencyType != CurrencyType.Heart) return;

            if (currencyChangedEvent is { beforeValue: MAX_HEART_AMOUNT, afterValue: < MAX_HEART_AMOUNT } or { beforeValue: -1, afterValue: < MAX_HEART_AMOUNT })
            {
                // 코루틴 실행
                StartCoroutine(RefreshTimeCoroutine());
            }
            
            if(currencyChangedEvent.beforeValue != MAX_HEART_AMOUNT && currencyChangedEvent.afterValue == MAX_HEART_AMOUNT)
            {
                // 코루틴 중지
                StopCoroutine(RefreshTimeCoroutine());
                refreshTimeText.text = "FULL";
            }
            
            if (amountText)
            {
                amountText.text = currencyChangedEvent.afterValue.ToString();
            }
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            CurrencyDataController.Instance.Subscribe(CurrencyType.Heart, this);
        }

        protected override void OnClickHudItemEvent()
        {
            var currentHeart = CurrencyDataController.Instance.GetCurrencyData(CurrencyType.Heart);
            
            if(currentHeart.f_amount == MAX_HEART_AMOUNT) return;
            
            CurrencyDataController.Instance.AddCurrency(CurrencyType.Heart, 1);
        }

        protected override void OnRemoveEvent()
        {
            CurrencyDataController.Instance.Unsubscribe(this);
            StopCoroutine(RefreshTimeCoroutine());
        }

        private IEnumerator RefreshTimeCoroutine()
        {
            var refreshTime = CurrencyDataController.Instance.GetCurrencyRefreshTime(CurrencyType.Heart);
            
            if(refreshTime == -1) yield break;
            
            var time = refreshTime;
            while (time > 0)
            {
                refreshTimeText.text = CommonUtil.GetTimeText(time);
                yield return new WaitForSeconds(1f);
                time--;
            }
            
            CurrencyDataController.Instance.AddCurrency(CurrencyType.Heart, 1);
        }
    }
}