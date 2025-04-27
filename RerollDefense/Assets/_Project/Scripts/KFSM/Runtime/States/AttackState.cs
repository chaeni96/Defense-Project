namespace Kylin.FSM
{
    using UnityEngine;

    [FSMContextFolder("Create/State/Attack")]
    public class AttackState : StateBase
    {
        private float _start;
        public override void OnEnter()
        {
            _start = Time.time;
            Controller.RegisterTrigger(Trigger.AttackAnimation); //�ִϸ��̼� �ߵ��� Ʈ����
            Controller.AddPersistentTrigger(Trigger.SuperArmor);
        }

        public override void OnUpdate()
        { 
        }

        public override void OnExit()
        {
            Controller.RemovePersistentTrigger(Trigger.SuperArmor);
        }
    }
}