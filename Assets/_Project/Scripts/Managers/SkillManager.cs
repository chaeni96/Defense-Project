using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    private static SkillManager _instance;

    // ���� Ȱ��ȭ�� ��� ��ų ����
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

    // �� ��ų�� ������ �� ���
    public void RegisterSkill(SkillBase skill)
    {
        if (skill != null)
        {
            activeSkills.Add(skill);
        }
    }

    // ��ų�� �ı��� �� ��� ����
    public void UnregisterSkill(SkillBase skill)
    {
        if (skill != null)
        {
            activeSkills.Remove(skill);
        }
    }

    // ��� ��ų ����
    public void ClearAllSkills()
    {
        // ���� Ȱ��ȭ�� ��� ��ų�� ���纻 ���� (���� �浹 ����)
        List<SkillBase> skillsToDestroy = new List<SkillBase>(activeSkills);

        // �� ��ų ����
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