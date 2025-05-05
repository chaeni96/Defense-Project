using System;
using UnityEngine;
using Kylin.FSM;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    private float targetCheckInterval = 0.2f; // Ÿ�� �� ���� üũ �ֱ�
    private float lastTargetCheckTime = 0f;
    private CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("AttackState ����");
        lastTargetCheckTime = 0f;

        // Owner�� CharacterFSMObject�� ĳ����
        characterFSM = Owner as CharacterFSMObject;

        if (characterFSM != null)
        {
            // Ÿ���� ���ٸ� ã��
            if (characterFSM.CurrentTarget == null)
            {
                characterFSM.UpdateTarget();

                // Ÿ���� ������ Chase ���·� (Chase���� �ٽ� Ÿ�� ã�� ��)
                if (characterFSM.CurrentTarget == null)
                {
                    Controller.RegisterTrigger(Trigger.TargetMiss);
                    return;
                }
            }
        }
    }

    public override void OnUpdate()
    {
        // CharacterFSMObject Ȯ��
        if (characterFSM == null) return;

        // �ֱ������� Ÿ�� ���¿� ���� Ȯ��
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            // Ÿ�� ��ȿ���� Ȯ��
            if (!IsTargetValid())
            {
                return; 
            }

            // ���� ���� üũ (�߰���)
            float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
            if (characterFSM.GetDistanceToTarget() > attackRange)
            {
                // Ÿ���� ���� ������ ������� �߰� ���·�
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    //Ÿ�� ��ȿ üũ �޼���
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // Ÿ���� ������ �� Ÿ�� ã��
        if (target == null)
        {
            characterFSM.UpdateTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ Idle ���·�
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }

            return true; // �� Ÿ���� ����
        }

        // Ÿ���� ��Ȱ��ȭ�Ǿ����� Ȯ�� (�׾��ų� Ǯ�� ��ȯ��)
        if (!target.gameObject.activeSelf)
        {
            // Ÿ���� �׾����� �� Ÿ�� ã��
            characterFSM.UpdateTarget();

            if (characterFSM.CurrentTarget == null)
            {
                // �� Ÿ�ٵ� ������ Idle ���·�
                Controller.RegisterTrigger(Trigger.AttackFinished);
                return false;
            }
            else
            {
                // �� Ÿ���� ������ Chase ���·�
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }      

        return true; 
    }

    public override void OnExit()
    {
        Debug.Log("AttackState ����");
    }
}