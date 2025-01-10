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
            Debug.Log("������ ���̺�!");
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

        // ���̺� �Ϸ� �� ���
        yield return new WaitForSeconds(waveData.f_SpawnDelay);

        // ���� ���̺� ����
        currentWaveIndex++;
        StartNextWave();
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
     public bool IsLastWave()
    {
        return currentWaveIndex >= currentStage.f_WaveData.Count;
    }



    private void CleanUp()
    {
        // ���� ���� �ڷ�ƾ�� �ִٸ� ����

        if (waveCoroutine != null)
        {
            StopCoroutine(waveCoroutine);
            waveCoroutine = null;
        }

        StopAllCoroutines();

     

        // �������� ������ �ʱ�ȭ
        currentStage = null;
        placedMap = null;
        tileMapGrid = null;
    }


}
