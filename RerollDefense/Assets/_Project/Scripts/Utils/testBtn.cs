using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testBtn : MonoBehaviour
{
    [SerializeField]private Kylin.FSM.UnitFSMObject fsmobj;


    public void OnclickTest()
    {
        fsmobj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.MoveRequested);
    }



}
