using UnityEngine;

namespace Kylin.FSM
{
    [FSMContextFolder("Create/State/Idle")]
    public class IdleState : StateBase
    {
        [SerializeField] private int testValueIdle;
        public override void OnUpdate()
        {
        }
    }
}