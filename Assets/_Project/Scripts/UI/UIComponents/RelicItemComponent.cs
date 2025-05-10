using System;
using BGDatabaseEnum;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBattle.Scripts.UI.UIComponents
{
    public class RelicItemComponent : MonoBehaviour
    {
        [SerializeField]
        private Image relicIcon;
        
        private string relicName;
        private Grade relicGrade;
        private string relicDescription;
        
        private Action<RelicItemDataParam> onClickCallback;
        
        public void SetData(RelicItemDataParam param, Action<RelicItemDataParam> onClickCallback)
        {
            relicName = param.RelicName;
            relicGrade = param.RelicGrade;
            relicDescription = param.RelicDescription;
            this.onClickCallback = onClickCallback;
            
            UpdateUI();
        }

        public void OnClickRelicItemComponent()
        {
            onClickCallback?.Invoke(new RelicItemDataParam(relicName, relicGrade, relicDescription));
        }

        private void UpdateUI()
        {
            
        }
    }

    public class RelicItemDataParam
    {
        public string RelicName;
        public Grade RelicGrade;
        public string RelicDescription;
        
        public RelicItemDataParam(string relicName, Grade relicGrade, string relicDescription)
        {
            RelicName = relicName;
            RelicGrade = relicGrade;
            RelicDescription = relicDescription;
        }
    }
}