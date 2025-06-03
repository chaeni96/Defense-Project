using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : SkillBase
{



    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);

    }

    public override void Fire(BasicObject target)
    {
      
    }

    public override void DestroySkill()
    {
        ownerObj = null;
    }

}