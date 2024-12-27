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

    public void ActiveSkill(UnitController unit, Enemy target)
    {
        SpawnProjectile(unit, target);
    }

    public void ActiveSkill(UnitController unit, List<Enemy> targets)
    {
        SpawnAOE(unit, targets);
    }

    private void SpawnProjectile(UnitController unit, Enemy target)
    {
        GameObject projectileObj = PoolingManager.Instance.GetObject("ProjectileObject", unit.transform.position);
        if (projectileObj != null)
        {
            TheProjectile projectile = projectileObj.GetComponent<TheProjectile>();
            projectile.Initialize(unit, target);
        }
    }

    private void SpawnAOE(UnitController unit, List<Enemy> targets)
    {
        GameObject aoeObj = PoolingManager.Instance.GetObject("AoeRangeObject", unit.transform.position);
        if (aoeObj != null)
        {
            TheAOE aoe = aoeObj.GetComponent<TheAOE>();
            aoe.Initialize(unit, targets);
        }
    }
}