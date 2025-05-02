using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using System.Linq;
using static BansheeGz.BGDatabase.BGSyncNameMapConfig;

public class StageManager : MonoBehaviour, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{
    public static StageManager _instance;

    public WaveBase nextAssignWave = null;

    private Tilemap placedMap;
    private Transform tileMapGrid;

    private D_StageData currentStage;
    private int currentWaveIndex = 0;

    //웨이브 관련 설명창
    private int currentWaveInfoScheduleUID = -1;
    private const float waveInfoDuration = 1.6f;

    //private WildCardSelectUI selectUI;
    private WaveInfoFloatingUI waveInfoUI;
    private InGameCountdownUI countdownUI;
    private WaveFinishFloatingUI waveFinishUI;

    //웨이브 목록 관리
    private List<WaveBase> waveList = new List<WaveBase>();
    private WaveBase currentWave = null;

    private int totalRemainingEnemies = 0;

    private bool isAssignWave = false;

    // 웨이브 시작/종료 관련 이벤트
    public event Action OnWaveStart;  // 웨이브 종료시 발생하는 이벤트
    public event Action OnWaveFinish;  // 웨이브 종료시 발생하는 이벤트

    public event Action<int> OnEnemyCountChanged; //enemy 생성 또는 삭제될때 발동 이벤트
    public event Action<int, int> OnWaveIndexChanged;

    public static StageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<StageManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("StageManager");
                    _instance = singleton.AddComponent<StageManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void InitializeManager(Tilemap map, Transform grid)
    {
        CleanUp();
        placedMap = map;
        tileMapGrid = grid;
    }

    public void StartStage(int stageNumber)
    {

        // episodeNumber도 함께 고려하여 스테이지 찾기
        D_StageData stageData = D_StageData.FindEntity(data => data.f_EpisodeData.f_episodeNumber == GameManager.Instance.SelectedEpisodeNumber && data.f_StageNumber == stageNumber);

        currentStage = stageData;
        currentWaveIndex = 0;

        //타일맵 매니저 초기화, 타일맵 data 전달
        TileMapManager.Instance.InitializeManager(placedMap, stageData.f_mapData, tileMapGrid);

        //pathFindingManager의 시작타일과 끝타일도 초기화 해줘야됨
        TileMapManager.Instance.InitializeTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);

        // 웨이브 데이터 초기화
        InitializeWaves();

