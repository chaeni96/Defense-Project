using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitController : PlacedObject
{
    public LayerMask targetLayer; // Ÿ������ �� Layer


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

    public BasicObject targetObject;

    public SkillAttackType attackType;

    private bool isActive = true;

    public override void Initialize()
    {
        base.Initialize();

    }

    //tileShpaeName�� ���� ���� ���ؼ� �������� �������°�
    public void InitializeUnitStat(string tileShapeName, int tileIndex)
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            // ��� Ÿ�� �ε����� �´� f_unitBuildData ��������
            if (tileIndex < tileShapeData.f_unitBuildData.Count)
            {
                var buildData = tileShapeData.f_unitBuildData[tileIndex];
                var unitData = buildData.f_unitData;
                attackType = unitData.f_SkillAttackType;
                unitNameText.text = unitData.f_name;
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

        }

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


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // ���� ���� �ð�ȭ
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
