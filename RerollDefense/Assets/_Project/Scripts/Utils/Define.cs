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

    PlayerArcher_1Star,   //�ü��� �÷��̾� ����
    PlayerMage_1Star,     //������� �÷��̾� ����
    PlayerBase,

    PlayerArhcer_2Star,
    PlayerArhcer_3Star,
    PlayerMage_2Star,
    PlayerMage_3Star,


    EnemyNormal, // �Ϲ� ����
    EnemyFaster, // ���ǵ� ���� �Ϲ� ����
    EnemyBoss, //���� ����
    

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
    WaveTime,           // �Ϲ� ���̺� �ð� (30��)
    WaveRestTime,       // ���̺� ���� ���� �ð� (20��) 
    WaveMinRestTime,    // �ּ� ���� �ð� (5��)

    //�߰� ����
    CurrentHp, 
    StoreLevel,
    UnitStarLevel,


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
    Temporal,      // �����ð����� ȿ���� �ߵ��ϴ� ���� - �ð����
    Instant,      // ��� ����Ǵ� ���� ����

}


public enum BuffTickType
{
    Instant,    // ���� ���۽� �ѹ��� ����
    Periodic,   // ƽ���� ���� (DoT/HoT)
    Continuous  // ���������� ����Ǵٰ� ���� ����� ����
}