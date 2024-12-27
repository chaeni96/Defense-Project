using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObject : BasicObject
{

    //stat들 여기서 가져오기
    public int attack;
    public int attackSpeed;
    public int attackRange;
    public int unitCost;
    public int unitBlockSize;
    public float attackCoolDown;

    public SkillAttackType attackType;

    public override void Initialize()
    {
        base.Initialize();

    }


    public override void Update()
    {
        base.Update();

    }

    //tileShpaeName은 이제 상점 통해서 랜덤으로 가져오는것
    public void InitializeUnitStat(string tileShapeName, int tileIndex)
    {
        var tileShapeData = D_TileShpeData.FindEntity(data => data.f_name == tileShapeName);

        if (tileShapeData != null)
        {
            // 상대 타일 인덱스에 맞는 f_unitBuildData 가져오기
            if (tileIndex < tileShapeData.f_unitBuildData.Count)
            {
                var buildData = tileShapeData.f_unitBuildData[tileIndex];
                var unitData = buildData.f_unitData;
                attackType = unitData.f_SkillAttackType;

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
                            case "Cost":
                                this.unitCost = statData.f_value;
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
          
        }
       
    }


}
