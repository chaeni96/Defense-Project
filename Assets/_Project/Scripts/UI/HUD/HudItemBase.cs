using UnityEngine;

namespace AutoBattle.Scripts.UI.HUD
{
    public class HudItemBase : MonoBehaviour
    {
        protected HudItemController hudItemController;
        
        public void Initialize(HudItemController controller)
        {
            hudItemController = controller;
            OnInitialize();
        }

        public void OnClickHudItem()
        {
            OnClickHudItemEvent();
        }
        
        protected virtual void OnEnable()
        {
            OnAddEvent();
        }

        protected virtual void OnDisable()
        {
            OnRemoveEvent();
        }
        
        protected virtual void OnInitialize() { }

        protected virtual void OnClickHudItemEvent() { }
        
        protected virtual void OnAddEvent() { }

        protected virtual void OnRemoveEvent() { }
    }
}