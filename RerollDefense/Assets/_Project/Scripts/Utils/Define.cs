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


public enum StatName
{
    Health, //ü��
    ATK, //���ݷ�
    AttackSpeed, //���� �ӵ�, ���ʸ��� ��������
    AttackRange,
    MoveSpeed,
    ProjectileSpeed,


}

public enum CardGrade
{
    Normal,
    Rare,
    Epic
}