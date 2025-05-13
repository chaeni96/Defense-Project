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
            // ���Կ� ������ ������ ����Ǿ��� ���� ������Ʈ
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
            
            // ���Կ� ������ ���� ���� ��������
            equippedRelicId = RelicDataController.Instance.GetEquippedRelicAt(slotIndex);
            
            if (equippedRelicId == BGId.Empty)
            {
                // �� ���� ǥ��
                relicIconImage.gameObject.SetActive(false);
                return;
            }
            
            // ������ ���� ǥ��
            relicIconImage.gameObject.SetActive(true);
            
            // ���� ���� ��������
            var relicItemData = D_RelicItemData.GetEntity(equippedRelicId);
            if (relicItemData == null)
            {
                Debug.LogError($"Cannot find relic item data for ID: {equippedRelicId}");
                return;
            }
            
            // ������ ����
            if (relicIconImage != null)
            {
                // ������ �ε� ���� (������Ʈ�� �°� ���� �ʿ�)
                relicIconImage.sprite = AtlasManager.Instance.GetItemIcon(relicItemData.f_iconKey);
            }
        }
        
        // ���� Ŭ�� �̺�Ʈ
        public void OnClickSlot()
        {
            // �ε������Ͱ� Ȱ��ȭ�� ���¶�� ���� ����̹Ƿ� ����
            if (indicaterObject.activeSelf)
            {
                return;
            }
            
            if (equippedRelicId == BGId.Empty)
            {
                // �� ���� Ŭ�� - ���� ������ ���� ���� UI ���� ���� ����
                return;
            }
            
            // ������ ���� ���� �˾� ����
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