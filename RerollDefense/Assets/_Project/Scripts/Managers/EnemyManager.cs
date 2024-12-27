using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Linq;
using UnityEngine.Jobs;

public class EnemyManager : MonoBehaviour
{
    private static EnemyManager _instance;

    //test�� ����
    [SerializeField] private float arrivalDist = 0.1f;
    [SerializeField] private bool showDebugPath = true;

    //��� ����
    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Enemy, List<Vector3>> enemyPaths = new Dictionary<Enemy, List<Vector3>>(); // �� enemy�� ���� ���
    private Dictionary<Enemy, int> enemyPathIndex = new Dictionary<Enemy, int>();     // �� enemy�� ���� ��� �ε��� -> ���� ���° ��� ����Ʈ�� �����ִ���

    //job System ����
    private NativeArray<float3> targetPositions;
    private TransformAccessArray transformAccessArray;
    private NativeArray<float> moveSpeeds;  // �߰�

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

    //��� enemy ��������
    public List<Enemy> GetEnemies() => enemies;

    public void SpawnEnemy(string enemyName)
    {
        Vector3 startPos = PathFindingManager.Instance.GetStartPosition();
        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyName, startPos);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemies.Add(enemy);

            // �ʱ� ��� ����
            List<Vector3> initialPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
            enemyPaths[enemy] = initialPath;
            enemyPathIndex[enemy] = 0;
        }
    }

    private void Update()
    {
        if (enemies.Count == 0) return;

        ShowDebug();

        // TransformAccessArray �ʱ�ȭ
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        Transform[] transforms = enemies.Select(e => e.transform).ToArray();
        transformAccessArray = new TransformAccessArray(transforms);

        // Ÿ�� ������, enemy speed ����
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (moveSpeeds.IsCreated) moveSpeeds.Dispose();

        targetPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);
        moveSpeeds = new NativeArray<float>(enemies.Count, Allocator.TempJob);

        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            List<Vector3> path = enemyPaths[enemy];
            int pathIndex = enemyPathIndex[enemy];

            moveSpeeds[i] = enemy.moveSpeed;

            if (pathIndex < path.Count - 1)
            {
                targetPositions[i] = path[pathIndex + 1];
            }
            else
            {
                targetPositions[i] = enemy.transform.position;
            }
        }

        // Job ����
        var moveJob = new MoveEnemiesJob
        {
            TargetPositions = targetPositions,
            DeltaTime = Time.deltaTime,
            MoveSpeeds = moveSpeeds,
            ArrivalDist = arrivalDist
        };

        JobHandle jobHandle = moveJob.Schedule(transformAccessArray);
        jobHandle.Complete();

        // ���� üũ �� enemy ����
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            int pathIndex = enemyPathIndex[enemy];
            List<Vector3> path = enemyPaths[enemy];

            if (pathIndex < path.Count - 1)
            {
                Vector3 targetPos = path[pathIndex + 1];
                if (Vector3.Distance(enemy.transform.position, targetPos) < arrivalDist)
                {
                    enemyPathIndex[enemy]++;
                }
            }
            else
            {
                // ������ ���� ����
                enemy.OnReachEndTile();
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
                enemies.RemoveAt(i);
                enemyPaths.Remove(enemy);
                enemyPathIndex.Remove(enemy);
            }
        }

        targetPositions.Dispose();
        moveSpeeds.Dispose();
    }

    public void UpdateEnemiesPath()
    {
        // �� enemy���� ���� ��ġ���� ���ο� ��� ���
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

    private void OnDestroy()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        CleanUp();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();

        enemyPaths.Clear();
        enemyPathIndex.Clear();
        enemies.Clear();
    }

    private void OnDisable()
    {
        CleanUp();
    }

    #region ��ã�� Debug
    private void ShowDebug()
    {
        // ����� ��� ǥ��
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

public struct MoveEnemiesJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float> MoveSpeeds;  // �� Enemy�� �ӵ�
    public float DeltaTime;
    public float ArrivalDist;

    public void Execute(int index, TransformAccess transform)
    {
        float3 currentPos = transform.position;
        float3 targetPos = TargetPositions[index];

        if (math.distance(currentPos, targetPos) > ArrivalDist)
        {
            float3 direction = math.normalize(targetPos - currentPos);
            float3 newPosition = currentPos + direction * DeltaTime * MoveSpeeds[index];
            transform.position = newPosition;
        }
    }
}