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


    public enum UpgradeUnitType
    {
        None,
        Archer,
        Archer2,
        Mage,
        Base,
    }


    public enum EnemyType
    {
        Normal,
        Boss,

    }


}
