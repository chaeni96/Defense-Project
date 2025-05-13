using System.Collections;
using System.Collections.Generic;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    using UnityEngine;

    public abstract class StateBase : IInjectable
    {
        public int Id { get; private set; }
        
        protected IScope fsmScope { get; private set; }

        internal void SetID(int Id)
        {
            this.Id = Id;
        }
        internal void Initialize(IScope fsmScope)
        {
            this.fsmScope = fsmScope;
            
            Inject();
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }
        public void Inject()
        {
            DependencyInjector.InjectWithScope(this, fsmScope);
        }
    }
}