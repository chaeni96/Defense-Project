using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kylin.FSM
{
    using UnityEngine;

    public abstract class StateBase
    {
        public int Id { get; private set; }
        protected StateController Controller { get; private set; }
        protected GameObject Owner; // ������ MonoBehaviour (�Ϲ������� FSMObjectBase)

        internal void SetID(int Id)
        {
            this.Id = Id;
        }
        internal void Initialize(StateController controller, GameObject owner)
        {
            Controller = controller;
            Owner = owner;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
    }
}