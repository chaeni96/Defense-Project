using System.Collections.Generic;
using AutoBattle.Scripts.Utils;

namespace AutoBattle.Scripts.DataController
{
    public struct CurrencyChangedEvent
    {
        public CurrencyType currencyType { get; }
        public long beforeValue { get; }
        public long afterValue { get; }
        
        public CurrencyChangedEvent(CurrencyType currencyType, long beforeValue, long afterValue)
        {
            this.currencyType = currencyType;
            this.beforeValue = beforeValue;
            this.afterValue = afterValue;
        }
    }
    
    public interface ICurrencyChangedSubscriber
    {
        void OnCurrencyChanged(CurrencyChangedEvent currencyChangedEvent);
    }
    
    public class CurrencyDataController : Singleton<CurrencyDataController>
    {
        private List<ICurrencyChangedSubscriber> subscribers = new();

        public void InitializeController()
        {
            subscribers.Clear();
        }

        public D_U_CurrencyData GetCurrencyData(CurrencyType currencyType)
        {
            var currencyData = D_U_CurrencyData.GetEntity(currencyType.ToString());
            if (currencyData == null) return null;

            return currencyData;
        }
        
        public bool AddCurrency(CurrencyType currencyType, long amount)
        {
            var currencyData = D_U_CurrencyData.GetEntity(currencyType.ToString());
            if (currencyData == null) return false;

            var beforeValue = currencyData.f_amount;
            currencyData.f_amount += amount;
            var afterValue = currencyData.f_amount;
            
            SaveLoadManager.Instance.SaveData();

            NotifyCurrencyChanged(new CurrencyChangedEvent(currencyType, beforeValue, afterValue));
            return true;
        }
        
        public int GetCurrencyRefreshTime(CurrencyType currencyType)
        {
            var currencyData = D_U_CurrencyData.GetEntity(currencyType.ToString());
            if (currencyData == null) return 0;

            return currencyData.f_refreshTime;
        }
        
        public void Subscribe(CurrencyType currencyType, ICurrencyChangedSubscriber subscriber)
        {
            if (!subscribers.Contains(subscriber))
            {
                subscribers.Add(subscriber);

                var currencyData = D_U_CurrencyData.GetEntity(currencyType.ToString());
                
                NotifyCurrencyChanged(new CurrencyChangedEvent(currencyType, -1, currencyData.f_amount));
            }
        }
        
        public void Unsubscribe(ICurrencyChangedSubscriber subscriber)
        {
            if (subscribers.Contains(subscriber))
            {
                subscribers.Remove(subscriber);
            }
        }

        private void NotifyCurrencyChanged(CurrencyChangedEvent currencyChangedEvent)
        {
            foreach (var subscriber in subscribers)
            {
                subscriber.OnCurrencyChanged(currencyChangedEvent);
            }
        }
        
    }
}