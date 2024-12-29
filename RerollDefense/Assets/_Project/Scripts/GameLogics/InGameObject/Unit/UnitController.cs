using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitController : PlacedObject
{
    //BgDatabase에서 읽어오기
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
    public float attackTimer = 0f;  // 타이머 추가

    public BasicObject targetObject;

    public SkillAttackType attackType;

    private bool isActive = true;

    public override void Initialize()
    {
        base.Initialize();

    }

    //tileShpaeName은 이제 상점 통해서 랜덤으로 가져오는것
    public void InitializeUnitStat(D_unitBuildData buildData)
    {
        if (buildData == null) return;

        var unitData = buildData.f_unitData;
        attackType = unitData.f_SkillAttackType;
        //unitNameText.text = unitData.f_name;
        if (unitData != null)
        {
            // statDatas를 순회하면서 스탯 설정
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
            Debug.LogError("UnitData 없음");
        }

    }

    public bool IsActive() => isActive;

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            attackTimer = 0f;  // 타이머 리셋
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 공격 범위 시각화
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
#endif
}
