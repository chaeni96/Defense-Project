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

    PlayerArcher,   //궁수류 플레이어 유닛
    PlayerMage,     //마법사류 플레이어 유닛
    PlayerBase,

    EnemyNormal, // 일반 몬스터
    EnemyFaster, // 스피드 빠른 일반 몬스터
    EnemyBoss, //보스 몬스터
    

}


public enum StatName
{

    //기존 유닛 스탯
    MaxHP,
    ATK, //공격력
    AttackSpeed, //공격 속도, 몇초마다 공격할지
    AttackRange,
    MoveSpeed,

    //시스템 관련 스탯
    Cost,
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
    WaveTime,           // 일반 웨이브 시간
    WaveRestTime,       // 웨이브 사이 쉬는 시간 - 와일드 카드 등장
    WaveMinRestTime,    // 최소 쉬는 시간

    //추가 스탯
    CurrentHp, 
    MaxCost,
    UnitStarLevel,
    StoreLevel,


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
public enum BuffType
{
    Temporal,      // 일정시간동안 효과가 발동하는 버프 - 시간기반
    Instant,      // 즉시 적용되는 영구 버프
    Range,        // 범위기반 버프

}


public enum BuffTickType
{
    Instant,    // 버프 시작시 한번만 적용
    Periodic,   // 틱마다 적용 (DoT/HoT)
    Continuous  // 지속적으로 적용되다가 버프 종료시 제거
}