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

    //스킬 ID, 스킬 발동한 주체, 타겟 필요
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

    //    //    // 0.1초 간격으로 생성
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

            // Projectile이 아닌 스킬 오브젝트만 등록 -> ProjectTile은 ProjectileManager 통해서 관리
            if (unit.attackType != SkillAttackType.Projectile)
            {
                activeSkillObjects.Add(skill);

            }

        }
    }
    // 게임 종료 시 모든 스킬 오브젝트들 정리
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