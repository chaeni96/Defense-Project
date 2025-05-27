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
            // ���� üũ
            // float currentMana = characterFSM.basicObject.GetStat(StatName.CurrentMana);
            // float requiredMana = characterFSM.basicObject.GetStat(StatName.MaxMana);
            //
            // if (currentMana < requiredMana)
            // {
            //     // ���� ���� �� ��Ÿ ����
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
            
            // ��ų ����
            //ExecuteSkill();
            
            // ���� �Ҹ�
            
            if (characterFSM.CurrentTarget != null)
            {
                    
                float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                characterFSM.CurrentTarget.OnDamaged(characterFSM.basicObject,damage);
            }
            // ��ų �� �������� 
            controller.RegisterTrigger(Trigger.SkillFinished);
        }
        
        private void ExecuteSkill()
        {
            // ��ų Ÿ�� �������� �ø�������� �ʵ�� �ޱ� 
            string skillPrefabName = "";
            
            // Ǯ���� ��ų ������Ʈ ��������
            var skillObject = PoolingManager.Instance.GetObject(skillPrefabName);
            if (skillObject != null)
            {
                var skill = skillObject.GetComponent<SkillBase>();
                if (skill != null)
                {
                    // ��ų �ʱ�ȭ �� ����
                    skill.Initialize(characterFSM.basicObject);
                    //skill.Fire();
                }
            }
        }
    }
}