using System;
using UnityEngine;
namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Move")]
    public class ChaseState : StateBase
    {
        [SerializeField] public int testIntValue;
        [SerializeField] public float testIntValue2;
        [SerializeField] public string testIntValue3;
        [SerializeField] public bool testIntValue4;
        [SerializeField] public int testIntValue5;
        [SerializeField] public int testIntValue6;
        public override void OnEnter()
        {
        }

        public override void OnUpdate()
        {
        }
    }
}