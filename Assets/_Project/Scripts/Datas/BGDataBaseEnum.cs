using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BGDatabaseEnum
{
    public enum SceneType
    {
        Lobby,
        Game,
    }

    // TODO ������ : Grade Enum ���� �ؾ��ϳ�..?
    public enum Grade
    {
        Normal,
        Rare,
        Epic,
        Legendary,
        Mythic,
    }


    public enum UnitGrade
    {
        Normal,
        Rare,
        Epic,
        Legendary,
        Mythic,
    }

    public enum SkillAttackType
    {
        None,
        Projectile,
        AOE,
    }

    public enum ObjstacleTileType
    {
        Basic, // ��ġ �Ұ�, ���� �Ұ�, ���� �̵� �Ұ� 
        Rock,
        Swamp,
        Lava,

    }


    public enum ObstacleTileType
    {
        Basic, // ��ġ �Ұ�, ���� �Ұ�, ���� �̵� �Ұ� 
        Rock,
        Swamp,
        Lava,

    }


    public enum EnemyType
    {
        Normal,
        Boss,

    }


}
