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

    //스킬 ID, 스킬 발동한 주체, 타겟 필요
    public void ActiveSkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {

        switch (unit.attackType)
        {
            case SkillAttackType.Projectile:
                StartCoroutine(ActiveProjectileSkill(skillPoolingKey, unit, targetPos));
                break;

            case SkillAttackType.AOE:
                CreateSkillObject(skillPoolingKey, unit, targetPos);
                break;
        }


    }


    private IEnumerator ActiveProjectileSkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        int projectileCount = (int)unit.GetStat(StatName.ProjectileCount);

        for (int i = 0; i < projectileCount; i++)
        {
            CreateSkillObject(skillPoolingKey, unit, targetPos);

            // 0.1초 간격으로 생성
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void CreateSkillObject(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        GameObject skillObj = PoolingManager.Instance.GetObject(skillPoolingKey, unit.transform.position);
        if (skillObj != null)
        {
            var skill = skillObj.GetComponent<SkillBase>();
            skill.Initialize(unit);
            skill.Fire(targetPos);
        }
    }

}