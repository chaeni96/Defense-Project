using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSkillManager : MonoBehaviour
{
    public static AttackSkillManager _instance;


    //test��
    public GameObject projectilePrefab; // ����ü ������
    public GameObject aoePrefab; // �������ݿ� ������

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

        // Unit�� Ÿ�Կ� ���� ���� �޼��� ȣ��
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
    /// ����ü ������ �����ϴ� �޼���
    /// </summary>
    private void ProjectileAttack(UnitController unit, BasicObject target)
    {
        // ����ü ����
        //�ʱ�ȭ
        GameObject projectile = Instantiate(projectilePrefab, unit.myBody.position, Quaternion.identity);
        projectile.GetComponent<TheProjectile>().Initialize(unit, target);

        Debug.Log($"Projectile attack executed by {unit.name} on {target.name}");
    }

    /// <summary>
    /// ���� ������ �����ϴ� �޼���
    /// </summary>
    private void AoeAttack(UnitController unit, BasicObject target)
    {
        // AOE ���� ���� ����
        GameObject aoeRange = Instantiate(aoePrefab, target.myBody.position, Quaternion.identity);

        // AOE ��ų �ʱ�ȭ
        TheAOE aoeSkill = aoeRange.GetComponent<TheAOE>();

        if (aoeSkill != null)
        {
            aoeSkill.Initialize(unit.myBody.position, unit.attackRange, unit);
        }

    }
}
