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

        //pathFindingManager�� ����Ÿ�ϰ� ��Ÿ�ϵ� �ʱ�ȭ ����ߵ�
        PathFindingManager.Instance.InitializePathTiles(stageData.f_StartTilePos, stageData.f_EndTilePos);
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
        yield return new WaitForSeconds(currentStage.f_WaveDelayTime);

        // ���� ���̺� ����
        currentWaveIndex++;
        StartNextWave();
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroup enemyGroupData)
    {
        // ��ŸƮ ������ ���
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
