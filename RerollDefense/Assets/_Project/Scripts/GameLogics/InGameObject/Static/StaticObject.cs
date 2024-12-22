using BansheeGz.BGDatabase;
using BGDatabaseEnum;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObject : BasicObject
{
    //고정(배치 가능한) 오브젝트

    [HideInInspector]
    public Vector3Int previousTilePosition; // 이전 타일 위치
    [HideInInspector]
    public List<Vector3Int> relativeTiles; // 타일 배치 크기

    //stat들 여기서 가져오기
    public int attack;
    public int attackSpeed;
    public int attackRange;
    public int unitCost;
    public int unitBlockSize;

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
                        }
                    }

                    Debug.Log($"스탯 초기화 완료: {unitData.f_name}, 공격력: {attack}, 공격 속도: {attackSpeed}, 블록 가격 : {unitCost} 블록사이즈: {unitBlockSize}");
                }
                else
                {
                    Debug.LogError("UnitData 없음");
                }
            }
          
        }
       
    }


}
