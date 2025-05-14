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
    Dog_Sword,
    Dog_Rogue,
    Dog_Boxing,

    
}
//���� Ÿ������ �����ص���
public enum AnimControllerType
{
    None,
    AttackBow,
    AttackSpear,
    AttackSword,
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

public enum ItemGrade
{
    Normal = 0,
    Rare,
    Epic,
    Legendary,
    Mythic,
}

public enum TriggerKeyword
{
    None = 0,
    Idle,
    Run,
    Attack,

}
