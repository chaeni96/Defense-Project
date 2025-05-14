using System;
using AutoBattle.Scripts.DataController;
using AutoBattle.Scripts.Managers;
using AutoBattle.Scripts.UI.UIComponents;
using AutoBattle.Scripts.Utils;
using BansheeGz.BGDatabase;
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
        [SerializeField] private GameObject relicUpgradeDimButton;
        [SerializeField] private TMP_Text relicUpgradeCostText;
        
        [Header("Equip Button")]
        [SerializeField] private Button equipButton;
        [SerializeField] private TMP_Text equipButtonText;
        [SerializeField] private GameObject equipDimObject;
        
        [Header("Etc")]
        [SerializeField] private Color normalColor;
        [SerializeField] private Color rareColor;
        [SerializeField] private Color epicColor;
        [SerializeField] private Color legendaryColor;
        [SerializeField] private Color mythicColor;
        
        private RelicItemDataParam relicItemDataParam;

        private event Action<BGId> onClickEquipButton;
        
        public void SetData(RelicItemDataParam param)
        {
            relicItemDataParam = param;
            
            UpdateUI();
        }

        public void SetOnClickEquipButtonAction(Action<BGId> action)
        {
            onClickEquipButton = action;
        }

        public void OnClickUpgradeButton()
        {
            Debug.Log("Upgrade button clicked");
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

            SetData(new RelicItemDataParam(nextRelic.Id, nextRelic.f_name, nextRelic.f_grade, nextRelic.f_description));
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

            SetData(new RelicItemDataParam(previousRelic.Id, previousRelic.f_name, previousRelic.f_grade, previousRelic.f_description));
        }
        
        // 장착 버튼 클릭 이벤트
        public void OnClickEquipButton()
        {
            BGId relicId = relicItemDataParam.RelicId;
    
            // 이미 장착된 유물인지 확인
            if (RelicDataController.Instance.IsRelicEquipped(relicId))
            {
                // 이미 장착된 경우 장착 해제
                RelicDataController.Instance.UnequipRelic(relicId);
                UpdateEquipButtonText(false);
            }
            else
            {
                onClickEquipButton?.Invoke(relicId);
            }
    
            // 팝업 닫기
            OnClickClose();
        }

        // 장착 버튼 텍스트 업데이트
        private void UpdateEquipButtonText(bool isEquipped)
        {
            if (equipButtonText != null)
            {
                equipButtonText.text = isEquipped ? "해제" : relicItemDataParam.RelicLevel > 0 ? "장착" : "미보유";
            }
        }

        private void UpdateUI()
        {
            relicIconImage.sprite = AtlasManager.Instance.GetItemIcon(relicItemDataParam.IconKey);
            
            relicNameText.text = relicItemDataParam.RelicName;
            relicDescriptionText.text = relicItemDataParam.RelicDescription;
            
            relicLevelText.text = $"Lv. {relicItemDataParam.RelicLevel}";
            
            var currentRelic = D_U_RelicData.FindEntity(data => data.f_relicData.Id == relicItemDataParam.RelicId);
            relicExpText.text = currentRelic == null ? "없음" : $"{relicItemDataParam.RelicExp} / {D_RelicItemExpData.GetRelicItemExpData(currentRelic.f_level).f_maxExp}";
            relicExpSlider.value = currentRelic == null ? 0 : (float)relicItemDataParam.RelicExp / D_RelicItemExpData.GetRelicItemExpData(currentRelic.f_level).f_maxExp;
            
            relicGradeText.text = $"{CommonUtil.GetGradeName(relicItemDataParam.RelicGrade)}";
            relicGradeLabelImage.color = GetColorByGrade(relicItemDataParam.RelicGrade);

            relicUpgradeDimButton.SetActive(relicItemDataParam.RelicLevel <= 0);
            
            equipDimObject.SetActive(relicItemDataParam.RelicLevel <= 0);
            UpdateEquipButtonText(currentRelic != null && currentRelic.f_isEquiped);

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

        private void OnDisable()
        {
            onClickEquipButton = null;
        }
    }
}