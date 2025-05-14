using System;
using AutoBattle.Scripts.DataController;
using AutoBattle.Scripts.Managers;
using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBattle.Scripts.UI.UIComponents
{
    public class RelicItemComponent : MonoBehaviour, IRelicStateChangeSubscriber
    {
        [SerializeField] private Image relicActiveIcon;
        [SerializeField] private Image relicInactiveIcon;

        [SerializeField] private GameObject relicDimObject;
        
        [SerializeField] private Slider relicExpSlider;
        [SerializeField] private TMP_Text relicExpText;
        
        private BGId relicId;
        private string relicName;
        private string iconKey;
        private int relicLevel;
        private int relicExp;
        private Grade relicGrade;
        private string relicDescription;
        
        private Action<RelicItemDataParam> onClickCallback;
        
        public void OnRelicStateChange(RelicStateChangeEvent relicStateChangeEvent)
        {
            if (relicStateChangeEvent.TargetRelicId != relicId) return;

            switch (relicStateChangeEvent.EventType)
            {
                case RelicStateEventType.Add:
                    break;
                case RelicStateEventType.LevelUp:
                    break;
                case RelicStateEventType.Equip:
                    break;
                case RelicStateEventType.UnEquip:
                    break;
            }
            
            var currentRelic = D_RelicItemData.FindEntity(data => data.Id == relicId);
            
            UpdateData(new RelicItemDataParam(currentRelic.Id, currentRelic.Name, currentRelic.f_grade, currentRelic.f_description));
        }
        
        public void SetData(RelicItemDataParam param, Action<RelicItemDataParam> onClickAction)
        {
            relicId = param.RelicId;
            relicName = param.RelicName;
            iconKey = param.IconKey;
            relicLevel = param.RelicLevel;
            relicExp = param.RelicExp;
            relicGrade = param.RelicGrade;
            relicDescription = param.RelicDescription;
            onClickCallback = onClickAction;
            
            UpdateUI();
        }

        public void OnClickRelicItemComponent()
        {
            onClickCallback?.Invoke(new RelicItemDataParam(relicId, relicName, relicGrade, relicDescription));
        }

        private void UpdateData(RelicItemDataParam param)
        {
            relicId = param.RelicId;
            relicName = param.RelicName;
            relicLevel = param.RelicLevel;
            relicExp = param.RelicExp;
            relicGrade = param.RelicGrade;
            relicDescription = param.RelicDescription;
            
            UpdateUI();
        }

        private void UpdateUI()
        {
            relicDimObject.SetActive(relicLevel <= 0);
            relicInactiveIcon.sprite = AtlasManager.Instance.GetItemIcon(iconKey);

            if (relicDimObject.activeSelf == false)
            {
                var expData = D_RelicItemExpData.GetRelicItemExpData(relicLevel);

                relicActiveIcon.sprite = AtlasManager.Instance.GetItemIcon(iconKey);
                
                relicExpSlider.value = expData.f_maxExp == 0 ? 0 : (float)relicExp / expData.f_maxExp;
                relicExpText.text = $"{relicExp} / {expData.f_maxExp}";
            }
        }
    }

    public class RelicItemDataParam
    {
        public BGId RelicId;
        public string RelicName;
        public string IconKey;
        public int RelicLevel;
        public int RelicExp;
        public Grade RelicGrade;
        public string RelicDescription;
        
        public RelicItemDataParam(BGId relicId, string relicName, Grade relicGrade, string relicDescription)
        {
            var userRelicData = D_U_RelicData.FindEntity(data => data.f_relicData.Id == relicId);
            var relicData = D_RelicItemData.FindEntity(data => data.Id == relicId);
            
            RelicId = relicId;
            RelicName = relicName;
            IconKey = relicData.f_iconKey;
            RelicLevel = userRelicData?.f_level ?? 0;
            RelicExp = userRelicData?.f_exp ?? 0;
            RelicGrade = relicGrade;
            RelicDescription = relicDescription;
        }
    }
}