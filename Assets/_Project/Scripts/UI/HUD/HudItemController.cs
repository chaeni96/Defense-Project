using UnityEngine;

namespace AutoBattle.Scripts.UI.HUD
{
    public class HudItemController : MonoBehaviour
    {
        [SerializeField]
        private HudItemBase[] hudItems;

        public void Initialize()
        {
            foreach (var hudItem in hudItems)
            {
                hudItem.Initialize(this);
            }
        }
    }
}