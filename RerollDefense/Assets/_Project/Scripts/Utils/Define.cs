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

    public enum EnemyType
    {
        Normal,
        Boss,
        
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


}


//database에서 사용안하는 enum
public class Define
{
 
    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }



}
