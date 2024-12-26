using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
 
    public static EnemyManager _instance;

    [SerializeField] private string enemyPoolId;  // D_ObjectPoolData�� name�� ��ġ�ؾ� ��
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    private List<Enemy> enemies = new List<Enemy>();
    private List<float3> startPositions = new List<float3>();
    private List<float3> endPositions = new List<float3>();
    private List<float3> currentPositions = new List<float3>();


    // �̱��� ���� ����
    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EnemyManager>();

                // �ν��Ͻ��� ������ ���ο� ���� ������Ʈ�� �����Ͽ� PoolingManager ������Ʈ�� �߰�
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("EnemyManager");
                    _instance = singleton.AddComponent<EnemyManager>();
                    DontDestroyOnLoad(singleton); // ���� ����Ǿ �ı����� �ʵ��� ����
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

    public void SpawnInitialEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            float3 startPos = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0);
            float3 endPos = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0);

            GameObject enemyObj = PoolingManager.Instance.GetObject(enemyPoolId, new Vector3(startPos.x, startPos.y, startPos.z));
            if (enemyObj != null)
            {
                Enemy enemy = enemyObj.GetComponent<Enemy>();
                enemy.StartPosition = startPos;
                enemy.EndPosition = endPos;
                enemy.Speed = moveSpeed;

                enemies.Add(enemy);
                startPositions.Add(startPos);
                endPositions.Add(endPos);
                currentPositions.Add(startPos);
            }
        }
    }

    private void Update()
    {
        if (enemies.Count == 0) return;

        int currentCount = enemies.Count;
        var tempCurrentPos = new NativeArray<float3>(currentCount, Allocator.TempJob);
        var tempStartPos = new NativeArray<float3>(currentCount, Allocator.TempJob);
        var tempEndPos = new NativeArray<float3>(currentCount, Allocator.TempJob);

        // List �����͸� NativeArray�� ����
        for (int i = 0; i < currentCount; i++)
        {
            tempCurrentPos[i] = currentPositions[i];
            tempStartPos[i] = startPositions[i];
            tempEndPos[i] = endPositions[i];
        }

        MoveEnemiesJob moveJob = new MoveEnemiesJob
        {
            StartPositions = tempStartPos,
            EndPositions = tempEndPos,
            CurrentPositions = tempCurrentPos,
            DeltaTime = Time.deltaTime,
            MoveSpeed = moveSpeed
        };

        // Job ����
        JobHandle jobHandle = moveJob.Schedule(currentCount, 64);
        jobHandle.Complete();

        // ����� ����Ʈ�� �ٽ� �����ϰ� ���� �˻�
        for (int i = currentCount - 1; i >= 0; i--)
        {
            currentPositions[i] = tempCurrentPos[i];
            float3 currentPos = currentPositions[i];
            float3 targetPos = endPositions[i];

            if (math.distance(currentPos, targetPos) < arrivalThreshold)
            {
                // Enemy ����
                PoolingManager.Instance.ReturnObject(enemies[i].gameObject);

                enemies.RemoveAt(i);
                startPositions.RemoveAt(i);
                endPositions.RemoveAt(i);
                currentPositions.RemoveAt(i);
            }
            else
            {
                enemies[i].UpdatePosition(currentPos);
            }
        }

        // �ӽ� NativeArray ����
        tempCurrentPos.Dispose();
        tempStartPos.Dispose();
        tempEndPos.Dispose();
    }
}

public struct MoveEnemiesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> StartPositions;
    [ReadOnly] public NativeArray<float3> EndPositions;
    public NativeArray<float3> CurrentPositions;
    public float DeltaTime;
    public float MoveSpeed;

    public void Execute(int index)
    {
        float3 currentPos = CurrentPositions[index];
        float3 targetPos = EndPositions[index];

        float3 direction = math.normalize(targetPos - currentPos);
        float3 newPosition = currentPos + direction * DeltaTime * MoveSpeed;

        CurrentPositions[index] = newPosition;
    }
}