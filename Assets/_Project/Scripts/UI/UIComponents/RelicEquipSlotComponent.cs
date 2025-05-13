using AutoBattle.Scripts.Managers;
using BansheeGz.BGDatabase;
using BGDatabaseEnum.DataController;
using UnityEngine;
using UnityEngine.UI;

namespace AutoBattle.Scripts.UI.UIComponents
{
    public class RelicEquipSlotComponent : MonoBehaviour, IRelicStateChangeSubscriber
    {
        [SerializeField] private GameObject equippedRelicSlotObject;
        [SerializeField] private GameObject emptyRelicSlotObject;
        
        [SerializeField] private Image relicIconImage;

        [SerializeField] private GameObject indicaterObject;
        
        private int slotIndex;
        private BGId equippedRelicId = BGId.Empty;
        
        public void Initialize(int index)
        {
            slotIndex = index;
            UpdateSlotUI();
        }
        
        public void OnRelicStateChange(RelicStateChangeEvent relicStateChangeEvent)
        {
            // 슬롯에 장착된 유물이 변경되었을 때만 업데이트
            BGId currentEquippedRelic = RelicDataController.Instance.GetEquippedRelicAt(slotIndex);
            
            if (equippedRelicId != currentEquippedRelic || 
                (relicStateChangeEvent.TargetRelicId == equippedRelicId && 
                (relicStateChangeEvent.EventType == RelicStateEventType.Equip || 
                 relicStateChangeEvent.EventType == RelicStateEventType.UnEquip || 
                 relicStateChangeEvent.EventType == RelicStateEventType.LevelUp)))
            {
                UpdateSlotUI();
            }
        }
        
        public void ActivateIndicator(bool isActive)
        {
            indicaterObject.SetActive(isActive);
        }
        
        private void UpdateSlotUI()
        {
            indicaterObject.SetActive(false);
            
            // 슬롯에 장착된 유물 정보 가져오기
            equippedRelicId = RelicDataController.Instance.GetEquippedRelicAt(slotIndex);
            
            if (equippedRelicId == BGId.Empty)
            {
                // 빈 슬롯 표시
                relicIconImage.gameObject.SetActive(false);
                return;
            }
            
            // 장착된 유물 표시
            relicIconImage.gameObject.SetActive(true);
            
            // 유물 정보 가져오기
            var relicItemData = D_RelicItemData.GetEntity(equippedRelicId);
            if (relicItemData == null)
            {
                Debug.LogError($"Cannot find relic item data for ID: {equippedRelicId}");
                return;
            }
            
            // 아이콘 설정
            if (relicIconImage != null)
            {
                // 아이콘 로딩 로직 (프로젝트에 맞게 수정 필요)
                relicIconImage.sprite = AtlasManager.Instance.GetItemIcon(relicItemData.f_iconKey);
            }
        }
        
        // 슬롯 클릭 이벤트
        public void OnClickSlot()
        {
            // 인디케이터가 활성화된 상태라면 장착 모드이므로 무시
            if (indicaterObject.activeSelf)
            {
                return;
            }
            
            if (equippedRelicId == BGId.Empty)
            {
                // 빈 슬롯 클릭 - 장착 가능한 유물 선택 UI 열기 등의 로직
                return;
            }
            
            // 장착된 유물 정보 팝업 열기
            OnClickEquippedRelic(equippedRelicId);
        }
        
        private async void OnClickEquippedRelic(BGId relicId)
        {
            var relicItemData = D_RelicItemData.GetEntity(relicId);
            if (relicItemData == null) return;
            
            var param = new RelicItemDataParam(
                relicId, 
                relicItemData.f_name, 
                relicItemData.f_grade, 
                relicItemData.f_description
            );
            
            var popup = await UIManager.Instance.ShowUI<RelicItemInfoPopup>();
            popup.SetData(param);
        }
    }
}