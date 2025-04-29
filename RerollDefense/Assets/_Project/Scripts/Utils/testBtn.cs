using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testBtn : MonoBehaviour
{
    [SerializeField]private Kylin.FSM.CharacterFSMObject fsmobj;


    public void OnclickTest()
    {
        fsmobj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.MoveRequested);
    }



}
