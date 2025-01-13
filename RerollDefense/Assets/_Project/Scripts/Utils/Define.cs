using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//database에서 사용안하는 enum
public enum SceneKind
{
    Lobby,
    InGame,
}

public enum Sound
{
    Bgm,
    Effect,
    MaxCount,
}


public enum StatName
{

    //기존 유닛 스탯
    Health, //체력
    ATK, //공격력
    AttackSpeed, //공격 속도, 몇초마다 공격할지
    AttackRange,
    MoveSpeed,

    //시스템 관련 스탯
    CostAdded,
    RerollCost,
    //스킬 관련 스탯
    ProjectileSpeed,
    ProjectileCount,
    ProjectileInterval,   // 연속 발사 간격
    AOERange,
}

public enum CardGrade
{
    Normal,
    Rare,
    Epic
}


public enum UnitType
{
    Base,
    Archer,
    Archer2,
    Mage,
}
