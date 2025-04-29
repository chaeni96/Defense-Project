using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;
using BansheeGz.BGDatabase;

namespace Kylin.FSM
{
    /// <summary>
    /// FSMObjectBase상속받은 유닛 예시임.
    /// </summary>
    public class CatFSMObject : FSMObjectBase
    {
        public CatFSMObject(D_UnitData unitData)
        {
            //fsmId = unitData.f_FsmID;

            Initialized();
        }
        public void SkillFire()
        {
            //스킬별 유닛별 별도 스킬 사용 >> 특정 스킬 사용
        }

    }
    public interface IFSMSubscriber
    {
        abstract void SetTrigger(Trigger trigger);
        abstract void ChangeState(int stateID);

    }

    public abstract class FSMObjectBase : MonoBehaviour, IFSMSubscriber
    {
        [SerializeField] protected FSMDataCollection dataCollection; //임시임 나중에 manager에 데이터 보관하던가 할것(혹은 BG로 다 넘기던가)
        [SerializeField] protected string fsmId;

        protected FSMDataAsset cachedDataAsset;
        public StateController stateMachine;

        private CancellationTokenSource localCts;
        private CancellationTokenSource linkedCts; //쓸일 있을지? 무리어미 같은거..?
        public Animator animator;



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

            // 데이터에서 초기 상태 ID 로드
            initialStateId = cachedDataAsset.InitialStateId;
            var tempID = initialStateId;
            // 유효한 상태인지 검증
            bool validInitialState = states.Any(s => s != null && s.Id == tempID);
            if (!validInitialState && states.Length > 0)
            {
                initialStateId = states[0].Id; // 첫 번째 유효한 상태로 대체
            }
        }

        void Awake()
        {
            //일단 토큰여기서 ㅇㅇ 나중에 초기화로 빼던지 ㄱ 링크는 잘 모르겠음 의미있나
            localCts = new CancellationTokenSource();
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token);
            Application.quitting += CancelFSM;

            Initialized();
        }

        protected void Initialized()
        {
            ConfigureStateMachine(out var states, out var transitions, out var initId);


            stateMachine = new StateController();
            stateMachine.Initialize(states, transitions, initId, this);
            stateMachine.RegistFSMSubscriber(this);
            // 업데이트 루프 유니테스크로 했는데 맞나 모르겠음 확인할것 TODO : 김기린
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
                // 정상적인 취서
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

        //수정해야될부분 트리거 넣으면 무조건 애니메이션 재생됨
        public void SetTrigger(Trigger trigger)
        {
            if (animator != null && trigger != Trigger.None)
            {
                // 열거형 값의 이름을 애니메이터 트리거로 사용
                string triggerName = trigger.ToString();
                animator.SetTrigger(triggerName);

            }
        }

      
        public void ChangeState(int stateID)
        {
        }

#if UNITY_EDITOR
        // 에디터에서 현재 상태 표시를 위한 디버그 속성
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

        //현FSM ID
        public string CurrentFSMId => fsmId;
#endif
    }
}