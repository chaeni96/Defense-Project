using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UnitController : BasicObject, IPointerClickHandler
{
    //BgDatabase���� �о����
    [HideInInspector]
    public int attack;
    [HideInInspector]
    public int attackSpeed;
    [HideInInspector]
    public int attackRange;
    [HideInInspector]
    public int unitBlockSize;
    [HideInInspector]
    public float attackCoolDown;

    [HideInInspector]
    public float attackTimer = 0f;  // Ÿ�̸� �߰�

    [HideInInspector]
    public string tileUniqueID;

    public SkillAttackType attackType;
    public SpriteRenderer unitSprite;

    [SerializeField] private SpriteRenderer unitBaseSprite;

   
    private bool isActive = true;
    private D_TileShpeData tileData;

    public override void Initialize()
    {
        base.Initialize();

    }

    //���� tileshpae�� ���� ������ �������ϼ������Ƿ� ������ ���� ID �ο�
    public void RegistereTileID(string uniqueID, D_TileShpeData tile)
    {
        tileUniqueID = uniqueID;
        tileData = tile;
    }

    public void ShowPreviewUnit()
    {
        UnityEngine.Color color;

        if (unitSprite != null)
        {
            color = unitSprite.color;
            color.a = 0.5f; // 50% ����
            unitSprite.color = color;
        }
        
        if(unitBaseSprite != null)
        {
            color = unitBaseSprite.color;
            color.a = 0.5f;
            unitBaseSprite.color = color;

        }
        
    }

    //tileShpaeName�� ���� ���� ���ؼ� �������� �������°�
    public void InitializeUnitStat(D_unitBuildData buildData)
    {
        if (buildData == null) return;

        var unitData = buildData.f_unitData;
        attackType = unitData.f_SkillAttackType;
        if (unitData != null)
        {
            // statDatas�� ��ȸ�ϸ鼭 ���� ����
            foreach (var statData in unitData.f_statDatas)
            {
                switch (statData.f_stat.f_name)
                {
                    case "Attack":
                        this.attack = statData.f_value;
                        break;
                    case "AttackSpeed":
                        this.attackSpeed = statData.f_value;
                        break;
                    case "AttackRange":
                        this.attackRange = statData.f_value;
                        break;
                    case "UnitBlockSize":
                        this.unitBlockSize = statData.f_value;
                        break;
                    case "AttackCoolTime":
                        this.attackCoolDown = statData.f_value;
                        break;
                }
            }
        }
        else
        {
            Debug.LogError("UnitData ����");
        }

    }

    //���� ��������
    public Dictionary<string, int> GetStats()
    {
        return new Dictionary<string, int>
    {
        { "Attack", attack },
        { "AttackSpeed", attackSpeed },
        { "AttackRange", attackRange },
        { "UnitBlockSize", unitBlockSize }
    };
    }

    public bool IsActive() => isActive;

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // Ÿ�̸� ����
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //���� Ŭ�������� �˾�â ����
         FieldUnitInfoUI unitInfo = UIManager.Instance.ShowUI<FieldUnitInfoUI>("FieldUnitInfoUI");

        //�˾�â�� Ÿ�� ���� �Ѱ��ֱ�

        unitInfo.InitUnitInfo(this);


    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // ���� ���� �ð�ȭ
        Gizmos.color = new UnityEngine.Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
