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


}


//database���� �����ϴ� enum
public class Define
{
 
    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }



}
