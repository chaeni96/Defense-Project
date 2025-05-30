using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//database���� �����ϴ� enum
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
    System,         //�ý��� ����
    PlayerCommon,   //�÷��̾� ���� ���� ����
    EnemyCommon,    //�� ���� ���� ����

    PlayerArcher,   //�ü��� �÷��̾� ����
    PlayerSwordman,
    PlayerSpearman,

    EnemyArcher,
    EnemySwordman,
    EnemySpearman,

    RangeDealer, //���Ÿ� ����
    MeleeDealer, //�ٰŸ� ����
    Support, //������
    Tank, //��Ŀ
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

    //���� ���� ����
    MaxHP,
    ATK, //���ݷ�
    AttackSpeed, //���� �ӵ�, ���ʸ��� ��������
    AttackRange,
    MoveSpeed,

    //�ý��� ���� ����
    Cost,
    RerollCost,
    //��ų ���� ����
    ProjectileSpeed,
    ProjectileCount,
    ProjectileInterval,   // ���� �߻� ����
    AOERange,

    //�����̻� ���� ����
    Bleed,
    Poison,
    Stun,
    Slow,
    Curse,

    //���̺� ���� �ð� ���� �߰�
    WaveTime,           // �Ϲ� ���̺� �ð�
    WaveRestTime,       // ���̺� ���� ���� �ð� - ���ϵ� ī�� ����
    WaveMinRestTime,    // �ּ� ���� �ð�

    //�߰� ����
    CurrentHp, 
    MaxCost,
    UnitStarLevel,
    StoreLevel,
    CostChargingSpeed,
    WildCardCount,

    InventoryCount,//���� �κ��丮 ����
    UnitInventoryCount, // ���� �� �������ִ� �κ��丮 ����
    UnitPlacementCount, //��ġ�Ҽ� �ִ� ���� ���� 
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
//���� Ÿ������ �����ص���
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
    Temporal,      // �����ð����� ȿ���� �ߵ��ϴ� ���� - �ð����
    Instant,      // ��� ����Ǵ� ���� ����
    Range,        // ������� ����

}


public enum BuffTickType
{
    Fixed, // �ѹ� ����ǰ� �����Ǵ� ȿ�� (�̵��ӵ� 50% ��)
    Periodic, // �ֱ������� ����Ǵ� ȿ�� (ƽ���� ü�� -1 ��)

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

//�̺�Ʈ Ÿ��
public enum IngameEventType
{
    None,
    SpawnEnemy,
    DropItem,
    DropGold,
}

// �̺�Ʈ �ߵ� Ʈ���� 
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
    ReplaceBasicAttack, //�Ϲ� ���� ��ų(���� �Ҹ��ϰ� ��Ÿ ��� ���)
    Persistent, // ��Ÿ�� ������ �׻� ���� ex)����� ����� - ���� ��ų 0
    Conditional, //Ư�� ���ǿ����� �ߵ� - ���ǿ� �´°��� ��ų ���
    
    //Ʈ������ ��ų(���� �̺�Ʈ �߻���)
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


