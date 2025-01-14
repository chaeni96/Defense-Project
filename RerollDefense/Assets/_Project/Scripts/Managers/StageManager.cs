using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using static BansheeGz.BGDatabase.BGSyncNameMapConfig;

public class StageManager : MonoBehaviour, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{

    public static StageManager _instance;


    private Tilemap placedMap;
    private Transform tileMapGrid;


    private D_StageData currentStage;
    private int currentWaveIndex = 0;
    private int currentWaveScheduleUID = -1;
    private int currentRestScheduleUID = -1;
    private bool hasSelectedWildCard = false;

    private float waveTime = 30f;
    private float restTime = 20f;
    private float minRestTime = 5f;
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

        hasSelectedWildCard = false;

        // 웨이브 시간 설정
        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];

        foreach (var timeData in waveData.f_WaveTimeData)
        {
            if (timeData.f_StatName == StatName.WaveTime)
            {
                waveTime = timeData.f_StatValue;
                break;
            }
        }

        // 웨이브 시작 스케줄 등록
        currentWaveScheduleUID = TimeTableManager.Instance.RegisterSchedule(waveTime);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentWaveScheduleUID);
        TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, currentWaveScheduleUID);

        SpawnWaveEnemies(waveData);
    }


    private void SpawnWaveEnemies(D_WaveData waveData)
    {
        foreach (D_enemyGroup groupData in waveData.f_enemyGroup)
        {
            StartCoroutine(CoSpawnEnemyGroup(groupData));
        }
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        //스케줄 시간 변경될때마다 호출하는 콜백 메서드
        if (scheduleUID == currentWaveScheduleUID)
        {
        }
        else if (scheduleUID == currentRestScheduleUID)
        {
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID == currentWaveScheduleUID)
        {
            // 웨이브 종료
            currentWaveScheduleUID = -1;
            StartRestPhase();
        }
        else if (scheduleUID == currentRestScheduleUID)
        {
            // 휴식 시간 종료
            
            // 와일드카드를 선택하지 않았다면 UI 닫기
            if (!hasSelectedWildCard)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();
            }
            currentRestScheduleUID = -1;
            currentWaveIndex++;
            StartNextWave();
        }
    }

    //휴식 페이즈 시작 -> 웨이브 사이 휴식 시간, 와일드 카드 선택UI 표시
    private async void StartRestPhase()
    {
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


        var wildCardUI = await UIManager.Instance.ShowUI<WildCardSelectUI>();
        wildCardUI.SetWildCardDeck();
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

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        // null 및 유효성 체크 추가
        if (enemyGroupData == null || enemyGroupData.f_enemy == null)
        {
            Debug.LogError("적 그룹 데이터가 유효하지 않습니다.");
            yield break;
        }

        // 스타트 딜레이 대기
        yield return new WaitForSeconds(enemyGroupData.f_startDelay);
        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            // null 체크 및 안전한 스폰
            if (enemyGroupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);
            }
            else
            {
                Debug.LogError("오브젝트 풀 키가 null입니다.");
                break;
            }

            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }
    }


    private void CleanUp()
    {
        if (currentWaveScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentWaveScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentWaveScheduleUID);
        }

        if (currentRestScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentRestScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(currentRestScheduleUID);
        }

        StopAllCoroutines();
        currentStage = null;
        currentWaveIndex = 0;
        currentWaveScheduleUID = -1;
        currentRestScheduleUID = -1;
        hasSelectedWildCard = false;
        placedMap = null;
        tileMapGrid = null;
    }

}
