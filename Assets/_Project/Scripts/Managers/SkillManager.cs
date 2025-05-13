using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private static SkillManager _instance;

    // 현재 활성화된 모든 스킬 추적
    private HashSet<SkillBase> activeSkills = new HashSet<SkillBase>();
    
    
    public static SkillManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SkillManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("AttackSkillManager");
                    _instance = singleton.AddComponent<SkillManager>();
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

    // 새 스킬이 생성될 때 등록
    public void RegisterSkill(SkillBase skill)
    {
        if (skill != null)
        {
            activeSkills.Add(skill);
        }
    }

    // 스킬이 파괴될 때 등록 해제
    public void UnregisterSkill(SkillBase skill)
    {
        if (skill != null)
        {
            activeSkills.Remove(skill);
        }
    }

    // 모든 스킬 정리
    public void ClearAllSkills()
    {
        // 현재 활성화된 모든 스킬의 복사본 생성 (수정 충돌 방지)
        List<SkillBase> skillsToDestroy = new List<SkillBase>(activeSkills);

        // 각 스킬 제거
        foreach (SkillBase skill in skillsToDestroy)
        {
            if (skill != null)
            {
                skill.DestroySkill();
            }
        }

        activeSkills.Clear();
    }

}