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
    PlayerMage,     //������� �÷��̾� ����
    PlayerBase,

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
    WaveTime,           // �Ϲ� ���̺� �ð�
    WaveRestTime,       // ���̺� ���� ���� �ð� - ���ϵ� ī�� ����
    WaveMinRestTime,    // �ּ� ���� �ð�

    //�߰� ����
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
    Temporal,      // �����ð����� ȿ���� �ߵ��ϴ� ���� - �ð����
    Instant,      // ��� ����Ǵ� ���� ����
    Range,        // ������� ����

}


public enum BuffTickType
{
    Instant,    // ���� ���۽� �ѹ��� ����
    Periodic,   // ƽ���� ���� (DoT/HoT)
    Continuous  // ���������� ����Ǵٰ� ���� ����� ����
}