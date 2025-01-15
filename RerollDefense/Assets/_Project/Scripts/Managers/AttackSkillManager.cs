using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSkillManager : MonoBehaviour
{
    private static AttackSkillManager _instance;
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
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    //��ų ID, ��ų �ߵ��� ��ü, Ÿ�� �ʿ�
    public void ActiveSkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {

        switch (unit.attackType)
        {
            case SkillAttackType.Projectile:
                ActiveProjectileSkill(skillPoolingKey, unit, targetPos);
                break;

            case SkillAttackType.AOE:
                ActiveAOESkill(skillPoolingKey, unit, targetPos);
                break;
        }


    }


    private void ActiveProjectileSkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        int projectileCount = Mathf.Max(1, (int)unit.GetStat(StatName.ProjectileCount));

        if (projectileCount > 1)
        {
            StartCoroutine(FireMultipleProjectiles(skillPoolingKey, unit, targetPos, projectileCount));
        }
        else
        {
            FireSingleProjectile(skillPoolingKey, unit, targetPos);
        }
    }

    private void ActiveAOESkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        GameObject skillObj = PoolingManager.Instance.GetObject(skillPoolingKey, unit.transform.position);
        if (skillObj != null)
        {
            var skill = skillObj.GetComponent<SkillBase>();
            skill.Initialize(unit);
            skill.Fire(targetPos);
        }
    }

    //TODO : projectile ��ũ��Ʈ�� �ű��
    private void FireSingleProjectile(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        GameObject skillObj = PoolingManager.Instance.GetObject(skillPoolingKey, unit.transform.position);
        if (skillObj != null)
        {
            var skill = skillObj.GetComponent<SkillBase>();
            skill.Initialize(unit);
            skill.Fire(targetPos);
        }
    }

    private IEnumerator FireMultipleProjectiles(string skillPoolingKey, UnitController unit, Vector3 targetPos, int count)
    {

        //���͹��� �������� ���� �����
        float interval = unit.GetStat(StatName.ProjectileInterval);
        if (interval <= 0)
        {
            interval = 2; // �⺻�� 0.2�ʷ� ����
        }

        interval *= 0.1f; // 0.1�� ������ ���
        interval = Mathf.Max(0.1f, interval); // �ּ� 0.1�� ����

        for (int i = 0; i < count; i++)
        {
            FireSingleProjectile(skillPoolingKey, unit, targetPos);

            if (i < count - 1)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }

}