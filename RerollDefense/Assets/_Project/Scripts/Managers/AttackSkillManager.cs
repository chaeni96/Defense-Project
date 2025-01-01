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
    public void ActiveSkill(string skillID, UnitController unit, Vector3 targetPos)
    {
        GameObject skillObj = PoolingManager.Instance.GetObject(skillID, unit.transform.position);

        if (skillObj != null)
        {
            var skill = skillObj.GetComponent<SkillBase>();
            skill.Initialize(unit);
            skill.Fire(targetPos);
        }


    }


}