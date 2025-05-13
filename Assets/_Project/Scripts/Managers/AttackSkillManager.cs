using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackSkillManager : MonoBehaviour
{
    private static AttackSkillManager _instance;

    private List<SkillBase> activeSkillObjects = new List<SkillBase>();
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
                //unit.MoveScale();
                //StartCoroutine(ActiveProjectileSkill(skillPoolingKey, unit, targetPos));
                break;

            case SkillAttackType.AOE:
                CreateSkillObject(skillPoolingKey, unit, targetPos);
                break;
        }


    }


    //private IEnumerator ActiveProjectileSkill(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    //{
    //    int projectileCount = (int)unit.GetStat(StatName.ProjectileCount);

    //    //Enemy target = EnemyManager.Instance.GetEnemyAtPosition(targetPos);

    //    //for (int i = 0; i < projectileCount; i++)
    //    //{
    //    //    Vector3 currentTargetPos = target.transform.position;
    //    //    CreateSkillObject(skillPoolingKey, unit, currentTargetPos);

    //    //    // 0.1�� �������� ����
    //    //    yield return new WaitForSeconds(0.1f);
    //    //}
    //}

    private void CreateSkillObject(string skillPoolingKey, UnitController unit, Vector3 targetPos)
    {
        GameObject skillObj = PoolingManager.Instance.GetObject(skillPoolingKey, unit.transform.position, (int)ObjectLayer.IgnoereRayCast);
        if (skillObj != null)
        {
            var skill = skillObj.GetComponent<SkillBase>();
            skill.Initialize(unit);
            //skill.Fire(targetPos);

            // Projectile�� �ƴ� ��ų ������Ʈ�� ��� -> ProjectTile�� ProjectileManager ���ؼ� ����
            if (unit.attackType != SkillAttackType.Projectile)
            {
                activeSkillObjects.Add(skill);

            }

        }
    }
    // ���� ���� �� ��� ��ų ������Ʈ�� ����
    public void CleanUp()
    {
        for (int i = activeSkillObjects.Count - 1; i >= 0; i--)
        {
            SkillBase skillObject = activeSkillObjects[i];
            skillObject.DestroySkill();
            PoolingManager.Instance.ReturnObject(skillObject.gameObject);
            activeSkillObjects.RemoveAt(i);
        }

        activeSkillObjects.Clear();
    }

}