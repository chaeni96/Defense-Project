using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class State
{
    public TriggerKeyword animTrigger;
    public BasicObject basicObj;

    public abstract void EnterState(BasicObject obj);
    public abstract void UpdateState(BasicObject obj);
    public abstract void ExitState(BasicObject obj);
}