        // 첫 번째 웨이브 시작
        StartNextWave();
    }

    private void InitializeWaves()
    {
        waveList.Clear();

        // WaveDummyData 데이터로부터 웨이브 객체 생성
        foreach (D_WaveDummyData waveData in currentStage.f_WaveDummyData)
        {
            WaveBase wave = CreateWaveFromData(waveData);
            if (wave != null)
            {
                waveList.Add(wave);
            }
        }
    }
    private WaveBase CreateWaveFromData(D_WaveDummyData waveData)
    {
        //웨이브 Data 타입에 따라서 각각의 클래스 생성하기
        if (waveData is D_NormalBattleWaveData normalData)
        {
            return new NormalBattleWave(normalData);
        }
        else if (waveData is D_BossBattleWaveData bossData)
        {
            return new BossBattleWave(bossData);
        }
        else if( waveData is D_EventEnemyWaveData eventData)
        {
            return new EventEnemyWave(eventData);
        }
        else if( waveData is D_WildCardWaveData wildCardData)
        {
            return new WildcardWave(wildCardData);
        }
        else if( waveData is D_HuntingSelectTimeWaveData huntingSelectData)
        {
            return new HuntingSelectTimeWave(huntingSelectData);
        }
        // 다른 웨이브 타입들도 필요에 따라 추가...

        Debug.LogError($"알 수 없는 웨이브 타입: {waveData.GetType().Name}");
        return null;
    }
    public void SetNextWave(WaveBase nextWave)
    {
        nextAssignWave = nextWave;
        isAssignWave = true;
    }

    public void StartNextWave()
    {

        // StageData에 들어가진 않고 앞의 웨이브에 영향받는 웨이브만
        if (nextAssignWave != null)
        {
            currentWave = nextAssignWave;
            nextAssignWave = null;
            isAssignWave = false;
            // 나머지 코드는 동일하게 실행
            OnWaveIndexChanged?.Invoke(currentWaveIndex + 1, waveList.Count);
            OnWaveStart?.Invoke();
            ShowWaveInfo();
            return;
        }

        if (currentWaveIndex >= waveList.Count)
        {
            Debug.Log("모든 웨이브 완료!");
            OnGameClear();
            return;
        }

        currentWave = waveList[currentWaveIndex];

        // 웨이브 인덱스 변경 알림
        OnWaveIndexChanged?.Invoke(currentWaveIndex + 1, waveList.Count);

        // 웨이브 시작 이벤트 발생
        OnWaveStart?.Invoke();

        // 웨이브 정보 표시
        ShowWaveInfo();
    }

    private async void ShowWaveInfo()
    {
        waveInfoUI = await UIManager.Instance.ShowUI<WaveInfoFloatingUI>();
        countdownUI = await UIManager.Instance.ShowUI<InGameCountdownUI>(); // 웨이브 시작 전 남은시간 보여주는 ui

        string waveText = $"Wave {currentWaveIndex + 1} Start!";
        string enemyInfo = currentWave.GetWaveInfoText(); // 웨이브 클래스에서 적 정보 가져오기
        waveInfoUI.UpdateWaveInfo(waveText, enemyInfo);

        // 웨이브 정보 표시 스케줄 등록
        currentWaveInfoScheduleUID = TimeTableManager.Instance.RegisterSchedule(waveInfoDuration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentWaveInfoScheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentWaveInfoScheduleUID); 
    }

    // 웨이브 완료 처리 - 웨이브에서 호출됨
    public void OnWaveComplete()
    {
        // 현재 웨이브 종료 처리
        currentWave.EndWave();

        // 웨이브 종료 이벤트 발생
        OnWaveFinish?.Invoke();

        // 마지막 웨이브였는지 확인
        if (IsLastWave())
        {
            OnGameClear();
        }
        else
        {
            // 웨이브 완료 UI 표시
            ShowWaveFinishUI();
        }
    }

    // 적 수 변경 처리 - UI 업데이트와 이벤트 발생만 담당
    public void UpdateEnemyCount(int count)
    {      
        // UI 업데이트를 위한 이벤트 발생 - 총 남은 적 수 전달
        OnEnemyCountChanged?.Invoke(count);
    }


    private async void ShowWaveFinishUI()
    {
        // 웨이브 완료 UI 표시
        waveFinishUI = await UIManager.Instance.ShowUI<WaveFinishFloatingUI>();
        string waveFinishText = $"Wave {currentWaveIndex + 1} Finish!";
        waveFinishUI.UpdateWaveInfo(waveFinishText);

        // FadeOut 완료 대기
        await waveFinishUI.WaitForFadeOut();
        UIManager.Instance.CloseUI<WaveFinishFloatingUI>();
        waveFinishUI = null;

        if(!isAssignWave)
        {
            currentWaveIndex++;
        }

        CleanUpBeforeNextWave();

        StartNextWave();
    }
    //웨이브 간 오브젝트 정리
    private void CleanUpBeforeNextWave()
    {
        if(currentWave is BattleWaveBase )
        {

            var units = UnitManager.Instance.GetAllUnits();
            foreach (var unit in units)
            {
                unit.fsmObj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.ReturnToOriginPos);

                // 타일맵에 있던 원래 위치로 돌아가기
                unit.ReturnToOriginalPosition();
            }

            // 적 오브젝트 비활성화
            var enemies = EnemyManager.Instance.GetAllEnemys();
            foreach (var enemy in enemies)
            {
                enemy.SetActive(false);
            }

            EnemyManager.Instance.CleanUp();    


            // 카메라 원위치로 이동
            CameraController cameraController = GameManager.Instance.mainCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.OnBattleEnd();
            }




        }
        //전투 웨이브인 경우에는 살아남은 유닛들은 원래 있던 자리로 돌아가야됨

        // 진행 중인 모든 투사체 제거
        ProjectileManager.Instance.CleanUp();

        // 진행중인 모든 스킬 제거
        AttackSkillManager.Instance.CleanUp();

    }

    public void SetTotalEnemyCount(int count)
    {
        // 총 남은 적 수 직접 설정
        totalRemainingEnemies = count;

        // UI 업데이트를 위한 이벤트 발생
        OnEnemyCountChanged?.Invoke(totalRemainingEnemies);
    }

    public bool IsLastWave()
    {
        return currentWaveIndex + 1 >= currentStage.f_WaveDummyData.Count;  // 다음 웨이브가 마지막인지 미리 체크
    }
   
    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID == currentWaveInfoScheduleUID)
        {
            if (countdownUI != null)
            {
                countdownUI.UpdateCountdown(remainTime);
            }
        }
    }
  
    public void OnCompleteSchedule(int scheduleUID)
    {
        // 웨이브 소개 UI 표시 스케줄 완료
        if (scheduleUID == currentWaveInfoScheduleUID)
        {
            // 웨이브 정보 표시 시간 종료
            UIManager.Instance.CloseUI<WaveInfoFloatingUI>();

            UIManager.Instance.CloseUI<InGameCountdownUI>();
            currentWaveInfoScheduleUID = -1;

            // 실제 웨이브 시작
            if (currentWave != null)
            {
                currentWave.StartWave();
            }
            return;
        }
    }

    private void OnGameClear()
    {
        var userData = D_LocalUserData.GetEntity(0);
        userData.f_lastClearedStageNumber = Mathf.Max(userData.f_lastClearedStageNumber, currentStage.f_StageNumber);
        SaveLoadManager.Instance.SaveData();

        GameManager.Instance.ChangeState(new GameResultState(GameStateType.Victory));
    }

    public WaveBase GetCurrentWave()
    {
        return currentWave;
    }

    public void CleanUp()
    {

        if (currentWaveInfoScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentWaveInfoScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentWaveInfoScheduleUID);
        }

        if (countdownUI != null)
        {
            UIManager.Instance.CloseUI<InGameCountdownUI>();
            countdownUI = null;
        }

        if (waveFinishUI != null)
        {
            UIManager.Instance.CloseUI<WaveFinishFloatingUI>();
            waveFinishUI = null;
        }

        if (waveInfoUI != null)
        {
            UIManager.Instance.CloseUI<WaveInfoFloatingUI>();
            waveInfoUI = null; // 참조 초기화
        }

        StopAllCoroutines();


        // 현재 웨이브 정리
        if (currentWave != null)
        {
            currentWave.EndWave();
            currentWave = null;
        }

        // 웨이브 리스트 정리
        waveList.Clear();

        currentStage = null;
        currentWaveIndex = 0;
        currentWaveInfoScheduleUID = -1;
        placedMap = null;
        tileMapGrid = null;
    }
}