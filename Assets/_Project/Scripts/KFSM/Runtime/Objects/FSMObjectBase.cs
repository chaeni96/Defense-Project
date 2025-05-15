using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using BansheeGz.BGDatabase;
using Kylin.LWDI;

namespace Kylin.FSM
{
    public class CatFSMObject : FSMObjectBase
    {
        public CatFSMObject(D_UnitData unitData)
        {
            //fsmId = unitData.f_FsmID;

            Initialized();
        }
        public void SkillFire()
        {
        }

    }
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

        private CancellationTokenSource localCts;
        private CancellationTokenSource linkedCts;
        public Animator animator;

        private IScope _currentScope;

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

        void Awake()
        {
            localCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token);
            Application.quitting += CancelFSM;

            Initialized();
        }

        protected virtual void Initialized()
        {

            ConfigureStateMachine(out var states, out var transitions, out var initId);
            
            _currentScope = DependencyInjector.CreateScope();
            _currentScope.RegisterInstance(typeof(FSMObjectBase), this);
            _currentScope.RegisterInstance(this.GetType(), this);
            stateMachine = new StateController();
            stateMachine.Initialize(states, transitions, initId, this, _currentScope);
            stateMachine.RegistFSMSubscriber(this);
            // ������Ʈ ���� �����׽�ũ�� �ߴµ� �³� �𸣰��� Ȯ���Ұ� TODO : ���?
            RunLoop(linkedCts.Token).Forget();
        }

        private async UniTaskVoid RunLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    stateMachine?.Update();
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // ��� ���� ó��
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in FSM update loop: {e.Message}\n{e.StackTrace}");
            }
        }

        public virtual void CancelFSM()
        {

            stateMachine.CancleFSMSubscriber(this);

            if (localCts != null && !localCts.IsCancellationRequested)
            {
                localCts.Cancel();
            }
        }

        void OnDestroy()
        {
            CancelFSM();

            Application.quitting -= CancelFSM;
        }

        //�ִϸ��̼� ��� �޼���
        public virtual void SetTrigger(Trigger trigger)
        {
            if (animator != null && trigger != Trigger.None)
            {
                //string triggerName = trigger.ToString();

                //// 1. Ʈ���Ű� �ִϸ����Ϳ� �����ϴ��� Ȯ��
                //bool triggerExists = false;
                //foreach (AnimatorControllerParameter param in animator.parameters)
                //{
                //    if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                //    {
                //        triggerExists = true;
                //        break;
                //    }
                //}

                //// 2. Ʈ���Ű� �ִϸ����Ϳ� ������ �������� ����
                //if (!triggerExists)
                //{
                //    return;
                //}

                //// 3. ���� ���� ���� �ִϸ��̼ǰ� ������ Ʈ�������� Ȯ��
                //if (currentAnimTrigger == triggerName)
                //{
                //    return;
                //}

                //// 4. Ʈ������ ��ŵ �ɼ� Ȯ��
                //if (skipNextAnimationTransition)
                //{
                //    skipNextAnimationTransition = false; // �÷��� ����
                //    return;
                //}

                //// 5. �ִϸ��̼� Ʈ���� ����
                string triggerName = trigger.ToString();
                animator.SetTrigger(triggerName);
            }
        }

        // ���� �ִϸ��̼� Ʈ�������� ��ŵ�ϵ��� �����ϴ� �޼���
        public void SkipNextAnimationTransition()
        {
            skipNextAnimationTransition = true;
        }

        // FSM ID�� ������Ʈ�ϴ� �޼���
        public void UpdateFSM(string newFsmId)
        {
            if (fsmId == newFsmId) return; // ���� ID�� ���� ���ʿ�

            // ���� FSM ����
            CancelFSM();

            // �� ID ����
            fsmId = newFsmId;

            localCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token);
            Application.quitting += CancelFSM;

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