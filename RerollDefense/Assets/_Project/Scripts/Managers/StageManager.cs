using System.Collections;
using System.Collections.Generic;
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
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentWaveIndex >= currentStage.f_WaveData.Count)
        {
            Debug.Log("��� ���̺� Ŭ����!");
            return;
        }

        D_WaveData waveData = currentStage.f_WaveData[currentWaveIndex];
        waveCoroutine = StartCoroutine(SpawnWaveRoutine(waveData));
    }


    private IEnumerator SpawnWaveRoutine(D_WaveData waveData)
    {
        int spawnedCount = 0;

        while (spawnedCount < waveData.f_Count)
        {
            EnemyManager.Instance.SpawnEnemy(waveData.f_SpawnEnemyName);
            spawnedCount++;

            yield return new WaitForSeconds(waveData.f_SpawnDelay);
        }

        // ���̺� �Ϸ� �� ���
        yield return new WaitForSeconds(currentStage.f_WaveDelayTime);

        // ���� ���̺� ����
        currentWaveIndex++;
        StartNextWave();
    }

}
