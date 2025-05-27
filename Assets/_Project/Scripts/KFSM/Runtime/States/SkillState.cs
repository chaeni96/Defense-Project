using UnityEngine;
using Kylin.LWDI;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Skill")]
    public class SkillState : StateBase
    {
        [Inject] private StateController controller;
        [Inject] private CharacterFSMObject characterFSM;
        
        public override void OnEnter()
        {
            // 마나 체크
            // float currentMana = characterFSM.basicObject.GetStat(StatName.CurrentMana);
            // float requiredMana = characterFSM.basicObject.GetStat(StatName.MaxMana);
            //
            // if (currentMana < requiredMana)
            // {
            //     // 마나 부족 시 평타 공격
            //     if (characterFSM.CurrentTarget != null)
            //     {
            //         
            //         float damage = characterFSM.basicObject.GetStat(StatName.ATK);
            //         characterFSM.CurrentTarget.OnDamaged(characterFSM.basicObject,damage);
            //     }
            //
            //     controller.RegisterTrigger(Trigger.SkillFinished);
            //     
            //     return;
            // }
            
            // 스킬 실행
            //ExecuteSkill();
            
            // 마나 소모
            
            if (characterFSM.CurrentTarget != null)
            {
                    
                float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                characterFSM.CurrentTarget.OnDamaged(characterFSM.basicObject,damage);
            }
            // 스킬 후 공격으로 
            controller.RegisterTrigger(Trigger.SkillFinished);
        }
        
        private void ExecuteSkill()
        {
            // 스킬 타입 가져오기 시리얼라이즈 필드로 받기 
            string skillPrefabName = "";
            
            // 풀에서 스킬 오브젝트 가져오기
            var skillObject = PoolingManager.Instance.GetObject(skillPrefabName);
            if (skillObject != null)
            {
                var skill = skillObject.GetComponent<SkillBase>();
                if (skill != null)
                {
                    // 스킬 초기화 및 실행
                    skill.Initialize(characterFSM.basicObject);
                    //skill.Fire();
                }
            }
        }
    }
}