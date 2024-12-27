using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager _instance;

    //test용 변수
    [SerializeField] private string enemyPoolName;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalDist = 0.1f;
    [SerializeField] private bool showDebugPath = true;

    //경로 변수
    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Enemy, List<Vector3>> enemyPaths; // 각 enemy의 개별 경로
    private Dictionary<Enemy, int> enemyPathIndex;     // 각 enemy의 현재 경로 인덱스 -> 현재 몇번째 경로 포인트로 가고있는지

    //job System 변수
    private NativeArray<float3> currentPositions;
    private NativeArray<float3> targetPositions;
    private NativeArray<float3> newPositions;

    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EnemyManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("EnemyManager");
                    _instance = singleton.AddComponent<EnemyManager>();
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
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        enemyPaths = new Dictionary<Enemy, List<Vector3>>();
        enemyPathIndex = new Dictionary<Enemy, int>();


    }

    public void SpawnEnemy(string enemyName)
    {
        Vector3 startPos = PathFindingManager.Instance.GetStartPosition();
        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyName, startPos);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemies.Add(enemy);

            // 초기 경로 설정
            List<Vector3> initialPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
            enemyPaths[enemy] = initialPath;
            enemyPathIndex[enemy] = 0;

            Debug.Log($"Spawned enemy at position: {startPos}");
        }
    }


    private void Update()
    {
        if (enemies.Count == 0) return;

        ShowDebug();

        // NativeArray 초기화
        if (currentPositions.IsCreated) currentPositions.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (newPositions.IsCreated) newPositions.Dispose();
        currentPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);
        targetPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);
        newPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);

        // job 사용을 위한 데이터 설정
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            List<Vector3> path = enemyPaths[enemy];
            int pathIndex = enemyPathIndex[enemy];

            if (pathIndex < path.Count - 1)
            {
                Vector3 currentPos = enemy.transform.position;
                Vector3 targetPos = path[pathIndex + 1];

                currentPositions[i] = new float3(currentPos.x, currentPos.y, currentPos.z);
                targetPositions[i] = new float3(targetPos.x, targetPos.y, targetPos.z);
            }
        }

        // Job 실행
        var moveJob = new MoveEnemiesJob
        {
            CurrentPositions = currentPositions,
            TargetPositions = targetPositions,
            NewPositions = newPositions,
            DeltaTime = Time.deltaTime,
            MoveSpeed = moveSpeed
        };

        JobHandle jobHandle = moveJob.Schedule(enemies.Count, 64);
        jobHandle.Complete();

        // enemy 이동 및 endtile 도착 체크
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            int pathIndex = enemyPathIndex[enemy];
            List<Vector3> path = enemyPaths[enemy];

            if (pathIndex < path.Count - 1)
            {
                float3 newPos = newPositions[i];

                // enemy class에서 이동 메서드 호출
                enemy.UpdatePosition(newPos);

                // 도착 체크
                Vector3 targetPos = path[pathIndex + 1];
                if (Vector3.Distance(enemy.transform.position, targetPos) < arrivalDist)
                {
                    enemyPathIndex[enemy]++;
                }
            }
            else
            {
                // 마지막 지점 도달
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
                enemies.RemoveAt(i);
                enemyPaths.Remove(enemy);
                enemyPathIndex.Remove(enemy);
            }
        }

        // Native 배열 정리
        currentPositions.Dispose();
        targetPositions.Dispose();
        newPositions.Dispose();
    }

    public void UpdateEnemiesPath()
    {
        // 각 enemy마다 현재 위치에서 새로운 경로 계산
        foreach (var enemy in enemies)
        {
            List<Vector3> newPath = PathFindingManager.Instance.FindPathFromPosition(enemy.transform.position);
            if (newPath.Count > 0)
            {
                enemyPaths[enemy] = newPath;
                enemyPathIndex[enemy] = 0;
            }
        }
    }


    #region 길찾기 Debug


    private void ShowDebug()
    {
        // 디버그 경로 표시
        if (showDebugPath)
        {
            foreach (var entry in enemyPaths)
            {
                List<Vector3> path = entry.Value;
                if (path != null && path.Count > 1)
                {
                    for (int i = 0; i < path.Count - 1; i++)
                    {
                        Debug.DrawLine(path[i], path[i + 1], Color.yellow);
                    }
                }
            }
        }
    }
    #endregion

}


public struct MoveEnemiesJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> CurrentPositions;
    [ReadOnly] public NativeArray<float3> TargetPositions;
    public NativeArray<float3> NewPositions;
    public float DeltaTime;
    public float MoveSpeed;

    public void Execute(int index)
    {
        float3 currentPos = CurrentPositions[index];
        float3 targetPos = TargetPositions[index];

        float3 direction = math.normalize(targetPos - currentPos);
        float3 newPosition = currentPos + direction * DeltaTime * MoveSpeed;

        NewPositions[index] = newPosition;
    }
}