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
            MoveSpeed = 2f // �̵� �ӵ� ���� ����
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

    // ���� ��ο��� ���� ����� ������ �ε����� ã�� �޼��� �߰�
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
        // �� ���� ���� ��ġ���� ���� ����� ��� ���� ã��
        for (int i = 0; i < enemies.Count; i++)
        {
            // ���� ��ġ���� ���� ����� ��� ������ �ε��� ã��
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

            // ���� ��ġ ��ó�� ��� �������� �̵� ����
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

       // ���� �������� ������ �ӵ��� �̵�
        float3 direction = math.normalize(targetPos - currentPos);
        float3 newPosition = currentPos + direction * DeltaTime * MoveSpeed;

        CurrentPositions[index] = newPosition;
    }
}