using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Kylin.LWDI;
using UnityEngine;
namespace Kylin.FSM
{
    public class StateController : IDependencyObject //��ǻ�?FSM�� ��ü?��
    {
        private Dictionary<int, StateBase> _states;
        //private StateBase[] _states;
        private Dictionary<int, Transition[]> _transitionsByState
            = new Dictionary<int, Transition[]>();
        public int CurrentStateId => _currentStateId;
        public int PersistentMask => _persistentMask;

        private int _currentStateId;
        private int _persistentMask;
        private int _eventMask;

        private HashSet<IFSMSubscriber> subscribers = new();

        private FSMObjectBase _ownerObject; // ������ GameObject - �̰͵� �ٲ�ߵ�?

        private IScope _fsmScope;

        public void Initialize(StateBase[] states, Transition[] transitions, int initialStateId, FSMObjectBase owner, IScope currentScope)
        {
            //_states = states;
            _fsmScope = currentScope;
            _states = new Dictionary<int, StateBase>();
            foreach(var state in states)
            {
                _states[state.Id] = state;
            }
            _transitionsByState = transitions
                .OrderByDescending(t => t.Priority)
                .GroupBy(t => t.FromStateId)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToArray()
                );

            _ownerObject = owner;

            _fsmScope.RegisterInstance(typeof(StateController), this);

            // ���� �ʱ�ȭ
            foreach (var s in states)
            {
                //DependencyInjector.InjectWithScope(s, fsmScope);
                s?.Inject(_fsmScope);
            }

            ChangeState(initialStateId);
        }

        public void RegistFSMSubscriber(IFSMSubscriber fSMSubscriber)
        {
            subscribers.Add(fSMSubscriber);
        }
        public void CancleFSMSubscriber(IFSMSubscriber fSMSubscriber)
        {
            subscribers.Remove(fSMSubscriber);
        }
        public void RegisterTrigger(Trigger trig)
        {
            _eventMask |= (int)trig;

            foreach(var sub in subscribers)
            {
                sub.SetTrigger(trig);
            }
            TryTransition();
            _eventMask = 0; // �̺�Ʈ�� Ʈ���Ÿ� ��ȸ��
        }

        public void AddPersistentTrigger(Trigger trig)
        {
            _persistentMask |= (int)trig;
            TryTransition();
        }

        public void RemovePersistentTrigger(Trigger trig)
        {
            _persistentMask &= ~(int)trig;
        }

        private void TryTransition()
        {
            var mask = _persistentMask | _eventMask;
            // ���� ���� ����Ʈ + AnyState ����Ʈ
            var list = _transitionsByState.TryGetValue(_currentStateId, out var cs)
                ? cs : Array.Empty<Transition>();

            _transitionsByState.TryGetValue(Transition.ANY_STATE, out var any);
            //var any = _transitionsByState[Transition.ANY_STATE];

            // ���º� ��ȯ
            for (int i = 0; i < list.Length; i++)
            {
                var t = list[i];
                if ((mask & t.RequiredMask) == t.RequiredMask && (mask & t.IgnoreMask) == 0)
                {
                    ChangeState(t.ToStateId);
                    return;
                }
            }
            // AnyState ��ȯ
            if(any != null)
            {
                for (int i = 0; i < any.Length; i++)
                {
                    var t = any[i];
                    if ((mask & t.RequiredMask) == t.RequiredMask && (mask & t.IgnoreMask) == 0)
                    {
                        ChangeState(t.ToStateId);
                        return;
                    }
                }
            }
        }

        private void ChangeState(int newStateId)
        {
            _states[_currentStateId]?.OnExit();
            _currentStateId = newStateId;
            _states[_currentStateId]?.OnEnter();
        }

        public void Update() => _states[_currentStateId]?.OnUpdate();
    }
}