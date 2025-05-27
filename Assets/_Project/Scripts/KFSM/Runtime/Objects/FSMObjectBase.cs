using UnityEngine;
using System.Linq;
using BansheeGz.BGDatabase;
using Kylin.LWDI;

namespace Kylin.FSM
{
    public interface IFSMSubscriber
    {
        abstract void SetTrigger(Trigger trigger);
        abstract void ChangeState(int stateID);

    }

    public abstract class FSMObjectBase : MonoBehaviour, IFSMSubscriber, IDependencyObject
    {
        [SerializeField] protected FSMDataCollection dataCollection; // ������ �÷���
        [SerializeField] protected string fsmId;

        protected FSMDataAsset cachedDataAsset;
        public StateController stateMachine;
        public Animator animator;
        
        private string currentAnimTrigger = ""; // ���� ���� ���� �ִϸ��̼� Ʈ����
        private bool skipNextAnimationTransition = false; // ���� �ִϸ��̼� Ʈ������ ��ŵ ����

        public bool isEnemy;
        public bool isFinished;

        protected FSMDataAsset LoadFSMDataById()
        {
            if (string.IsNullOrEmpty(fsmId) || dataCollection == null)
            {
                return null;
            }

            var data = dataCollection.GetFSMDataById(fsmId);
            if (data == null)
            {
                return null;
            }

            return data;
        }

        protected virtual void ConfigureStateMachine(out StateBase[] states, out Transition[] transitions, out int initialStateId)
        {
            cachedDataAsset = LoadFSMDataById();

            if (cachedDataAsset == null)
            {
                Debug.LogWarning("FSM Data not found. Creating empty FSM.", this);
                states = new StateBase[0];
                transitions = new Transition[0];
                initialStateId = 0;
                return;
            }

            states = StateFactory.CreateStates(cachedDataAsset);
            transitions = TransitionConverter.ConvertToRuntimeTransitions(cachedDataAsset.Transitions);

            // �⺻ �ʱ� ���� ID ����
            initialStateId = cachedDataAsset.InitialStateId;
            var tempID = initialStateId;
            // ��ȿ�� �������� Ȯ��
            bool validInitialState = states.Any(s => s != null && s.Id == tempID);
            if (!validInitialState && states.Length > 0)
            {
                initialStateId = states[0].Id; // ù ��° ��ȿ�� ���·� ��ü
            }
        }

        protected virtual void Initialized()
        {

            ConfigureStateMachine(out var states, out var transitions, out var initId);
            if (stateMachine == null)
            {
                stateMachine = new StateController();
            }
            stateMachine.Clear();
            
            using (var scope = DependencyInjector.CreateScope())
            {
                scope.RegisterInstance(typeof(FSMObjectBase), this);
                scope.RegisterInstance(this.GetType(), this);
                stateMachine.Initialize(states, transitions, initId, this, scope);
                stateMachine.RegistFSMSubscriber(this);
            }
        }

        private void Update()
        {
            stateMachine?.Update();
        }
        
        public virtual void CancelFSM()
        {
            stateMachine?.CancleFSMSubscriber(this);
        }

        void OnDestroy()
        {
            CancelFSM();
        }

        public virtual void SetTrigger(Trigger trigger)
        {
            if (animator != null && trigger != Trigger.None)
            {
                string triggerName = trigger.ToString();

                if (HasAnimatorTrigger(triggerName))
                {
                    animator.SetTrigger(triggerName);
                }
                else
                {
                    Debug.LogWarning($"Animator에 '{triggerName}' 트리거가 없습니다.");
                }
            }
        }
        
        private bool HasAnimatorTrigger(string triggerName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;
        
            foreach (var parameter in animator.parameters)
            {
                if (parameter.name == triggerName && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    return true;
                }
            }
    
            return false;
        }
        

        // FSM ID�� ������Ʈ�ϴ� �޼���
        public void UpdateFSM(string newFsmId)
        {
            if (fsmId == newFsmId) return; // ���� ID�� ���� ���ʿ�

            // ���� FSM ����
            CancelFSM();

            // �� ID ����
            fsmId = newFsmId;

            // FSM ���ʱ�ȭ
            Initialized();
        }


        public void ChangeState(int stateID)
        {
        }

#if UNITY_EDITOR
        // �����Ϳ��� ���� ���� ǥ�ø� ���� �����?�Ӽ�
        public string CurrentStateName
        {
            get
            {
                if (stateMachine == null) return "Not Initialized";

                var currentId = stateMachine.CurrentStateId;

                if (cachedDataAsset == null) return $"State ID: {currentId}";

                var stateEntry = cachedDataAsset.StateEntries?.FirstOrDefault(s => s.Id == currentId);

                if (stateEntry != null)
                {
                    return stateEntry.stateTypeName.Split('.').Last();
                }

                return $"State ID: {currentId}";
            }
        }

        //��FSM ID
        public string CurrentFSMId => fsmId;
#endif
    }
}