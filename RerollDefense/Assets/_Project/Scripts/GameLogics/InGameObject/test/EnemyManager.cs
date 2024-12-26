using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

public class EnemyManager : MonoBehaviour
{
 
    public static EnemyManager _instance;

    [SerializeField] private string enemyPoolId;  // D_ObjectPoolData의 name과 일치해야 함
    [SerializeField] private int enemyCount = 5;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;

    private List<Enemy> enemies = new List<Enemy>();
    private List<float3> startPositions = new List<float3>();
    private List<float3> endPositions = new List<float3>();
    private List<float3> currentPositions = new List<float3>();


    // 싱글톤 패턴 구현
    public static EnemyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EnemyManager>();

                // 인스턴스가 없으면 새로운 게임 오브젝트를 생성하여 PoolingManager 컴포넌트를 추가
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("EnemyManager");
                    _instance = singleton.AddComponent<EnemyManager>();
                    DontDestroyOnLoad(singleton); // 씬이 변경되어도 파괴되지 않도록 설정
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
        List<Vector3> currentPath = PathFindingManager.Instance.GetCurrentPath();

        for (int i = 0; i < enemyCount; i++)
        {
            float3 startPos = new float3(currentPath[0].x, currentPath[0].y, currentPath[0].z);
            float3 endPos = new float3(currentPath[1].x, currentPath[1].y, currentPath[1].z);

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
            MoveSpeed = 2f // 이동 속도 조절 가능
        };

        JobHandle jobHandle = moveJob.Schedule(currentCount, 64);
        jobHandle.Complete();

        for (int i = currentCount - 1; i >= 0; i--)
        {
            currentPositions[i] = tempCurrentPos[i];
            float3 currentPos = currentPositions[i];
            float3 targetPos = endPositions[i];

            if (math.distance(currentPos, targetPos) < 0.1f)
            {
                List<Vector3> currentPath = PathFindingManager.Instance.GetCurrentPath();
                int currentPathIndex = GetCurrentPathIndex(new Vector3(currentPos.x, currentPos.y, currentPos.z), currentPath);

                if (currentPathIndex < currentPath.Count - 1)
                {
                    endPositions[i] = new float3(
                        currentPath[currentPathIndex + 1].x,
                        currentPath[currentPathIndex + 1].y,
                        currentPath[currentPathIndex + 1].z
                    );
                }
                else
                {
                    PoolingManager.Instance.ReturnObject(enemies[i].gameObject);

                    enemies.RemoveAt(i);
                    startPositions.RemoveAt(i);
                    endPositions.RemoveAt(i);
                    currentPositions.RemoveAt(i);
                }
            }
            else
            {
                enemies[i].UpdatePosition(currentPos);
            }
        }

        tempCurrentPos.Dispose();
        tempStartPos.Dispose();
        tempEndPos.Dispose();
    }

    // 현재 경로에서 가장 가까운 지점의 인덱스를 찾는 메서드 추가
    private int GetCurrentPathIndex(Vector3 currentPos, List<Vector3> path)
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < path.Count; i++)
        {
            float distance = Vector3.Distance(currentPos, path[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void UpdateEnemiesPath(List<Vector3> newPath)
    {
        // 각 적의 현재 위치에서 가장 가까운 경로 지점 찾기
        for (int i = 0; i < enemies.Count; i++)
        {
            // 현재 위치에서 가장 가까운 경로 지점의 인덱스 찾기
            int closestIndex = 0;
            float minDistance = float.MaxValue;
            for (int j = 0; j < newPath.Count; j++)
            {
                float distance = Vector3.Distance(enemies[i].transform.position, newPath[j]);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestIndex = j;
                }
            }

            // 현재 위치 근처의 경로 지점부터 이동 시작
            int nextIndex = Mathf.Min(closestIndex + 1, newPath.Count - 1);
            endPositions[i] = new float3(
                newPath[nextIndex].x,
                newPath[nextIndex].y,
                newPath[nextIndex].z
            );
        }
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

       // 직선 방향으로 고정된 속도로 이동
        float3 direction = math.normalize(targetPos - currentPos);
        float3 newPosition = currentPos + direction * DeltaTime * MoveSpeed;

        CurrentPositions[index] = newPosition;
    }
}