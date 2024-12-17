using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitObject : PlacedObject
{
    public override void Initialize()
    {
        base.Initialize();

        ChangeState(new UnitIdleState());
    }

    public override void Update()
    {
        base.Update();
    }
}
