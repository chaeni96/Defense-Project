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

public enum StatSubject
{
    System,         //시스템 스탯
    PlayerCommon,   //플레이어 유닛 전부 적용
    EnemyCommon,    //적 유닛 전부 적용

    PlayerArchor,   //궁수류 플레이어 유닛
    //...
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

    //상태이상 관련 스탯
    Bleed,
    Poison,
    Stun,
    Slow,
    Curse,

    //웨이브 관련 시간 스탯 추가
    WaveTime,           // 일반 웨이브 시간 (30초)
    WaveRestTime,       // 웨이브 사이 쉬는 시간 (20초) 
    WaveMinRestTime,    // 최소 쉬는 시간 (5초)
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
