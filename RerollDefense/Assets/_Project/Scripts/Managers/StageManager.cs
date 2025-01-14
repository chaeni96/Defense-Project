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

        hasSelectedWildCard = false;

        // ���̺� �ð� ����
        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];

        foreach (var timeData in waveData.f_WaveTimeData)
        {
            if (timeData.f_StatName == StatName.WaveTime)
            {
                waveTime = timeData.f_StatValue;
                break;
            }
        }

        // ���̺� ���� ������ ���
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
        //������ �ð� ����ɶ����� ȣ���ϴ� �ݹ� �޼���
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
            // ���̺� ����
            currentWaveScheduleUID = -1;
            StartRestPhase();
        }
        else if (scheduleUID == currentRestScheduleUID)
        {
            // �޽� �ð� ����
            
            // ���ϵ�ī�带 �������� �ʾҴٸ� UI �ݱ�
            if (!hasSelectedWildCard)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();
            }
            currentRestScheduleUID = -1;
            currentWaveIndex++;
            StartNextWave();
        }
    }

    //�޽� ������ ���� -> ���̺� ���� �޽� �ð�, ���ϵ� ī�� ����UI ǥ��
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
        return currentWaveIndex >= currentStage.f_WaveData.Count;
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        // null �� ��ȿ�� üũ �߰�
        if (enemyGroupData == null || enemyGroupData.f_enemy == null)
        {
            Debug.LogError("�� �׷� �����Ͱ� ��ȿ���� �ʽ��ϴ�.");
            yield break;
        }

        // ��ŸƮ ������ ���
        yield return new WaitForSeconds(enemyGroupData.f_startDelay);
        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            // null üũ �� ������ ����
            if (enemyGroupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy.f_ObjectPoolKey.f_PoolObjectAddressableKey);
            }
            else
            {
                Debug.LogError("������Ʈ Ǯ Ű�� null�Դϴ�.");
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
