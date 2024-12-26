using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] private Enemy enemyPrefab;
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    private List<Enemy> enemies = new List<Enemy>();
    private List<float3> startPositions = new List<float3>();
    private List<float3> endPositions = new List<float3>();
    private List<float3> currentPositions = new List<float3>();

    private void Start()
    {
        SpawnInitialEnemies();
    }

    private void SpawnInitialEnemies()
    {
        for (int i = 0; i < enemyCount; i++)
        {
            float3 startPos = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0);
            float3 endPos = new float3(UnityEngine.Random.Range(-10f, 10f), UnityEngine.Random.Range(-10f, 10f), 0);

            Enemy enemy = Instantiate(enemyPrefab);
            enemy.StartPosition = startPos;
            enemy.EndPosition = endPos;
            enemy.Speed = moveSpeed;
            enemy.transform.position = new Vector3(startPos.x, startPos.y, startPos.z);

            enemies.Add(enemy);
            startPositions.Add(startPos);
            endPositions.Add(endPos);
            currentPositions.Add(startPos);
        }
    }

    private void Update()
    {
        if (enemies.Count == 0) return;

        int currentCount = enemies.Count;
        var tempCurrentPos = new NativeArray<float3>(currentCount, Allocator.TempJob);
        var tempStartPos = new NativeArray<float3>(currentCount, Allocator.TempJob);
        var tempEndPos = new NativeArray<float3>(currentCount, Allocator.TempJob);

        // List 데이터를 NativeArray로 복사
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

        // Job 실행
        JobHandle jobHandle = moveJob.Schedule(currentCount, 64);
        jobHandle.Complete();

        // 결과를 리스트에 다시 복사하고 도착 검사
        for (int i = currentCount - 1; i >= 0; i--)
        {
            currentPositions[i] = tempCurrentPos[i];
            float3 currentPos = currentPositions[i];
            float3 targetPos = endPositions[i];

            if (math.distance(currentPos, targetPos) < arrivalThreshold)
            {
                // Enemy 제거
                Destroy(enemies[i].gameObject);
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

        // 임시 NativeArray 해제
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