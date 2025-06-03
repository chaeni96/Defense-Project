using UnityEngine;
using Kylin.LWDI;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Skill")]
    public class SkillState : StateBase
    {
        [Inject] private StateController controller;
        [Inject] private CharacterFSMObject characterFSM;
        
        private float remainingAttackTime = 0f;
        private bool waitingForAttackFinish = false;
        
        // Ÿ�̹� ���
        private const float ATTACK_ANIMATION_DURATION = 1.0f;
        private const float SKILL_TIMING = 0.667f; // 40������ = 0.667��

        
        public override void OnEnter()
        {
            if (!characterFSM.CurrentTarget.isActive)
            {
                controller.RegisterTrigger(Trigger.SkillFinished);
                return;
            }
            
            // ���� �ִϸ��̼��� ���� �ð� ��� (1�� - 0.4�� = 0.6��)
            remainingAttackTime = ATTACK_ANIMATION_DURATION - SKILL_TIMING;
            waitingForAttackFinish = false;
            
            // ���� üũ
            float currentMana = characterFSM.basicObject.GetStat(StatName.CurrentMana);
            float maxMana = characterFSM.basicObject.GetStat(StatName.MaxMana);

            if (characterFSM.basicObject.isEnemy)
            {
                float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                characterFSM.CurrentTarget.OnDamaged(damage);
                waitingForAttackFinish = true;
                return;
            }
            
            if (currentMana < maxMana)
            {
                // ���� ���� �� ��Ÿ ����
                if (characterFSM.CurrentTarget.isActive)
                {
                    if (characterFSM.basicObject.basicSkillData != null)
                    {
                        ExecuteSkill(characterFSM.basicObject.basicSkillData.f_addressableKey);   
                    }
                    else
                    {
                        float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                        characterFSM.CurrentTarget.OnDamaged(damage);
                    }
                }
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, 10, 1f);
                waitingForAttackFinish = true;
                return;
            }
            
            // ���� �Ҹ�, ��ų ���
            if (characterFSM.CurrentTarget.isActive && characterFSM.basicObject.manaFullSkillData != null)
            {
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(currentMana), 1f);
                ExecuteSkill(characterFSM.basicObject.manaFullSkillData.f_addressableKey);   
            }
            
            waitingForAttackFinish = true;
        }
        
        public override void OnUpdate()
        {
            // ���� �ִϸ��̼��� ���� ������ ���
            if (waitingForAttackFinish)
            {
                remainingAttackTime -= Time.deltaTime;
                
                if (remainingAttackTime <= 0f)
                {
                    // ���� �ִϸ��̼��� �������� AttackState�� ���ư�
                    controller.RegisterTrigger(Trigger.SkillFinished);
                }
            }
        }
        
        private void ExecuteSkill(string skillAddressableKey)
        {
            Vector3 firingPosition = characterFSM.transform.position;
            GameObject skillObj = PoolingManager.Instance.GetObject(skillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

            if (skillObj != null)
            {
                SkillBase skill = skillObj.GetComponent<SkillBase>();
                if (skill != null)
                {
                    Debug.Log($"��ų �߻�: {skillAddressableKey}, Ÿ��: {characterFSM.CurrentTarget.name}");

                    skill.Initialize(characterFSM.basicObject);
                    skill.Fire(
                        characterFSM.CurrentTarget
                    );
                }
                else
                {
                    Debug.LogError($"��ų ������Ʈ�� �����ϴ�: {skillAddressableKey}");
                }
            }
            else
            {
                Debug.LogError($"��ų ������Ʈ�� �������µ� �����߽��ϴ�: {skillAddressableKey}");
            }
        }
    }
}