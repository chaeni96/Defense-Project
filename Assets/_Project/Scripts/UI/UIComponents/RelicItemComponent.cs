using System;
using BansheeGz.BGDatabase;
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
        
        private BGId relicId;
        private string relicName;
        private int relicLevel;
        private int relicExp;
        private Grade relicGrade;
        private string relicDescription;
        
        private Action<RelicItemDataParam> onClickCallback;
        
        public void SetData(RelicItemDataParam param, Action<RelicItemDataParam> onClickAction)
        {
            relicId = param.RelicId;
            relicName = param.RelicName;
            relicLevel = param.RelicLevel;
            relicExp = param.RelicExp;
            relicGrade = param.RelicGrade;
            relicDescription = param.RelicDescription;
            onClickCallback = onClickAction;
            
            UpdateUI();
        }

        public void UpdateData(RelicItemDataParam param)
        {
            relicId = param.RelicId;
            relicName = param.RelicName;
            relicLevel = param.RelicLevel;
            relicExp = param.RelicExp;
            relicGrade = param.RelicGrade;
            relicDescription = param.RelicDescription;
            
            UpdateUI();
        }

        public void OnClickRelicItemComponent()
        {
            onClickCallback?.Invoke(new RelicItemDataParam(relicId, relicName, relicLevel, relicExp, relicGrade, relicDescription));
        }

        private void UpdateUI()
        {
            relicDimObject.SetActive(relicLevel <= 0);

            if (relicDimObject.activeSelf == false)
            {
                var currentRelic = D_RelicItemData.GetEntity(relicId);
                var expData = D_RelicItemExpData.GetRelicItemExpData(currentRelic);
                
                relicExpSlider.value = expData.f_maxExp == 0 ? 0 : (float)relicExp / expData.f_maxExp;
                relicExpText.text = $"{relicExp} / 2";
            }
        }
    }

    public class RelicItemDataParam
    {
        public BGId RelicId;
        public string RelicName;
        public int RelicLevel;
        public int RelicExp;
        public Grade RelicGrade;
        public string RelicDescription;
        
        public RelicItemDataParam(BGId relicId, string relicName, int relicLevel, int relicExp, Grade relicGrade, string relicDescription)
        {
            RelicId = relicId;
            RelicName = relicName;
            RelicLevel = relicLevel;
            RelicExp = relicExp;
            RelicGrade = relicGrade;
            RelicDescription = relicDescription;
        }
    }
}