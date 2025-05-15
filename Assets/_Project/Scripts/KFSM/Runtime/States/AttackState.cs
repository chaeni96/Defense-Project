using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kylin.FSM;
using Kylin.LWDI;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
    [SerializeField] private string skillAddressableKey; // �⺻ ��ų ��巹���� Ű
    [SerializeField] private string manaFullSkillAddressableKey; // ����Ǯ ��ų ��巹���� Ű

    private float targetCheckInterval = 0.2f; // Ÿ�� üũ ����
    private float lastTargetCheckTime = 0f;

    // ���� �ִϸ��̼� Ÿ�̹�
    private float attackAnimationLength = 1.0f; // �⺻��
    private float damageApplyTime = 0.5f; // �⺻��
    private float attackTimer = 0f; // ���� Ÿ�̸�
    private bool damageApplied = false; // ������ ���� ����

    // �ִϸ��̼� ���� üũ
    private bool animLengthChecked = false;
    private float animCheckDelay = 0.05f; // �ִϸ��̼� üũ ������
    private float animCheckTimer = 0f; // �ִϸ��̼� üũ Ÿ�̸�

    // �ִϸ��̼� Ÿ�̹� ���
    private const float DAMAGE_TIMING_RATIO = 0.4f;
    private const int LAYER_INDEX = 0;
    [Inject] protected StateController Controller;
    [Inject] protected CharacterFSMObject characterFSM;

    public override void OnEnter()
    {
        Debug.Log("���� ���� ����");
        lastTargetCheckTime = 0f;

        // ĳ���� FSM ���� Ȯ��
        if (characterFSM == null) return;

        // �ʿ�� Ÿ�� ã��
        if (characterFSM.CurrentTarget == null)
        {
            characterFSM.UpdateTarget();
            // Ÿ���� ������ �߰� ���·� ��ȯ
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }

        // ���� Ÿ�̸ӿ� �÷��� �ʱ�ȭ
        ResetAttack();

        // �ִϸ��̼� ���� üũ �ʱ�ȭ
        animLengthChecked = false;
        animCheckTimer = 0f;
    }

    public override void OnUpdate()
    {
        // ĳ���� FSM ���� Ȯ��
        if (characterFSM == null || characterFSM.basicObject == null)
        {
            Controller?.RegisterTrigger(Trigger.AttackFinished);
            return;
        }

        // ���� üũ���� �ʾҴٸ� �ִϸ��̼� ���� üũ
        if (!animLengthChecked)
        {
            animCheckTimer += Time.deltaTime;

            // ������ �� �ִϸ��̼� ���� ��������
            if (animCheckTimer >= animCheckDelay)
            {
                GetCurrentAnimationLength();
                animLengthChecked = true;

                // ���� Ÿ�̸� ���� (�ִϸ��̼� üũ ������ ����)
                attackTimer = 0f;
            }
            return;
        }

        // ���� Ÿ�̸� ������Ʈ
        attackTimer += Time.deltaTime;

        // ������ �ð��� ������ ����
        if (!damageApplied && attackTimer >= damageApplyTime)
        {
            // ���� Ȯ��
            int currentMana = (int)characterFSM.basicObject.GetStat(StatName.CurrentMana);
            float maxMana = characterFSM.basicObject.GetStat(StatName.MaxMana);

            // ������ ���� á�� ����Ǯ ��ų�� �ִ� ��� ����Ǯ ��ų ���
            if (currentMana >= maxMana && !string.IsNullOrEmpty(manaFullSkillAddressableKey))
            {
                Debug.Log("������ ���� á���ϴ�! ����Ǯ ��ų�� ����մϴ�.");
                FireSkill(manaFullSkillAddressableKey);
                // ���� �Ҹ�
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, -currentMana, 1f); // ���� ��� �Ҹ�
            }
            // �׷��� ������ �Ϲ� ����/��ų ���
            else
            {
                if (string.IsNullOrWhiteSpace(skillAddressableKey))
                {
                    // ���� ������ ����
                    ApplyDamage();
                }
                else
                {
                    // �⺻ ��ų ���
                    if (characterFSM.CurrentTarget != null)
                    {
                        FireSkill(skillAddressableKey);
                    }
                }

                // ���� �� �ҷ��� ���� ȹ��
                int manaGain = 10; // �ʿ信 ���� �� �� ����
                characterFSM.basicObject.ModifyStat(StatName.CurrentMana, manaGain, 1f);
            }

            damageApplied = true;
        }

        // ���� �ִϸ��̼� �Ϸ� ���� üũ
        if (attackTimer >= attackAnimationLength)
        {
            // Ÿ���� ������ ��ȿ�ϰ� ��Ÿ� ���� �ִ��� Ȯ��
            if (IsTargetValid() && IsTargetInRange())
            {
                // ���� ������ ���� �ʱ�ȭ
                ResetAttack();
                animLengthChecked = false;
                animCheckTimer = 0f;
            }
            else
            {
                // Ÿ���� �� �̻� ��ȿ���� �ʰų� ��Ÿ� �ۿ� ������ ���� ���� ����
                Controller.RegisterTrigger(Trigger.TargetMiss);
            }
        }

        // �ֱ������� Ÿ�� ��ȿ�� �� ��Ÿ� üũ
        if (Time.time - lastTargetCheckTime > targetCheckInterval)
        {
            lastTargetCheckTime = Time.time;

            if (!IsTargetValid())
            {
                return;
            }

            // Ÿ���� ��Ÿ� ���� �ִ��� üũ
            if (!IsTargetInRange())
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return;
            }
        }
    }

    // Ÿ�ٿ��� ��ų �߻� �޼ҵ�
    private void FireSkill(string skillKey)
    {
        if (characterFSM.CurrentTarget == null) return;

        // Ÿ�� ��ġ�� ���� ��������
        Vector3 currentTargetPosition = characterFSM.CurrentTarget.transform.position;
        Vector3 firingPosition = characterFSM.transform.position;
        Vector3 targetDirection = (currentTargetPosition - firingPosition).normalized;

        // Ǯ���� ��ų ������Ʈ ��������
        GameObject skillObj = PoolingManager.Instance.GetObject(skillKey, firingPosition, (int)ObjectLayer.IgnoereRayCast);

        // ����ü �ʱ�ȭ �� �߻�
        if (skillObj != null)
        {
            SkillBase projectile = skillObj.GetComponent<SkillBase>();
            if (projectile != null)
            {
                projectile.Initialize(characterFSM.basicObject);
                projectile.Fire(
                    characterFSM.basicObject,
                    currentTargetPosition,
                    targetDirection,
                    characterFSM.CurrentTarget
                );

                Debug.Log($"��ų �߻�: {skillKey}, Ÿ��: {characterFSM.CurrentTarget.name}");
            }
            else
            {
                Debug.LogError($"��ų ������Ʈ�� �����ϴ�: {skillKey}");
            }
        }
        else
        {
            Debug.LogError($"��ų ������Ʈ�� �������µ� �����߽��ϴ�: {skillKey}");
        }
    }

    // ���� �ִϸ��̼� ���� ��������
    private void GetCurrentAnimationLength()
    {
        if (characterFSM?.animator == null) return;

        // ���� �ִϸ��̼� Ŭ�� ���� ��������
        AnimatorClipInfo[] clipInfo = characterFSM.animator.GetCurrentAnimatorClipInfo(LAYER_INDEX);

        if (clipInfo.Length > 0)
        {
            // �ִϸ��̼� ���̿� ������ Ÿ�̹� ����
            attackAnimationLength = clipInfo[0].clip.length;
            damageApplyTime = attackAnimationLength * DAMAGE_TIMING_RATIO;
        }
    }

    // Ÿ�ٿ��� ���� ������ ����
    private void ApplyDamage()
    {
        if (characterFSM != null && characterFSM.CurrentTarget != null)
        {
            if (characterFSM.CurrentTarget.isEnemy)
            {
                var enemyObj = characterFSM.CurrentTarget.GetComponent<Enemy>();
                if (enemyObj != null)
                {
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    enemyObj.OnDamaged(characterFSM.basicObject, damage);
                    Debug.Log($"���� ������ ����: {characterFSM.CurrentTarget.name}, ������={damage}");
                }
            }
            else
            {
                var unitObj = characterFSM.CurrentTarget.GetComponent<UnitController>();
                if (unitObj != null)
                {
                    float damage = characterFSM.basicObject.GetStat(StatName.ATK);
                    unitObj.OnDamaged(characterFSM.basicObject, damage);
                    Debug.Log($"���� ������ ����: {characterFSM.CurrentTarget.name}, ������={damage}");
                }
            }
        }
    }

    // ���� ���� �ʱ�ȭ
    private void ResetAttack()
    {
        attackTimer = 0f;
        damageApplied = false;
    }

    // Ÿ���� ��Ÿ� ���� �ִ��� Ȯ��
    private bool IsTargetInRange()
    {
        if (characterFSM == null || characterFSM.basicObject == null)
            return false;

        float attackRange = characterFSM.basicObject.GetStat(StatName.AttackRange);
        return characterFSM.GetDistanceToTarget() <= attackRange;
    }

    // Ÿ�� ��ȿ�� üũ
    private bool IsTargetValid()
    {
        var target = characterFSM.CurrentTarget;

        // ���� Ÿ���� ������ �� Ÿ�� ã��
        if (target == null)
        {
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
            return true;
        }

        // Ÿ���� Ȱ��ȭ �������� üũ
        if (!target.isActive)
        {
            characterFSM.UpdateTarget();
            if (characterFSM.CurrentTarget == null)
            {
                Controller.RegisterTrigger(Trigger.TargetMiss);
                return false;
            }
        }

        return true;
    }

    public override void OnExit()
    {
        Debug.Log("���� ���� ����");
    }
}