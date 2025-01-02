using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;
using static BansheeGz.BGDatabase.BGSyncNameMapConfig;

public class StageManager : MonoBehaviour
{

    public static StageManager _instance;


    private Tilemap placedMap;
    private Transform tileMapGrid;


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

    public void Initialize(Tilemap map, Transform grid)
    {
        placedMap = map;
        tileMapGrid = grid;
    }


    public void StartStage(int stageNumber)
    {

        D_StageData stageData = D_StageData.FindEntity(data => data.f_StageNumber == stageNumber);

        currentStage = stageData;
        currentWaveIndex = 0;


        //TileMapData에서 해당 스테이지의 장애물 맵 프리팹 키 가져오기

        D_TileMapData tileMapData = D_TileMapData.GetEntityByKeyStageID(stageNumber);

        if (tileMapData != null)
        {

            // 어드레서블에서 장애물 맵 프리팹 로드
            GameObject obstacleMapPrefab = ResourceManager.Instance.Instantiate(tileMapData.f_ObstacleAddressableKey, tileMapGrid);

            if (obstacleMapPrefab != null)
            {
                // 프리팹에서 Tilemap 컴포넌트 가져오기
                Tilemap obstacleMap = obstacleMapPrefab.GetComponent<Tilemap>();

                // 타일맵 매니저 초기화하면서 장애물 정보 전달
                TileMapManager.Instance.InitializeManager(placedMap, obstacleMap);
            }


            //pathFindingManager의 시작타일과 끝타일도 초기화 해줘야됨
            PathFindingManager.Instance.InitializePathTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);

            StartNextWave();
        }
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
        yield return new WaitForSeconds(waveData.f_SpawnDelay);

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
