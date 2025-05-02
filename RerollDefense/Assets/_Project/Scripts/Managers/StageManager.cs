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

    //���̺� ���� ����â
    private int currentWaveInfoScheduleUID = -1;
    private const float waveInfoDuration = 1.6f;

    //private WildCardSelectUI selectUI;
    private WaveInfoFloatingUI waveInfoUI;
    private InGameCountdownUI countdownUI;
    private WaveFinishFloatingUI waveFinishUI;

    //���̺� ��� ����
    private List<WaveBase> waveList = new List<WaveBase>();
    private WaveBase currentWave = null;

    private int totalRemainingEnemies = 0;

    private bool isAssignWave = false;

    // ���̺� ����/���� ���� �̺�Ʈ
    public event Action OnWaveStart;  // ���̺� ����� �߻��ϴ� �̺�Ʈ
    public event Action OnWaveFinish;  // ���̺� ����� �߻��ϴ� �̺�Ʈ

    public event Action<int> OnEnemyCountChanged; //enemy ���� �Ǵ� �����ɶ� �ߵ� �̺�Ʈ
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

        // episodeNumber�� �Բ� ����Ͽ� �������� ã��
        D_StageData stageData = D_StageData.FindEntity(data => data.f_EpisodeData.f_episodeNumber == GameManager.Instance.SelectedEpisodeNumber && data.f_StageNumber == stageNumber);

        currentStage = stageData;
        currentWaveIndex = 0;

        //Ÿ�ϸ� �Ŵ��� �ʱ�ȭ, Ÿ�ϸ� data ����
        TileMapManager.Instance.InitializeManager(placedMap, stageData.f_mapData, tileMapGrid);

        //pathFindingManager�� ����Ÿ�ϰ� ��Ÿ�ϵ� �ʱ�ȭ ����ߵ�
        TileMapManager.Instance.InitializeTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);

        // ���̺� ������ �ʱ�ȭ
        InitializeWaves();

        // ù ��° ���̺� ����
        StartNextWave();
    }

    private void InitializeWaves()
    {
        waveList.Clear();

        // WaveDummyData �����ͷκ��� ���̺� ��ü ����
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
        //���̺� Data Ÿ�Կ� ���� ������ Ŭ���� �����ϱ�
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
        // �ٸ� ���̺� Ÿ�Ե鵵 �ʿ信 ���� �߰�...

        Debug.LogError($"�� �� ���� ���̺� Ÿ��: {waveData.GetType().Name}");
        return null;
    }
    public void SetNextWave(WaveBase nextWave)
    {
        nextAssignWave = nextWave;
        isAssignWave = true;
    }

    public void StartNextWave()
    {

        // StageData�� ���� �ʰ� ���� ���̺꿡 ����޴� ���̺길
        if (nextAssignWave != null)
        {
            currentWave = nextAssignWave;
            nextAssignWave = null;
            isAssignWave = false;
            // ������ �ڵ�� �����ϰ� ����
            OnWaveIndexChanged?.Invoke(currentWaveIndex + 1, waveList.Count);
            OnWaveStart?.Invoke();
            ShowWaveInfo();
            return;
        }

        if (currentWaveIndex >= waveList.Count)
        {
            Debug.Log("��� ���̺� �Ϸ�!");
            OnGameClear();
            return;
        }

        currentWave = waveList[currentWaveIndex];

        // ���̺� �ε��� ���� �˸�
        OnWaveIndexChanged?.Invoke(currentWaveIndex + 1, waveList.Count);

        // ���̺� ���� �̺�Ʈ �߻�
        OnWaveStart?.Invoke();

        // ���̺� ���� ǥ��
        ShowWaveInfo();
    }

    private async void ShowWaveInfo()
    {
        waveInfoUI = await UIManager.Instance.ShowUI<WaveInfoFloatingUI>();
        countdownUI = await UIManager.Instance.ShowUI<InGameCountdownUI>(); // ���̺� ���� �� �����ð� �����ִ� ui

        string waveText = $"Wave {currentWaveIndex + 1} Start!";
        string enemyInfo = currentWave.GetWaveInfoText(); // ���̺� Ŭ�������� �� ���� ��������
        waveInfoUI.UpdateWaveInfo(waveText, enemyInfo);

        // ���̺� ���� ǥ�� ������ ���
        currentWaveInfoScheduleUID = TimeTableManager.Instance.RegisterSchedule(waveInfoDuration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentWaveInfoScheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentWaveInfoScheduleUID); 
    }

    // ���̺� �Ϸ� ó�� - ���̺꿡�� ȣ���
    public void OnWaveComplete()
    {
        // ���� ���̺� ���� ó��
        currentWave.EndWave();

        // ���̺� ���� �̺�Ʈ �߻�
        OnWaveFinish?.Invoke();

        // ������ ���̺꿴���� Ȯ��
        if (IsLastWave())
        {
            OnGameClear();
        }
        else
        {
            // ���̺� �Ϸ� UI ǥ��
            ShowWaveFinishUI();
        }
    }

    // �� �� ���� ó�� - UI ������Ʈ�� �̺�Ʈ �߻��� ���
    public void UpdateEnemyCount(int count)
    {      
        // UI ������Ʈ�� ���� �̺�Ʈ �߻� - �� ���� �� �� ����
        OnEnemyCountChanged?.Invoke(count);
    }


    private async void ShowWaveFinishUI()
    {
        // ���̺� �Ϸ� UI ǥ��
        waveFinishUI = await UIManager.Instance.ShowUI<WaveFinishFloatingUI>();
        string waveFinishText = $"Wave {currentWaveIndex + 1} Finish!";
        waveFinishUI.UpdateWaveInfo(waveFinishText);

        // FadeOut �Ϸ� ���
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
    //���̺� �� ������Ʈ ����
    private void CleanUpBeforeNextWave()
    {
        if(currentWave is BattleWaveBase )
        {

            var units = UnitManager.Instance.GetAllUnits();
            foreach (var unit in units)
            {
                unit.fsmObj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.ReturnToOriginPos);

                // Ÿ�ϸʿ� �ִ� ���� ��ġ�� ���ư���
                unit.ReturnToOriginalPosition();
            }

            // �� ������Ʈ ��Ȱ��ȭ
            var enemies = EnemyManager.Instance.GetAllEnemys();
            foreach (var enemy in enemies)
            {
                enemy.SetActive(false);
            }

            EnemyManager.Instance.CleanUp();    


            // ī�޶� ����ġ�� �̵�
            CameraController cameraController = GameManager.Instance.mainCamera.GetComponent<CameraController>();
            if (cameraController != null)
            {
                cameraController.OnBattleEnd();
            }




        }
        //���� ���̺��� ��쿡�� ��Ƴ��� ���ֵ��� ���� �ִ� �ڸ��� ���ư��ߵ�

        // ���� ���� ��� ����ü ����
        ProjectileManager.Instance.CleanUp();

        // �������� ��� ��ų ����
        AttackSkillManager.Instance.CleanUp();

    }

    public void SetTotalEnemyCount(int count)
    {
        // �� ���� �� �� ���� ����
        totalRemainingEnemies = count;

        // UI ������Ʈ�� ���� �̺�Ʈ �߻�
        OnEnemyCountChanged?.Invoke(totalRemainingEnemies);
    }

    public bool IsLastWave()
    {
        return currentWaveIndex + 1 >= currentStage.f_WaveDummyData.Count;  // ���� ���̺갡 ���������� �̸� üũ
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
        // ���̺� �Ұ� UI ǥ�� ������ �Ϸ�
        if (scheduleUID == currentWaveInfoScheduleUID)
        {
            // ���̺� ���� ǥ�� �ð� ����
            UIManager.Instance.CloseUI<WaveInfoFloatingUI>();

            UIManager.Instance.CloseUI<InGameCountdownUI>();
            currentWaveInfoScheduleUID = -1;

            // ���� ���̺� ����
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
            waveInfoUI = null; // ���� �ʱ�ȭ
        }

        StopAllCoroutines();


        // ���� ���̺� ����
        if (currentWave != null)
        {
            currentWave.EndWave();
            currentWave = null;
        }

        // ���̺� ����Ʈ ����
        waveList.Clear();

        currentStage = null;
        currentWaveIndex = 0;
        currentWaveInfoScheduleUID = -1;
        placedMap = null;
        tileMapGrid = null;
    }
}