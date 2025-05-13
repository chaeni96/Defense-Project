using System;
using System.Collections.Generic;
using AutoBattle.Scripts.Utils;
using BansheeGz.BGDatabase;

namespace BGDatabaseEnum.DataController
{
    public enum RelicStateEventType
    {
        None,
        Add,
        LevelUp,
        Equip,
        UnEquip
    }
    
    public struct RelicStateChangeEvent
    {
        public BGId TargetRelicId { get; }
        public RelicStateEventType EventType { get;}
        
        public RelicStateChangeEvent(BGId targetRelicId, RelicStateEventType eventType)
        {
            TargetRelicId = targetRelicId;
            EventType = eventType;
        }
    }
    
    public interface IRelicStateChangeSubscriber
    {
        void OnRelicStateChange(RelicStateChangeEvent relicStateChangeEvent);
    }
    
    public class RelicDataController : Singleton<RelicDataController>
    {
        private const int MAX_EQUIP_SLOT_COUNT = 4;
        
        private List<D_U_RelicData> relicDataList = new List<D_U_RelicData>();

        // 장착된 유물 ID를 저장할 리스트
        private List<BGId> equippedRelics = new List<BGId>(MAX_EQUIP_SLOT_COUNT);
        
        private List<IRelicStateChangeSubscriber> subscribers = new();

        public void Initialize()
        {
            relicDataList = D_U_RelicData.FindEntities(data => true);

            equippedRelics.Clear();
            // 장착된 유물 리스트 초기화
            for (int i = 0; i < MAX_EQUIP_SLOT_COUNT; ++i)
            {
                equippedRelics.Add(BGId.Empty);
            }

            foreach (var relic in relicDataList)
            {
                if (relic.f_isEquiped)
                {
                    equippedRelics[relic.f_equipedSlotIndex] = relic.f_relicData.Id;
                }
            }
        }
        
        public void AddSubscriber(IRelicStateChangeSubscriber subscriber)
        {
            if (subscribers.Contains(subscriber)) return;
            
            subscribers.Add(subscriber);
        }

        public void AddRelicItem(D_RelicItemData relicItemData)
        {
            // 이미 획득한 유물인지 체크
            var existingRelic = D_U_RelicData.FindEntity(data => data.f_relicData.Id == relicItemData.Id);
            if (existingRelic != null)
            {
                existingRelic.f_exp += 1;
                
                NotifyToSubscriber(new RelicStateChangeEvent(relicItemData.Id, RelicStateEventType.LevelUp));
                
                SaveLoadManager.Instance.SaveData();
                return;
            }
            
            var ne = D_U_RelicData.NewEntity();
            
            ne.f_level = 1;
            ne.f_exp = 0;
            ne.f_relicData = relicItemData;
            ne.f_isEquiped = false;
            
            relicDataList.Add(ne);
            
            NotifyToSubscriber(new RelicStateChangeEvent(relicItemData.Id, RelicStateEventType.Add));
            
            SaveLoadManager.Instance.SaveData();
        }

        // 유물 장착 메서드
        public bool EquipRelic(BGId relicId, int slotIndex)
        {
            // 슬롯 범위 체크
            if (slotIndex < 0 || slotIndex >= MAX_EQUIP_SLOT_COUNT)
            {
                return false;
            }

            // 유효한 유물인지 체크
            var relicData = D_U_RelicData.FindEntity(data => data.f_relicData.Id == relicId);
            if (relicData == null)
            {
                return false;
            }

            // 해당 슬롯에 이미 장착된 유물이 있다면 해제
            if (equippedRelics[slotIndex] != BGId.Empty)
            {
                var prevEquippedRelic =
                    D_U_RelicData.FindEntity(data => data.f_relicData.Id == equippedRelics[slotIndex]);
                if (prevEquippedRelic != null)
                {
                    prevEquippedRelic.f_isEquiped = false;
                    prevEquippedRelic.f_equipedSlotIndex = -1;
                    NotifyToSubscriber(
                        new RelicStateChangeEvent(equippedRelics[slotIndex], RelicStateEventType.UnEquip));
                }
            }

            // 새 유물 장착
            relicData.f_isEquiped = true;
            relicData.f_equipedSlotIndex = slotIndex;
            equippedRelics[slotIndex] = relicId;

            // 구독자들에게 알림
            NotifyToSubscriber(new RelicStateChangeEvent(relicId, RelicStateEventType.Equip));

            // 데이터 저장
            SaveLoadManager.Instance.SaveData();

            return true;
        }

        // 유물 장착 해제 메서드
        public bool UnequipRelic(BGId relicId)
        {
            // 유효한 유물인지 체크
            var relicData = D_U_RelicData.FindEntity(data => data.f_relicData.Id == relicId);
            if (relicData == null || !relicData.f_isEquiped)
            {
                return false;
            }

            // 장착 해제
            relicData.f_isEquiped = false;
            relicData.f_equipedSlotIndex = -1;

            // 장착 리스트에서 제거
            for (int i = 0; i < equippedRelics.Count; i++)
            {
                if (equippedRelics[i] == relicId)
                {
                    equippedRelics[i] = BGId.Empty;
                    break;
                }
            }

            // 구독자들에게 알림
            NotifyToSubscriber(new RelicStateChangeEvent(relicId, RelicStateEventType.UnEquip));

            // 데이터 저장
            SaveLoadManager.Instance.SaveData();

            return true;
        }

        // 특정 슬롯에 장착된 유물 ID 반환
        public BGId GetEquippedRelicAt(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= equippedRelics.Count)
            {
                return BGId.Empty;
            }

            foreach (var equippedRelic in equippedRelics)
            {
                var relicData = D_U_RelicData.FindEntity(data => data.f_relicData.Id == equippedRelic);
                if (relicData is { f_isEquiped: true } && relicData.f_equipedSlotIndex == slotIndex)
                {
                    return equippedRelic;
                }
            }

            return BGId.Empty;
        }

        // 유물이 장착되어 있는지 확인
        public bool IsRelicEquipped(BGId relicId)
        {
            return equippedRelics.Contains(relicId);
        }

        // 유물이 장착된 슬롯 인덱스 반환
        public int GetRelicEquippedSlotIndex(BGId relicId)
        {
            return equippedRelics.IndexOf(relicId);
        }

        public void ClearAll()
        {
            foreach (var relicData in relicDataList)
            {
                relicData.Delete();
            }
            
            SaveLoadManager.Instance.SaveData();
        }

        private void NotifyToSubscriber(RelicStateChangeEvent relicStateChangeEvent)
        {
            foreach (var subscriber in subscribers)
            {
                subscriber.OnRelicStateChange(relicStateChangeEvent);
            }
        }
    }
}