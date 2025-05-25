using System.Collections;
using System.Collections.Generic;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    using UnityEngine;

    public abstract class StateBase :  IDependencyObject, IInjectable
    {
        public int Id { get; private set; }
        
        internal void SetID(int Id)
        {
            this.Id = Id;
        }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }

        public void Inject(IScope scope = null)
        {
            DependencyInjector.Inject(this, scope);
        }

        public virtual void RegisterServices(IScope scope)
        {
        }
    }
}