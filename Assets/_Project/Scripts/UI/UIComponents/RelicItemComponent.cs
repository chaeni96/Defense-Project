using System;
using BGDatabaseEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBattle.Scripts.UI.UIComponents
{
    public class RelicItemComponent : MonoBehaviour
    {
        [SerializeField] private Image relicActiveIcon;
        [SerializeField] private Image relicInactiveIcon;

        [SerializeField] private GameObject relicDimObject;
        
        [SerializeField] private Slider relicExpSlider;
        [SerializeField] private TMP_Text relicExpText;
        
        private string relicName;
        private int relicLevel;
        private int relicExp;
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
            onClickCallback?.Invoke(new RelicItemDataParam(relicName, relicLevel, relicExp, relicGrade, relicDescription));
        }

        private void UpdateUI()
        {
            relicDimObject.SetActive(relicLevel <= 0);

            if (relicDimObject.activeSelf == false)
            {
                relicExpSlider.value = (float)relicExp / 2;
                relicExpText.text = $"{relicExp} / 2";
            }
        }
    }

    public class RelicItemDataParam
    {
        public string RelicName;
        public int RelicLevel;
        public int RelicExp;
        public Grade RelicGrade;
        public string RelicDescription;
        
        public RelicItemDataParam(string relicName, int relicLevel, int relicExp, Grade relicGrade, string relicDescription)
        {
            RelicName = relicName;
            RelicLevel = relicLevel;
            RelicExp = relicExp;
            RelicGrade = relicGrade;
            RelicDescription = relicDescription;
        }
    }
}