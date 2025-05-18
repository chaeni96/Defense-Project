using BansheeGz.BGDatabase;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;


public class BasicObject : MonoBehaviour, IStatSubscriber
{
    public Animator animator;

    public Kylin.FSM.FSMObjectBase fsmObj;


    //ó�� Subject���� ������ �⺻ ����, �������̳� ���� ����� �����ϴ� ���ذ�
    public Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //���� ���ӿ��� ���Ǵ� ���� ���� ��, ������ ������� ��ȭ �����ϴ� ��
    public Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //�������� 
    public List<StatSubject> subjects = new List<StatSubject>();
    [HideInInspector]
    public float attackTimer = 0f;  // Ÿ�̸� �߰�
    public bool isEnemy = false;
    public bool isActive = false;

    [SerializeField] protected Slider hpBar;  // Inspector���� �Ҵ�
    [SerializeField] protected Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    
    [SerializeField] protected UnitAppearanceProvider appearanceProvider;


    //TODO : �ִϸ��̼� ��Ʈ���ϴ� ���� ��ũ��Ʈ ���� �����
    [SerializeField] protected RuntimeAnimatorController attackBowController;
    [SerializeField] protected RuntimeAnimatorController attackSpearController;
    [SerializeField] protected RuntimeAnimatorController attackSwordController;
    [SerializeField] protected RuntimeAnimatorController attackThrowController;
    [SerializeField] protected RuntimeAnimatorController attackPunchController;

    protected bool isDead = false;
    public virtual void Initialize()
    {
        foreach (var subject in subjects)
        {
            StatManager.Instance.Subscribe(this, subject);
        }

        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;

        UpdateHpBar();
    }

    protected void UpdateHpBar()
    {
        float currentHp = GetStat(StatName.CurrentHp);
        float maxHp = GetStat(StatName.MaxHP);

        if (hpBar != null && maxHp > 0)
        {
            hpBar.value = currentHp / maxHp;
        }
    }

    public void AddSubject(StatSubject subject)
    {
        if (!subjects.Contains(subject))
        {
            subjects.Add(subject);
            StatManager.Instance.Subscribe(this, subject);
        }
    }

    public virtual void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (!subjects.Contains(subject)) return;

        // currentStats ������Ʈ
        if (!currentStats.ContainsKey(statChange.statName))
        {
            currentStats[statChange.statName] = new StatStorage
            {
                statName = statChange.statName,
                value = baseStats.ContainsKey(statChange.statName) ? baseStats[statChange.statName].value : 0,
                multiply = baseStats.ContainsKey(statChange.statName) ? baseStats[statChange.statName].multiply : 1f
            };
        }

        currentStats[statChange.statName].value += statChange.value;
        currentStats[statChange.statName].multiply *= statChange.multiply;
    }

    //������ ���� Ư�� ������ �� ��ȯ
    public float GetStat(StatName statName)
    {
        if (currentStats.TryGetValue(statName, out var stat))
        {
            return stat.value * stat.multiply;
        }
        return 0f;
    }


    public void ModifyStat(StatName statName, int value, float multiply)
    {
        // ���� ������ ������ �� ����
        if (currentStats.TryGetValue(statName, out var currentStat))
        {
            currentStat.value += value;
            currentStat.multiply *= multiply;
        }
        // ������ �� ���� �߰�
        else
        {
            currentStats[statName] = new StatStorage
            {
                statName = statName,
                value = value,
                multiply = multiply
            };
        }

        // ���� ���濡 ���� ȿ�� ���� (HP �� ������Ʈ ��)
        if (statName == StatName.CurrentHp || statName == StatName.MaxHP)
        {
            UpdateHpBar();
        }
        else if (statName == StatName.AttackSpeed)
        {
            attackTimer = 0;
        }


    }

    public void OnDamaged(BasicObject attacker, float damage = 0)
    {
        // �̹� �׾��ٸ� ������ ó�� �ߴ�
        if (isDead) return;

        if (attacker != null)
        {
            //attacker�� ���ݷ� 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            isDead = true;
            OnDead();
        }
    }


    // �ִϸ����� ��Ʈ�ѷ� ���� �޼���
    public virtual void SetAnimatorController(AnimControllerType unitType)
    {
        if (animator == null) return;

        switch (unitType)
        {
            case AnimControllerType.AttackBow:
                animator.runtimeAnimatorController = attackBowController;
                break;
            case AnimControllerType.AttackSpear:
                animator.runtimeAnimatorController = attackSpearController;
                break;
            case AnimControllerType.AttackSword:
                animator.runtimeAnimatorController = attackSwordController;
                break;
            case AnimControllerType.AttackPunch:
                animator.runtimeAnimatorController = attackPunchController;
                break;
            case AnimControllerType.AttackThrow:
                animator.runtimeAnimatorController = attackThrowController;
                break;
        }
    }


    public virtual void OnDead()
    {

    }

    public virtual BasicObject GetNearestTarget()
    {
        return null;
    }

    public virtual List<BasicObject> GetTargetList()
    {
        return null;
    }
    protected virtual void CleanUp()
    {

        foreach (var subject in subjects)
        {
            StatManager.Instance.Unsubscribe(this, subject);
        }

        currentStats.Clear();
        baseStats.Clear();
        subjects.Clear();
    }



}
