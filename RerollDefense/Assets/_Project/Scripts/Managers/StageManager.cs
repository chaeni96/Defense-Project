using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class StageManager : MonoBehaviour
{

    public static StageManager _instance;

    private D_StageData currentStage;
    private int currentWaveIndex = 0;
    private Coroutine waveCoroutine;

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
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }


    public void StartStage(int stageNumber)
    {

        D_StageData stageData = D_StageData.FindEntity(data => data.f_StageNumber == stageNumber);

        currentStage = stageData;
        currentWaveIndex = 0;

        //pathFindingManager의 시작타일과 끝타일도 초기화 해줘야됨
        PathFindingManager.Instance.InitializePathTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentStage.f_WaveData.Count)
        {
            Debug.Log("마지막 웨이브!");
            return;
        }

        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];
        waveCoroutine = StartCoroutine(CoProcessWave(waveData));
    }
    private IEnumerator CoProcessWave(D_WaveData waveData)
    {
        var spawnRoutines = new List<Coroutine>();

        foreach (D_enemyGroup groupData in waveData.f_enemyGroup)
        {
            spawnRoutines.Add(StartCoroutine(CoSpawnEnemyGroup(groupData)));
        }
        foreach (var routine in spawnRoutines)
        {
            yield return routine;
        }

        // 웨이브 완료 후 대기
        yield return new WaitForSeconds(currentStage.f_WaveDelayTime);

        // 다음 웨이브 시작
        currentWaveIndex++;
        StartNextWave();
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        // 스타트 딜레이 대기
        yield return new WaitForSeconds(enemyGroupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy.f_ObjectPoolKey.f_name);
            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }
    }
     public bool IsLastWave()
    {
        return currentWaveIndex >= currentStage.f_WaveData.Count;
    }

}
