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
        Basic, // 배치 불가, 삭제 불가, 몬스터 이동 불가 
        Rock,
        Swamp,
        Lava,

    }


    public enum ObstacleTileType
    {
        Basic, // 배치 불가, 삭제 불가, 몬스터 이동 불가 
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
