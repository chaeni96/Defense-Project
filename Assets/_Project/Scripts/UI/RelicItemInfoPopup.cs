using AutoBattle.Scripts.UI.UIComponents;
using AutoBattle.Scripts.Utils;
using BGDatabaseEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBattle.Scripts.UI
{
    [UIInfo("RelicItemInfoPopup", "RelicItemInfoPopup", false)]
    public class RelicItemInfoPopup : PopupBase
    {
        [Header("Main Info")]
        [SerializeField] private Image relicIconImage;
        [SerializeField] private TMP_Text relicNameText;
        [SerializeField] private TMP_Text relicDescriptionText;
        [SerializeField] private TMP_Text relicGradeText;
        [SerializeField] private Image relicGradeLabelImage;
        
        [Header("Relic Level Info")]
        [SerializeField] private Slider relicExpSlider;
        [SerializeField] private TMP_Text relicExpText;
        [SerializeField] private TMP_Text relicLevelText;
        
        [Header("Relic Upgrade Info")]
        [SerializeField] private TMP_Text relicUpgradeCostText;
        
        [Header("Etc")]
        [SerializeField] private Color normalColor;
        [SerializeField] private Color rareColor;
        [SerializeField] private Color epicColor;
        [SerializeField] private Color legendaryColor;
        [SerializeField] private Color mythicColor;
        
        private RelicItemDataParam relicItemDataParam;
        
        public override void InitializeUI()
        {
            base.InitializeUI();
        }
        
        public void SetData(RelicItemDataParam param)
        {
            relicItemDataParam = param;
            
            UpdateUI();
        }

        public void OnClickUpgradeButton()
        {
            
        }
        
        public void OnClickRightButton()
        {
            var currentRelic = D_RelicItemData.GetEntity(relicItemDataParam.RelicId);
            if (currentRelic == null)
            {
                Debug.LogError("Current relic is null");
                return;
            }
            
            var nextRelic = D_RelicItemData.GetNextRelicItemData(currentRelic);

            SetData(new RelicItemDataParam(nextRelic.Id, nextRelic.f_name, nextRelic.f_level, nextRelic.f_exp,
                nextRelic.f_grade, nextRelic.f_description));
        }
        
        public void OnClickLeftButton()
        {
            var currentRelic = D_RelicItemData.GetEntity(relicItemDataParam.RelicId);
            if (currentRelic == null)
            {
                Debug.LogError("Current relic is null");
                return;
            }
            
            var previousRelic = D_RelicItemData.GetPreviousRelicItemData(currentRelic);

            SetData(new RelicItemDataParam(previousRelic.Id, previousRelic.f_name, previousRelic.f_level, previousRelic.f_exp,
                previousRelic.f_grade, previousRelic.f_description));
        }

        private void UpdateUI()
        {
            relicNameText.text = relicItemDataParam.RelicName;
            relicDescriptionText.text = relicItemDataParam.RelicDescription;
            relicLevelText.text = $"Lv. {relicItemDataParam.RelicLevel}";
            relicExpText.text = $"{relicItemDataParam.RelicExp} / 2";
            relicExpSlider.value = (float)relicItemDataParam.RelicExp / 2;
            relicGradeText.text = $"{CommonUtil.GetGradeName(relicItemDataParam.RelicGrade)}";
            relicGradeLabelImage.color = GetColorByGrade(relicItemDataParam.RelicGrade);

            return;
            // ===================================================================
            Color GetColorByGrade(Grade grade)
            {
                return grade switch
                {
                    Grade.Normal => normalColor,
                    Grade.Rare => rareColor,
                    Grade.Epic => epicColor,
                    Grade.Legendary => legendaryColor,
                    Grade.Mythic => mythicColor,
                    _ => Color.white
                };
            }
        }
    }
}