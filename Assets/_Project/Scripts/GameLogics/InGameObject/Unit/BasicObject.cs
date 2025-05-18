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


    //처음 Subject에서 가져온 기본 스탯, 레벨업이나 버프 적용시 참조하는 기준값
    public Dictionary<StatName, StatStorage> baseStats = new Dictionary<StatName, StatStorage>();

    //실제 게임에서 사용되는 현재 스탯 값, 버프나 디버프로 변화 적용하는 값
    public Dictionary<StatName, StatStorage> currentStats = new Dictionary<StatName, StatStorage>();

    //구독중인 
    public List<StatSubject> subjects = new List<StatSubject>();
    [HideInInspector]
    public float attackTimer = 0f;  // 타이머 추가
    public bool isEnemy = false;
    public bool isActive = false;

    [SerializeField] protected Slider hpBar;  // Inspector에서 할당
    [SerializeField] protected Canvas hpBarCanvas;  // Inspector에서 할당

    
    [SerializeField] protected UnitAppearanceProvider appearanceProvider;


    //TODO : 애니메이션 컨트롤하는 매핑 스크립트 따로 만들기
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

        // currentStats 업데이트
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

    //유닛의 현재 특정 스탯의 값 반환
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
        // 현재 스탯이 있으면 값 수정
        if (currentStats.TryGetValue(statName, out var currentStat))
        {
            currentStat.value += value;
            currentStat.multiply *= multiply;
        }
        // 없으면 새 스탯 추가
        else
        {
            currentStats[statName] = new StatStorage
            {
                statName = statName,
                value = value,
                multiply = multiply
            };
        }

        // 스탯 변경에 따른 효과 적용 (HP 바 업데이트 등)
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
        // 이미 죽었다면 데미지 처리 중단
        if (isDead) return;

        if (attacker != null)
        {
            //attacker의 공격력 
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


    // 애니메이터 컨트롤러 변경 메서드
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
