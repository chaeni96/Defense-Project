using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class DefaultDamageState : StateBase
{
    [SerializeField] private string defaultSkillAddressableKey; // ��ų ��巹���� Ű, ��Ÿ�� ��ų ���°� (null�̸� ���� ������)
    [SerializeField] private string manaFullSkillAddressableKey; // ���� Ǯ�� ä������ ���� ��ų ��巹���� Ű


    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("Attack_DamageState ����");

        // CharacterFSMObject Ȯ��
        if (characterFSM == null)
        {
            Controller?.RegisterTrigger(Trigger.DamageFinished);
            return;
        }

        // Ÿ�� Ȯ��
        if (characterFSM.CurrentTarget == null || !IsTargetValid())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        //TODO: �б⹮ �ʿ� ���� �� ���� ��ų ����ϴ°Ͱ� �ƴѰ�����

        // skillAddressableKey�� null�̸� ���� ������, �ƴϸ� ��ų ���
        if (string.IsNullOrEmpty(defaultSkillAddressableKey))
        {
            ApplyDamage();
        }
        else
        {
            ActiveDefaultSkill();
        }

        // ��� ���� �Ϸ� Ʈ���� �߻�
        Controller.RegisterTrigger(Trigger.DamageFinished);
    }

    // ���� ������ ���� �޼���
    private void ApplyDamage()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // ������ ��� (ĳ������ ���ݷ� ��������)
        float damage = characterFSM.basicObject.GetStat(StatName.ATK);

        // Ÿ�ٿ��� ������ ����
        characterFSM.CurrentTarget.OnDamaged(characterFSM.basicObject, damage);

        Debug.Log($"���� ������ ����: ���={characterFSM.CurrentTarget.name}, ������={damage}");
    }

    // ��ų �ߵ� �޼���
    private void ActiveDefaultSkill()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // ���� Ÿ�� ��ġ�� ���� ���
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // Ǯ�� �Ŵ������� ��ų ������Ʈ ��������
        GameObject skillObj = PoolingManager.Instance.GetObject(defaultSkillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // ��ų �ʱ�ȭ �� �߻�
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"��ų �߻�: {defaultSkillAddressableKey}, Ÿ��: {characterFSM.CurrentTarget.name}");

                skill.Initialize(characterFSM.basicObject);
                skill.Fire(
                    characterFSM.basicObject,
                    currentTargetPosition,  // Ÿ�� ��ġ
                    targetDirection,        // Ÿ�� ����
                    characterFSM.CurrentTarget  // Ÿ�� ������Ʈ
                );
            }
            else
            {
                Debug.LogError($"��ų ������Ʈ�� �����ϴ�: {defaultSkillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"��ų ������Ʈ�� �������µ� �����߽��ϴ�: {defaultSkillAddressableKey}");
        }
    }

    // Ÿ�� ��ȿ�� üũ �޼���
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        if (target == null)
            return false;

        // Ȱ��ȭ �����̰� ü���� �ִ��� Ȯ��
        return target.isActive && target.GetStat(StatName.CurrentHp) > 0;
    }

    public override void OnExit()
    {
        Debug.Log("Attack_DamageState ����");
    }
}
