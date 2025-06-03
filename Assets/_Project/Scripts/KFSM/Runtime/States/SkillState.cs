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
        
        // 타이밍 상수
        private const float ATTACK_ANIMATION_DURATION = 1.0f;
        private const float SKILL_TIMING = 0.667f; // 40프레임 = 0.667초

        
        public override void OnEnter()
        {
            if (!characterFSM.CurrentTarget.isActive)
            {
                controller.RegisterTrigger(Trigger.SkillFinished);
                return;
            }
            
            // 공격 애니메이션의 남은 시간 계산 (1초 - 0.4초 = 0.6초)
            remainingAttackTime = ATTACK_ANIMATION_DURATION - SKILL_TIMING;
            waitingForAttackFinish = false;
            
            // 마나 체크
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
                // 마나 부족 시 평타 공격
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
            
            // 마나 소모, 스킬 사용
            if (characterFSM.CurrentTarget.isActive && characterFSM.basicObject.manaFullSkillData != null)
            {
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(currentMana), 1f);
                ExecuteSkill(characterFSM.basicObject.manaFullSkillData.f_addressableKey);   
            }
            
            waitingForAttackFinish = true;
        }
        
        public override void OnUpdate()
        {
            // 공격 애니메이션이 끝날 때까지 대기
            if (waitingForAttackFinish)
            {
                remainingAttackTime -= Time.deltaTime;
                
                if (remainingAttackTime <= 0f)
                {
                    // 공격 애니메이션이 끝났으니 AttackState로 돌아감
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
                    Debug.Log($"스킬 발사: {skillAddressableKey}, 타겟: {characterFSM.CurrentTarget.name}");

                    skill.Initialize(characterFSM.basicObject);
                    skill.Fire(
                        characterFSM.CurrentTarget
                    );
                }
                else
                {
                    Debug.LogError($"스킬 컴포넌트가 없습니다: {skillAddressableKey}");
                }
            }
            else
            {
                Debug.LogError($"스킬 오브젝트를 가져오는데 실패했습니다: {skillAddressableKey}");
            }
        }
    }
}