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

    private bool isSpawnDone = false; //현재 웨이브의 모든적이 스폰완료됐는지
    private int totalSpawnCount = 0; //현재 웨이브에서 스폰된 에너미 총 수 
    private int remainEnemies = 0; //현재 웨이브에서 살아있는 에너미 수

    private float restTime = 20f;
    private float minRestTime = 5f;
    private int currentRestScheduleUID = -1;

    private WildCardSelectUI selectUI;
    
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

        //타일맵 매니저 초기화, 타일맵 data 전달
        TileMapManager.Instance.InitializeManager(placedMap, stageData.f_mapData, tileMapGrid);

        //pathFindingManager의 시작타일과 끝타일도 초기화 해줘야됨
        TileMapManager.Instance.InitializeTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);

        GameManager.Instance.InitializePlayerCamp(stageData.f_EndTilePos);

        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentStage.f_WaveData.Count)
        {
            Debug.Log("모든 웨이브 완료!");
            return;
        }

        isSpawnDone = false;
        totalSpawnCount = 0;

        // 현재 웨이브의 총 적 수 계산
        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];
        remainEnemies = waveData.f_enemyGroup.Sum(group => group.f_amount);

        SpawnWaveEnemies(waveData);
    }

    private void SpawnWaveEnemies(D_WaveData waveData)
    {
        foreach (D_enemyGroup groupData in waveData.f_enemyGroup)
        {
            StartCoroutine(CoSpawnEnemyGroup(groupData));
        }
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        if (enemyGroupData == null || enemyGroupData.f_enemy == null)
        {
            Debug.LogError("적 그룹 데이터가 유효하지 않습니다.");
            yield break;
        }

        yield return new WaitForSeconds(enemyGroupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            if (enemyGroupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy);
                totalSpawnCount++;
            }
            else
            {
                Debug.LogError("오브젝트 풀 키가 null입니다.");
                break;
            }

            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }

        // 모든 그룹의 스폰이 완료되었는지 체크
        if (totalSpawnCount >= remainEnemies)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }
    public void AddRemainingEnemies(int count)
    {
        remainEnemies += count;
    }

    public void DecreaseEnemyCount()
    {
        --remainEnemies;
        CheckWaveCompletion();
    }

    private void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0)
        {
            StartRestPhase();
        }
    }


    private async void StartRestPhase()
    {
        isSpawnDone = false;

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
            // 현재 rest 스케줄 취소하고 새로운 최소 시간 스케줄 시작
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
        return currentWaveIndex >= currentStage.f_WaveData.Count;
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID == currentRestScheduleUID)
        {
            // 휴식 시간 변경 시 필요한 처리
            if(selectUI != null)
            {
                selectUI.UpdateSelectTime(Mathf.CeilToInt(remainTime));
            }
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID == currentRestScheduleUID)
        {
            // 와일드카드를 선택하지 않았다면 자동으로 처리
            if (!hasSelectedWildCard)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();

                // 와일드카드 선택 없이 다음 웨이브 진행을 원한다면
                currentRestScheduleUID = -1;
                currentWaveIndex++;
                StartNextWave();
            }
            else
            {
                // 와일드카드를 선택한 경우
                currentRestScheduleUID = -1;
                currentWaveIndex++;
                StartNextWave();
            }
        }
    }

    private void CleanUp()
    {
        if (currentRestScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentRestScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentRestScheduleUID);
        }

        StopAllCoroutines();
        currentStage = null;
        currentWaveIndex = 0;
        currentRestScheduleUID = -1;
        hasSelectedWildCard = false;
        placedMap = null;
        tileMapGrid = null;

        isSpawnDone = false;
        totalSpawnCount = 0;
        remainEnemies = 0;
    }
}