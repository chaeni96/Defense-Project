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
    PlayerSwordman,
    PlayerSpearman,

    EnemyArcher,
    EnemySwordman,
    EnemySpearman,

    RangeDealer, //원거리 딜러
    MeleeDealer, //근거리 딜러
    Support, //서포터
    Tank, //탱커
    Dog_Rogue,
    Dog_Sword,
    Dog_Boxing,
    Dog_Jester,
    Dog_Hunter,
    Dog_Coin,
    Dog_Potion,
    Dog_Hammer,
    Dog_shortsword,
    Royal_Spear,
    Royal_Sword,
    Royal_Priest,
    Royal_Wand,
    Royal_LongSword,
    Royal_Shield,
    Boss_Shield,
    Boss_Archer,
    Boss_Skull,
    Boss_Hammer,
    Boss_Priest,
    Boss_Shortsword,
    Boss_Boxing,
    Expedition_Hunter,
    Expedition_skull,
    Expedition_wand,
    Expedition_longsword,
    Love_Hunter,
    Love_sword,
    Love_archer,
    Love_potion,
    Love_priest,
    Love_wand,
    ShadowClan_Hunter,
    ShadowClan_Spear,
    ShadowClan_skull,
    ShadowClan_shortsword,
    ShadowClan_boxing,
    Clean_shield,
    Explo_archer,
    Explo_coin,
    Explo_potion,
    Explo_hammer,
    Explo_wand,





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
    CostChargingSpeed,
    WildCardCount,

    InventoryCount,//유저 인벤토리 개수
    UnitInventoryCount, // 유닛 당 가질수있는 인벤토리 개수
    UnitPlacementCount, //배치할수 있느 유닛 개수 
    CurrentMana,
    MaxMana,

}

public enum CardGrade
{
    Normal,
    Rare,
    Epic
}


public enum UnitType
{
    None,
    Dog_Rogue,
    Dog_Sword,
    Dog_Boxing,
    Dog_Jester,
    Dog_Hunter,
    Dog_Coin,
    Dog_Potion,
    Dog_Hammer,
    Dog_shortsword,
    Royal_Spear,
    Royal_Sword,
    Royal_Priest,
    Royal_Wand,
    Royal_LongSword,
    Royal_Shield,
    Boss_Shield,
    Boss_Archer,
    Boss_Skull,
    Boss_Hammer,
    Boss_Priest,
    Boss_Shortsword,
    Boss_Boxing,
    Expedition_Hunter,
    Expedition_skull,
    Expedition_wand,
    Expedition_longsword,
    Love_Hunter,
    Love_sword,
    Love_archer,
    Love_potion,
    Love_priest,
    Love_wand,
    ShadowClan_Hunter,
    ShadowClan_Spear,
    ShadowClan_skull,
    ShadowClan_shortsword,
    ShadowClan_boxing,
    Clean_shield,
    Explo_archer,
    Explo_coin,
    Explo_potion,
    Explo_hammer,
    Explo_wand,



}
//공격 타입으로 구분해도됨
public enum AnimControllerType
{
    None,
    AttackBow,
    AttackSpear,
    AttackSword,
    AttackPunch,
    AttackThrow,
}


public enum BuffType
{
    Temporal,      // 일정시간동안 효과가 발동하는 버프 - 시간기반
    Instant,      // 즉시 적용되는 영구 버프
    Range,        // 범위기반 버프

}


public enum BuffTickType
{
    Fixed, // 한번 적용되고 유지되는 효과 (이동속도 50% 등)
    Periodic, // 주기적으로 적용되는 효과 (틱마다 체력 -1 등)

}

public enum ObjectLayer
{
    IgnoereRayCast = 2,
    Enemy = 7,
    Player = 8,
}


public enum WaveType
{
    NormalBattle,
    BossBattle,
    EventEnemy,
    HuntingSelectTime,
    PrizeHunting,
    WildCardSelect,
}

//이벤트 타입
public enum IngameEventType
{
    None,
    SpawnEnemy,
    DropItem,
    DropGold,
}

// 이벤트 발동 트리거 
public enum EventTriggerType
{
    None,
    OnSpawn,
    OnDeath,
    OnDamaged,


}

public enum ItemType
{
    Currency,
    Equipment_Item,
}

public enum CurrencyType
{
    Heart,
    Gold,
    Gem,
}

public enum SkillAcivationType
{
    None = 0,
    ReplaceBasicAttack, //일반 마나 스킬(마나 소모하고 평타 대신 사용)
    Persistent, // 평타와 별개로 항상 켜짐 ex)대장냥 오우라 - 마나 스킬 0
    Conditional, //특정 조건에서만 발동 - 조건에 맞는곳에 스킬 사용
    
    //트리거형 스킬(즉정 이벤트 발생시)
    OnHIt,
    OnKill,
    OnDamaged,
}

public enum ItemGrade
{
    Normal = 0,
    Rare,
    Epic,
    Legendary,
    Mythic,
}


