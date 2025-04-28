using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Kylin.FSM
{
    public class StateController //사실상 FSM의 실체?임
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

        private GameObject _ownerObject; // 소유자 GameObject


        public void Initialize(StateBase[] states, Transition[] transitions, int initialStateId, GameObject owner)
        {
            //_states = states;
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

            // 상태 초기화
            foreach (var s in states) s?.Initialize(this, _ownerObject);

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
            _eventMask = 0; // 이벤트성 트리거만 일회성
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
            // 현재 상태 리스트 + AnyState 리스트
            var list = _transitionsByState.TryGetValue(_currentStateId, out var cs)
                ? cs : Array.Empty<Transition>();
            var any = _transitionsByState[Transition.ANY_STATE];

            // 상태별 전환
            for (int i = 0; i < list.Length; i++)
            {
                var t = list[i];
                if ((mask & t.RequiredMask) == t.RequiredMask && (mask & t.IgnoreMask) == 0)
                {
                    ChangeState(t.ToStateId);
                    return;
                }
            }
            // AnyState 전환
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

        private void ChangeState(int newStateId)
        {
            _states[_currentStateId]?.OnExit();
            _currentStateId = newStateId;
            _states[_currentStateId]?.OnEnter();
        }

        public void Update() => _states[_currentStateId]?.OnUpdate();
    }
}