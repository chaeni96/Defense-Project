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
    Health, //체력
    ATK, //공격력
    AttackSpeed, //공격 속도, 몇초마다 공격할지
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