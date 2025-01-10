using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.Universal;
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
     public bool IsLastWave()
    {
        return currentWaveIndex >= currentStage.f_WaveData.Count;
    }



    private void CleanUp()
    {
        // 실행 중인 코루틴이 있다면 정지

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        StopAllCoroutines();

     

        // 스테이지 데이터 초기화
        currentStage = null;
        placedMap = null;
        tileMapGrid = null;
    }


}
