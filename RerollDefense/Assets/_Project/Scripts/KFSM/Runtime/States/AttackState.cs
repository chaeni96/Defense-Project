using System;
using UnityEngine;
using Kylin.FSM;

[FSMContextFolder("Create/State/Attack")]
public class AttackState : StateBase
{
 
    public override void OnEnter()
    {
        Debug.Log("AttackState 시작");
     
    }

    public override void OnUpdate()
    {
      

        
    }

    public override void OnExit()
    {
        Debug.Log("AttackState 종료");
    }

   
}