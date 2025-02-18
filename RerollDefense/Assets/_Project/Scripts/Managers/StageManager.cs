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

    private Tilemap placedMap;
    private Transform tileMapGrid;

    private D_StageData currentStage;
    private int currentWaveIndex = 0;
    private bool hasSelectedWildCard = false;

    private bool isSpawnDone = false; //���� ���̺��� ������� �����Ϸ�ƴ���
    private int totalGroupCount = 0; // ���� ���� ���� ���ʹ� �׷� ��
    private int completedGroupCount = 0; // ���� �Ϸ�� ���ʹ� �׷� ��
    private int remainEnemies = 0; //���� ���̺꿡�� ����ִ� ���ʹ� ��

    //���ϵ� ī�� ���� : restTime = ���ϵ�ī�� ���ýð� -
    private float restTime;
    private float minRestTime;
    private int currentRestScheduleUID = -1;

    //���̺� ���� ����â
    private int currentWaveInfoScheduleUID = -1;
    private const float waveInfoDuration = 5f;

    private WildCardSelectUI selectUI;
    private WaveInfoFloatingUI waveInfoUI;
    private InGameCountdownUI countdownUI;


    private WaveFinishFloatingUI waveFinishUI;

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
        D_StageData stageData = D_StageData.FindEntity(data => data.f_StageNumber == stageNumber);

        currentStage = stageData;
        currentWaveIndex = 0;

        //Ÿ�ϸ� �Ŵ��� �ʱ�ȭ, Ÿ�ϸ� data ����
        TileMapManager.Instance.InitializeManager(placedMap, stageData.f_mapData, tileMapGrid);

        //pathFindingManager�� ����Ÿ�ϰ� ��Ÿ�ϵ� �ʱ�ȭ ����ߵ�
        TileMapManager.Instance.InitializeTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);

        GameManager.Instance.InitializePlayerCamp(stageData.f_EndTilePos);

        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentStage.f_WaveData.Count)
        {
            Debug.Log("��� ���̺� �Ϸ�!");
            return;
        }
        isSpawnDone = false;

        // ���� ���̺��� �� �� �� ���
        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];
        remainEnemies = waveData.f_enemyGroup.Sum(group => group.f_amount);

   
        OnWaveStart?.Invoke();

        ShowWaveInfo(waveData);
    }

    private async void ShowWaveInfo(D_WaveData waveData)
    {
        waveInfoUI = await UIManager.Instance.ShowUI<WaveInfoFloatingUI>();
        countdownUI = await UIManager.Instance.ShowUI<InGameCountdownUI>(); // ���̺� ���� �� �����ð� �����ִ� ui

        string waveText = $"Wave {currentWaveIndex + 1} Start!";
        string enemyInfo = "";

        //LINQ -> GroupBy ����ؼ� ���� �� Ÿ�Գ��� �׷�ȭ�ؼ� �����ֱ�
        var groupedEnemies = waveData.f_enemyGroup
                            .GroupBy(g => g.f_enemy.f_name)
                            .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

        foreach (var group in groupedEnemies)
        {
            enemyInfo += $"{group.Name} x{group.TotalAmount}\n";
        }

        // ���̺� �ε��� ���� �˸�
        OnWaveIndexChanged?.Invoke(currentWaveIndex + 1, currentStage.f_WaveData.Count);
        OnEnemyCountChanged?.Invoke(remainEnemies);

        waveInfoUI.UpdateWaveInfo(waveText, enemyInfo);

        currentWaveInfoScheduleUID = TimeTableManager.Instance.RegisterSchedule(waveInfoDuration);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentWaveInfoScheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentWaveInfoScheduleUID); 
    }

    private void SpawnWaveEnemies(D_WaveData waveData)
    {

        totalGroupCount = waveData.f_enemyGroup.Count; //�� �����Ǿ���ϴ� ���ʹ� �׷� ��

        completedGroupCount = 0;

        foreach (D_enemyGroup groupData in waveData.f_enemyGroup)
        {
            StartCoroutine(CoSpawnEnemyGroup(groupData));
        }
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        if (enemyGroupData == null || enemyGroupData.f_enemy == null)
        {
            Debug.LogError("�� �׷� �����Ͱ� ��ȿ���� �ʽ��ϴ�.");
            yield break;
        }

        yield return new WaitForSeconds(enemyGroupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            if (enemyGroupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy);
            }
            else
            {
                Debug.LogError("������Ʈ Ǯ Ű�� null�Դϴ�.");
                break;
            }

            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }

        ++completedGroupCount;

        // ��� �׷��� ������ �Ϸ�Ǿ����� üũ
        if (completedGroupCount >= totalGroupCount)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }
    public void AddRemainingEnemies(int count)
    {
        remainEnemies += count;
        OnEnemyCountChanged?.Invoke(remainEnemies);
    }

    public void DecreaseEnemyCount()
    {
        --remainEnemies;
        OnEnemyCountChanged?.Invoke(remainEnemies);  // �̺�Ʈ �߻�
        CheckWaveCompletion();
    }

    private void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0)
        {
            if (IsLastWave())
            {
                OnGameClear();
            }
            else
            {
                ShowWaveFinishUI();
            }
        }
    }

    private void OnGameClear()
    {
        var userData = D_LocalUserData.GetEntity(0);
        userData.f_lastClearedStageNumber = Mathf.Max(userData.f_lastClearedStageNumber, currentStage.f_StageNumber);
        SaveLoadManager.Instance.SaveData();

        GameManager.Instance.ChangeState(new GameResultState(GameStateType.Victory));
    }

    private async void ShowWaveFinishUI()
    {
        isSpawnDone = false;

        // ���̺� �Ϸ� UI ǥ��
        waveFinishUI = await UIManager.Instance.ShowUI<WaveFinishFloatingUI>();
        string waveFinishText = $"Wave {currentWaveIndex + 1} Finish!";
        waveFinishUI.UpdateWaveInfo(waveFinishText);

        // FadeOut �Ϸ� ���
        await waveFinishUI.WaitForFadeOut();

        // UI �ݰ� ���ϵ� ī�� ����Ÿ������
        UIManager.Instance.CloseUI<WaveFinishFloatingUI>();

        StartRestPhase();
    }

    private async void StartRestPhase()
    {
        isSpawnDone = false;
        hasSelectedWildCard = false;
        // ���̺� ���� �̺�Ʈ �߻�
        OnWaveFinish?.Invoke();

        D_WaveData currentWaveData = currentStage.f_WaveData[currentWaveIndex];

        foreach (var timeData in currentWaveData.f_WaveTimeData)
        {
            if (timeData.f_StatName == StatName.WaveRestTime)
            {
                restTime = timeData.f_StatValue;
                break;
            }
        }

        currentRestScheduleUID = TimeTableManager.Instance.RegisterSchedule(restTime);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentRestScheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentRestScheduleUID);

        selectUI = await UIManager.Instance.ShowUI<WildCardSelectUI>();
        selectUI.SetWildCardDeck();
    }

    public void OnWildCardSelected()
    {
        hasSelectedWildCard = true;

        float remainingTime = GetCurrentRestScheduleRemainingTime();

        D_WaveData currentWaveData = currentStage.f_WaveData[currentWaveIndex];

        foreach (var timeData in currentWaveData.f_WaveTimeData)
        {
            if (timeData.f_StatName == StatName.WaveMinRestTime)
            {
                minRestTime = timeData.f_StatValue;
                break;
            }
        }

        if (remainingTime > minRestTime)
        {
            // ���� rest ������ ����ϰ� ���ο� �ּ� �ð� ������ ����
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentRestScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentRestScheduleUID);

            currentRestScheduleUID = TimeTableManager.Instance.RegisterSchedule(minRestTime);
            TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentRestScheduleUID);
            TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentRestScheduleUID);
        }
    }

    private float GetCurrentRestScheduleRemainingTime()
    {
        var schedule = TimeTableManager.Instance.GetSchedule(currentRestScheduleUID);
        if (schedule != null)
        {
            return (float)((schedule.endTime - schedule.currentTime) / 100.0);
        }
        return 0f;
    }

    public bool IsLastWave()
    {
        return currentWaveIndex + 1 >= currentStage.f_WaveData.Count;  // ���� ���̺갡 ���������� �̸� üũ
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID == currentRestScheduleUID)
        {
            // �޽� �ð� ���� �� �ʿ��� ó��
            if(selectUI != null)
            {
                selectUI.UpdateSelectTime(Mathf.CeilToInt(remainTime));
            }

            if (countdownUI != null)
            {
                countdownUI.UpdateCountdown(remainTime);
            }
        }
        else if (scheduleUID == currentWaveInfoScheduleUID)
        {
            if (countdownUI != null)
            {
                countdownUI.UpdateCountdown(remainTime);
            }
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID == currentWaveInfoScheduleUID)
        {
            UIManager.Instance.CloseUI<WaveInfoFloatingUI>();
            UIManager.Instance.CloseUI<InGameCountdownUI>();
            currentWaveInfoScheduleUID = -1;
            SpawnWaveEnemies(currentStage.f_WaveData[currentWaveIndex]);
        }
        else if (scheduleUID == currentRestScheduleUID)
        {
            UIManager.Instance.CloseUI<InGameCountdownUI>();

            // ���ϵ�ī�带 �������� �ʾҴٸ� �ڵ����� ó��
            if (!hasSelectedWildCard)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();

                // ���ϵ�ī�� ���� ���� ���� ���̺� ������ ���Ѵٸ�
                currentRestScheduleUID = -1;
                currentWaveIndex++;
                StartNextWave();
            }
            else
            {
                // ���ϵ�ī�带 ������ ���
                currentRestScheduleUID = -1;
                currentWaveIndex++;
                StartNextWave();
            }
        }
    }

    public void CleanUp()
    {

        if (currentWaveInfoScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentWaveInfoScheduleUID);
        }

        if (currentRestScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentRestScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentRestScheduleUID);
        }


        if (countdownUI != null)
        {
            UIManager.Instance.CloseUI<InGameCountdownUI>();
        }

        if (waveFinishUI != null)
        {
            UIManager.Instance.CloseUI<WaveFinishFloatingUI>();
        }

        StopAllCoroutines();
        currentStage = null;
        currentWaveIndex = 0;
        currentRestScheduleUID = -1;
        hasSelectedWildCard = false;
        placedMap = null;
        tileMapGrid = null;

        isSpawnDone = false;
        remainEnemies = 0;
    }
}