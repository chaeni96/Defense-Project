using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

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
    public Vector2 tilePosition;

    [HideInInspector]
    public SkillAttackType attackType;


    public UpgradeUnitType upgradeUnitType;

    public GameObject unitStarObject;

    public SpriteRenderer unitSprite;


    [SerializeField] private SpriteRenderer unitBaseSprite;
    

    //inspector�� �Ҵ�
    [SerializeField] private Material enabledMaterial;   // ��ġ ������ �� ���
    [SerializeField] private Material disabledMaterial; // ��ġ �Ұ����� �� ���
    [SerializeField] private Material originalMaterial; //�⺻ ���׸���

    private int unitSortingOrder;
    private int baseSortingOrder;
    private bool isActive = true;


    public D_UnitData unitData;

    public override void Initialize()
    {
        base.Initialize();

        unitSortingOrder = unitSprite.sortingOrder;
        baseSortingOrder = unitBaseSprite.sortingOrder;

    }

    public void InitializeTilePos(Vector2 tilePos)
    {
        tilePosition = new Vector2(tilePos.x, tilePos.y);
    }

    public Vector2 GetTilePosition()
    {
        return tilePosition;
    }
    public void InitializeUnitData(D_UnitData unit)
    {
        if (unit == null) return;

        unitData = unit;

        attackType = unitData.f_SkillAttackType;
        upgradeUnitType = unitData.f_UpgradeUnitType;

        if (unitData != null || attackType != SkillAttackType.None)
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

    public void MoveScale()
    {
        // DOPunchScale �� �Ķ����
        // punch:ũ�� ��ȭ
        // duration: ��ü ��� �ð�
        // vibrato: ���� Ƚ��
        // elasticity: ź�� (0~1)

        unitSprite.transform.DOPunchScale(punch: new Vector3(0.4f, 0.4f, 0f), duration: 0.1f, vibrato: 4, elasticity: 0.8f);


       

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

    public void UnitMerge(UnitController otherUnit)
    {
        // ���� Ÿ���� ���ָ� �ռ� ����
        if (this.upgradeUnitType != otherUnit.upgradeUnitType)
        {
            Debug.LogWarning("Different unit types cannot be merged.");
            return;
        }

        // ���� �ռ� ����
        // ���� ���, ���� Ÿ���� ���ֵ��� ������ ��ճ��ų� ������ �� ����
        foreach (var statData in otherUnit.unitData.f_statDatas)
        {
            var currentStat = this.unitData.f_statDatas.Find(s => s.f_stat.f_name == statData.f_stat.f_name);

            if (currentStat != null)
            {
                // ����: ������ ����
                currentStat.f_value += statData.f_value;
            }
            else
            {
                // ���ο� ���� �߰�
                this.unitData.f_statDatas.Add(statData);
            }
        }

        // ���� �ٽ� �ʱ�ȭ
        InitializeUnitData(this.unitData);

        // �ð��� ȿ�� (��: ��ġ ������)
        MoveScale();
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

        if(isActive)
        {
            //���� Ŭ�������� �˾�â ����
            UnitSelectFloatingUI unitInfo = UIManager.Instance.ShowUI<UnitSelectFloatingUI>("UnitSelectFloatingUI");

            //�˾�â�� Ÿ�� ���� �Ѱ��ֱ�

            unitInfo.InitUnitInfo(this);
        }
     


    }

    public void SetPreviewMaterial(bool canPlace)
    {
        // ��ġ ������ ���� �Ұ����� ���� ���׸��� ����
        Material targetMaterial = canPlace ? enabledMaterial : disabledMaterial;

        if (unitSprite != null)
        {
            unitSprite.material = targetMaterial;
            unitBaseSprite.material = targetMaterial;

            // �������� ���� Sorting Order�� ���� ����
            unitSprite.sortingOrder = 100;
            unitBaseSprite.sortingOrder = 99;  // base�� �Ѵܰ� �Ʒ���
        }
    }


    // ������ ���� �� ���� ���׸���� ����
    public void DestroyPreviewUnit()
    {

        // ���� �������� ��ȯ �� ���� sorting order�� ����
        unitSprite.sortingOrder = unitSortingOrder;
        unitBaseSprite.sortingOrder = baseSortingOrder;

        unitSprite.material = originalMaterial;
        unitBaseSprite.material = originalMaterial;


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
