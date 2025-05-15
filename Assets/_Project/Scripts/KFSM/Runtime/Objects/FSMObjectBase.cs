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
        [SerializeField] protected FSMDataCollection dataCollection; // µ¥ÀÌÅÍ ÄÃ·º¼Ç
        [SerializeField] protected string fsmId;

        protected FSMDataAsset cachedDataAsset;
        public StateController stateMachine;

        private CancellationTokenSource localCts;
        private CancellationTokenSource linkedCts;
        public Animator animator;

        private IScope _currentScope;

        private string currentAnimTrigger = ""; // ÇöÀç ½ÇÇà ÁßÀÎ ¾Ö´Ï¸ÞÀÌ¼Ç Æ®¸®°Å
        private bool skipNextAnimationTransition = false; // ´ÙÀ½ ¾Ö´Ï¸ÞÀÌ¼Ç Æ®·£Áö¼Ç ½ºÅµ ¿©ºÎ

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

            // ±âº» ÃÊ±â »óÅÂ ID ¼³Á¤
            initialStateId = cachedDataAsset.InitialStateId;
            var tempID = initialStateId;
            // À¯È¿ÇÑ »óÅÂÀÎÁö È®ÀÎ
            bool validInitialState = states.Any(s => s != null && s.Id == tempID);
            if (!validInitialState && states.Length > 0)
            {
                initialStateId = states[0].Id; // Ã¹ ¹øÂ° À¯È¿ÇÑ »óÅÂ·Î ´ëÃ¼
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
            // ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Æ® ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿½×½ï¿½Å©ï¿½ï¿½ ï¿½ß´Âµï¿½ ï¿½Â³ï¿½ ï¿½ð¸£°ï¿½ï¿½ï¿½ È®ï¿½ï¿½ï¿½Ò°ï¿½ TODO : ï¿½ï¿½â¸?
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
                // Ãë¼Ò ¿¹¿Ü Ã³¸®
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

        //¾Ö´Ï¸ÞÀÌ¼Ç Àç»ý ¸Þ¼­µå
        public virtual void SetTrigger(Trigger trigger)
        {
            if (animator != null && trigger != Trigger.None)
            {
                //string triggerName = trigger.ToString();

                //// 1. Æ®¸®°Å°¡ ¾Ö´Ï¸ÞÀÌÅÍ¿¡ Á¸ÀçÇÏ´ÂÁö È®ÀÎ
                //bool triggerExists = false;
                //foreach (AnimatorControllerParameter param in animator.parameters)
                //{
                //    if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
                //    {
                //        triggerExists = true;
                //        break;
                //    }
                //}

                //// 2. Æ®¸®°Å°¡ ¾Ö´Ï¸ÞÀÌÅÍ¿¡ ¾øÀ¸¸é ½ÇÇàÇÏÁö ¾ÊÀ½
                //if (!triggerExists)
                //{
                //    return;
                //}

                //// 3. ÇöÀç ½ÇÇà ÁßÀÎ ¾Ö´Ï¸ÞÀÌ¼Ç°ú µ¿ÀÏÇÑ Æ®¸®°ÅÀÎÁö È®ÀÎ
                //if (currentAnimTrigger == triggerName)
                //{
                //    return;
                //}

                //// 4. Æ®·£Áö¼Ç ½ºÅµ ¿É¼Ç È®ÀÎ
                //if (skipNextAnimationTransition)
                //{
                //    skipNextAnimationTransition = false; // ÇÃ·¡±× ¸®¼Â
                //    return;
                //}

                //// 5. ¾Ö´Ï¸ÞÀÌ¼Ç Æ®¸®°Å ½ÇÇà
                string triggerName = trigger.ToString();
                animator.SetTrigger(triggerName);
            }
        }

        // ´ÙÀ½ ¾Ö´Ï¸ÞÀÌ¼Ç Æ®·£Áö¼ÇÀ» ½ºÅµÇÏµµ·Ï ¼³Á¤ÇÏ´Â ¸Þ¼­µå
        public void SkipNextAnimationTransition()
        {
            skipNextAnimationTransition = true;
        }

        // FSM ID¸¦ ¾÷µ¥ÀÌÆ®ÇÏ´Â ¸Þ¼­µå
        public void UpdateFSM(string newFsmId)
        {
            if (fsmId == newFsmId) return; // °°Àº ID¸é º¯°æ ºÒÇÊ¿ä

            // ±âÁ¸ FSM Á¤¸®
            CancelFSM();

            // »õ ID ¼³Á¤
            fsmId = newFsmId;

            localCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token);
            Application.quitting += CancelFSM;

            // FSM ÀçÃÊ±âÈ­
            Initialized();
        }


        public void ChangeState(int stateID)
        {
        }

#if UNITY_EDITOR
        // ï¿½ï¿½ï¿½ï¿½ï¿½Í¿ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ Ç¥ï¿½Ã¸ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½ï¿½ï¿½ï¿½ï¿?ï¿½Ó¼ï¿½
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

        //ï¿½ï¿½FSM ID
        public string CurrentFSMId => fsmId;
#endif
    }
}