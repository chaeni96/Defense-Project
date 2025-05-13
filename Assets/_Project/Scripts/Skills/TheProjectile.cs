using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : SkillBase
{



    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);

    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
      
    }

    public override void DestroySkill()
    {
        owner = null;
    }

}