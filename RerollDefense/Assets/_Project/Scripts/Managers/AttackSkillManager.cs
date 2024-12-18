using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSkillManager : MonoBehaviour
{
    public static AttackSkillManager _instance;


    //test용
    public GameObject projectilePrefab; // 투사체 프리팹
    public GameObject aoePrefab; // 범위공격용 프리팹

    public static AttackSkillManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AttackSkillManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("AttackSkillManager");
                    _instance = singleton.AddComponent<AttackSkillManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }

    public void ActiveSkill(UnitController unit, BasicObject target)
    {
        if (unit == null || target == null)
        {
            Debug.LogError("Unit or Target is null!");
            return;
        }

        // Unit의 타입에 따라 공격 메서드 호출
        switch (unit.attackType)
        {
            case SkillAttackType.Projectile:
                ProjectileAttack(unit, target);
                break;

            case SkillAttackType.AOE:
                AoeAttack(unit, target);
                break;

            default:
                Debug.LogError("Unknown unit type for attack.");
                break;
        }
    }

    /// <summary>
    /// 투사체 공격을 수행하는 메서드
    /// </summary>
    private void ProjectileAttack(UnitController unit, BasicObject target)
    {
        // 투사체 생성
        //초기화
        GameObject projectile = Instantiate(projectilePrefab, unit.myBody.position, Quaternion.identity);
        projectile.GetComponent<TheProjectile>().Initialize(unit, target);

        Debug.Log($"Projectile attack executed by {unit.name} on {target.name}");
    }

    /// <summary>
    /// 범위 공격을 수행하는 메서드
    /// </summary>
    private void AoeAttack(UnitController unit, BasicObject target)
    {
        // AOE 공격 범위 생성
        GameObject aoeRange = Instantiate(aoePrefab, target.myBody.position, Quaternion.identity);

        // AOE 스킬 초기화
        TheAOE aoeSkill = aoeRange.GetComponent<TheAOE>();

        if (aoeSkill != null)
        {
            aoeSkill.Initialize(unit.myBody.position, unit.attackRange, unit);
        }

    }
}
