using UnityEngine;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Idle")]
    public class IdleState : StateBase
    {
        [SerializeField] private int testValueIdle;

        public override void OnEnter()
        {
            Debug.Log("IDle : State Enter!!");
        }

        public override void OnUpdate()
        {
        }
    }
}