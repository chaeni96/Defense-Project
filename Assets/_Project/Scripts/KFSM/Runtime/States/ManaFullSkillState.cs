using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class ManaFullSkillState : StateBase
{
    [SerializeField] private string manaFullSkillAddressableKey; // ��ų ��巹���� Ű, ��Ÿ�� ��ų ���°� (null�̸� ���� ������)


    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("���� Ǯ ��ų ����");

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

        // ���� Ȯ��
        float currentMana = characterFSM.basicObject.GetStat(StatName.CurrentMana);
        float maxMana = characterFSM.basicObject.GetStat(StatName.MaxMana);
        Debug.Log($"{currentMana}");

        // ������ ������� Ȯ��
        if (currentMana < maxMana)
        {
            Debug.Log($"{currentMana}");
            // ������ �����ϸ� �Ϲ� ���� ���·� ��ȯ
            Debug.Log("������ �����Ͽ� �Ϲ� �������� ��ȯ�մϴ�.");
            Controller.RegisterTrigger(Trigger.TargetSelected);
            return;
        }

        characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -Mathf.RoundToInt(currentMana), 1f);

        // ������ ����ϸ� �ñر� ���
        ActiveManaFullSkill();

        // ��� ���� �Ϸ� Ʈ���� �߻�
        Controller.RegisterTrigger(Trigger.DamageFinished);
    }

    // ��ų �ߵ� �޼���
    private void ActiveManaFullSkill()
    {
        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // ���� Ÿ�� ��ġ�� ���� ���
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // Ǯ�� �Ŵ������� ��ų ������Ʈ ��������
        GameObject skillObj = PoolingManager.Instance.GetObject(manaFullSkillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // ��ų �ʱ�ȭ �� �߻�
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"��ų �߻�: {manaFullSkillAddressableKey}, Ÿ��: {characterFSM.CurrentTarget.name}");

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
                Debug.LogError($"��ų ������Ʈ�� �����ϴ�: {manaFullSkillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"��ų ������Ʈ�� �������µ� �����߽��ϴ�: {manaFullSkillAddressableKey}");
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
