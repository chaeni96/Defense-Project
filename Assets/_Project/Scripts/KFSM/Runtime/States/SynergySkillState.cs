using System.Collections;
using System.Collections.Generic;
using Kylin.FSM;
using Kylin.LWDI;
using UnityEngine;

[FSMContextFolder("Create/State/Attack")]
public class SynergySkillState : StateBase
{
    [SerializeField] private string skillAddressableKey; // ��ų ��巹���� Ű

    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("ActiveState ����");

        // CharacterFSMObject Ȯ��
        if (characterFSM == null)
        {
            //Controller?.RegisterTrigger(Trigger.SynergySkillFinished);
            return;
        }

        // Ÿ�� Ȯ��
        if (characterFSM.CurrentTarget == null || !IsTargetValid())
        {
            Controller.RegisterTrigger(Trigger.TargetMiss);
            return;
        }

        // ��ų ���
        CastSkill();

        // ��� ��ų �Ϸ� Ʈ���� �߻�
        //Controller.RegisterTrigger(Trigger.SynergySkillFinished);
    }

    // ��ų �ߵ� �޼���
    private void CastSkill()
    {
        if (string.IsNullOrEmpty(skillAddressableKey))
        {
            Debug.LogError("��ų ��巹���� Ű�� �������� �ʾҽ��ϴ�.");
            return;
        }

        if (characterFSM == null || characterFSM.CurrentTarget == null) return;

        // ���� Ÿ�� ��ġ�� ���� ���
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // Ǯ�� �Ŵ������� ��ų ������Ʈ ��������
        GameObject skillObj = PoolingManager.Instance.GetObject(skillAddressableKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // ��ų �ʱ�ȭ �� �߻�
        if (skillObj != null)
        {
            SkillBase skill = skillObj.GetComponent<SkillBase>();
            if (skill != null)
            {
                Debug.Log($"��ų �߻�: {skillAddressableKey}, Ÿ��: {characterFSM.CurrentTarget.name}");

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
                Debug.LogError($"��ų ������Ʈ�� �����ϴ�: {skillAddressableKey}");
            }
        }
        else
        {
            Debug.LogError($"��ų ������Ʈ�� �������µ� �����߽��ϴ�: {skillAddressableKey}");
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
        Debug.Log("SkillCastState ����");
    }
}
