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

    PlayerArchor,   //�ü��� �÷��̾� ����
    //...
}


public enum StatName
{

    //���� ���� ����
    Health, //ü��
    ATK, //���ݷ�
    AttackSpeed, //���� �ӵ�, ���ʸ��� ��������
    AttackRange,
    MoveSpeed,

    //�ý��� ���� ����
    CostAdded,
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
