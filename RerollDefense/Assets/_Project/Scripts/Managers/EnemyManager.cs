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

    //��� ����
    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Collider2D, Enemy> activeEnemies = new Dictionary<Collider2D, Enemy>();

    // �� enemy�� ���� ���, �� enemy�� ��ü �̵� ��θ� �����ϴ� ��ųʸ�
    private Dictionary<Enemy, List<Vector3>> enemyPaths = new Dictionary<Enemy, List<Vector3>>();


    // �� enemy�� ���� ��� �ε��� -> ���� ���° ��� ����Ʈ�� �����ִ���, ���° ��������
    private Dictionary<Enemy, int> enemyPathIndex = new Dictionary<Enemy, int>();     

    //job System ����
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;
    private NativeArray<float> moveSpeeds;  // �߰�


    private LineRenderer mainPathRenderer;  // ���� ��ο� LineRenderer
    private float pathDistanceGap = 0.5f;  // ��� ��Ż �����ϴ� ��ġ,enemy�� ���ΰ�ΰ� ���ΰ�ο� �󸶳� �޶�� ���� ��θ� �׸��� �����ϴ� ���ذ�
                                                  
    private List<Vector3> mainPath = new List<Vector3>();


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
    }

    //�ʱ�ȭ �۾� ���ֱ� 

    public void InitializeMnanager()
    {
        //���� �����Ͱ� �ִٸ� ���� ����
        CleanUp();

        if (mainPathRenderer == null)
        {
            mainPathRenderer = gameObject.AddComponent<LineRenderer>();
            mainPathRenderer.startWidth = 0.03f;
            mainPathRenderer.endWidth = 0.03f;
            mainPathRenderer.sortingOrder = 1;
        }

        enemyPaths = new Dictionary<Enemy, List<Vector3>>();
        enemyPathIndex = new Dictionary<Enemy, int>();
        activeEnemies = new Dictionary<Collider2D, Enemy>();

        // OnWaveFinish �̺�Ʈ ����
        StageManager.Instance.OnWaveFinish += HidePathLines;
    }

    private void InitializeArrays(int enemyCount)
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (moveSpeeds.IsCreated) moveSpeeds.Dispose();

        Transform[] transforms = new Transform[enemyCount];
        for (int i = 0; i < enemyCount; i++)
        {
            transforms[i] = enemies[i].transform;
        }
        transformAccessArray = new TransformAccessArray(transforms);

        targetPositions = new NativeArray<float3>(enemyCount, Allocator.TempJob);
        moveSpeeds = new NativeArray<float>(enemyCount, Allocator.TempJob);
    }

    public void SpawnEnemy(D_EnemyData enemyData, Vector3? initPos = null)
    {

        var startTilePos = TileMapManager.Instance.GetStartPosition();
        Vector3 startPos = initPos ?? TileMapManager.Instance.GetTileToWorldPosition(startTilePos);

        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyData.f_ObjectPoolKey.f_PoolObjectAddressableKey, startPos, (int)ObjectLayer.Enemy);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemy.Initialize();
            enemy.InitializeEnemyInfo(enemyData);

            // �ʱ� ��� ����
            List<Vector3> initialPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
            enemyPaths[enemy] = initialPath;
            enemyPathIndex[enemy] = 0;

            UpdateEnemiesPath();
        }

    }

    // ��� enemy List ������ ����
    public List<Enemy> GetAllEnemys() => enemies;
    public int GetEnemyCount() => enemies.Count;


    public void GetEnemyPositions(NativeArray<float3> positions)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            positions[i] = enemies[i].transform.position;
        }
    }

    public Enemy GetEnemyAtIndex(int index)
    {
        return (index >= 0 && index < enemies.Count) ? enemies[index] : null;
    }



    //Ư�� ������ enemy ��ȯ
    public Enemy GetActiveEnemys(Collider2D collider)
    {
        Enemy enemy = null;
        activeEnemies.TryGetValue(collider, out enemy);
        return enemy;
    }

    public Enemy GetEnemyAtPosition(Vector3 position)
    {
        return activeEnemies
            .FirstOrDefault(kvp => kvp.Value.transform.position == position)
            .Value;
    }

    // Collider2D�� Ű�� ����Ͽ� Enemy ���
    public void RegisterEnemy(Enemy enemy, Collider2D collider)
    {
        if (!activeEnemies.ContainsKey(collider))
        {
            activeEnemies.Add(collider, enemy);
            enemies.Add(enemy);
        }
    }

    // Enemy ����
    public void UnregisterEnemy(Collider2D collider)
    {
        if (activeEnemies.ContainsKey(collider))
        {
            Enemy enemy = activeEnemies[collider];
            activeEnemies.Remove(collider);
            enemy.pathRenderer.positionCount = 0;
            enemies.Remove(enemy);
            enemyPaths.Remove(enemy);
            enemyPathIndex.Remove(enemy);
        }
    }


    private void Update()
    {
        if (enemies.Count == 0 || Time.timeScale == 0) return; //enemy ���ų� ��Ȱ��ȭ�� �����ؾߵ�

        try
        {
            // TransformAccessArray, NativeArray �ʱ�ȭ
            InitializeArrays(enemies.Count);

            //jobSystem�� �ʿ��� ������ ����
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];
                List<Vector3> enemyPath = enemyPaths[enemy];
                int pathIndex = enemyPathIndex[enemy];

                moveSpeeds[i] = enemy.GetStat(StatName.MoveSpeed);

                if (pathIndex < enemyPath.Count - 1) // ���� ��� ����Ʈ ���� ���
                {
                    // ���� ��ǥ �������� �̵� ������ �� ���� üũ
                    Vector3 currentPos = enemyPath[pathIndex];
                    Vector3 nextPos = enemyPath[pathIndex + 1];
                    float directionX = nextPos.x - currentPos.x;

                    if (Mathf.Abs(directionX) > 0.01f)
                    {
                        enemy.spriteRenderer.flipX = directionX < 0;
                    }

                    targetPositions[i] = enemyPath[pathIndex + 1]; // ���� ��ǥ ��ġ ����
                }
                else
                {
                    targetPositions[i] = enemy.transform.position; //������ ��ġ�� ���� ��ġ ����
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

            // �̵� �� ó�� (���� üũ �� enemy ����)
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;

                Enemy enemy = enemies[i];

                if (enemy == null) continue;

                if (!enemyPaths.ContainsKey(enemy) || !enemyPathIndex.ContainsKey(enemy)) continue;

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

                }
            }

        }
        finally
        {
            // �׻� NativeArray�� ����
            if (transformAccessArray.isCreated) transformAccessArray.Dispose();
            if (targetPositions.IsCreated) targetPositions.Dispose();
            if (moveSpeeds.IsCreated) moveSpeeds.Dispose();
        }
    }
    public void UpdateEnemiesPath()
    {
        // ���� ��� ������Ʈ
        var startPos = TileMapManager.Instance.GetTileToWorldPosition(TileMapManager.Instance.GetStartPosition());
        mainPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
       
        // ��� ���� Ȱ��ȭ �� ������Ʈ
        if (mainPathRenderer != null)
        {
            mainPathRenderer.enabled = true;
            UpdateMainPathVisual();
        }

        // �� enemy�� ��� ������Ʈ
        foreach (var enemy in enemies)
        {
            List<Vector3> newPath = PathFindingManager.Instance.FindPathFromPosition(enemy.transform.position);
            if (newPath.Count > 0)
            {
                enemyPaths[enemy] = newPath;
                enemyPathIndex[enemy] = 0;

                // ���� ��ο� ũ�� �ٸ� ��쿡�� ���� ��� ǥ��
                bool shouldShowPath = IsPathDifferentMainPath(newPath);
                UpdateEnemyPathVisual(enemy, shouldShowPath ? newPath : null);
            }
        }
    }

    //���ΰ�� ���� �׸���
    private void UpdateMainPathVisual()
    {
        mainPathRenderer.positionCount = mainPath.Count;
        mainPathRenderer.SetPositions(mainPath.ToArray());
    }

    private void UpdateEnemyPathVisual(Enemy enemy, List<Vector3> path)
    {
        if (path == null)
        {
            // ���� ��ο� ����ϸ� ���� ��θ� ����
            // ��ΰ� null�̸� LineRenderer ��Ȱ��ȭ
            if (enemy.pathRenderer != null)
            {
                enemy.pathRenderer.positionCount = 0;
            }
        }
        else
        {
            // ���� ��ο� ���� �ٸ��� ���� ��θ� �׸�
            if (enemy.pathRenderer != null)
            {
                enemy.pathRenderer.positionCount = path.Count;
                enemy.pathRenderer.SetPositions(path.ToArray());
            }
        }
    }

    //���� ��ο� ���� �˻�
    private bool IsPathDifferentMainPath(List<Vector3> enemyPath)
    {
        if (mainPath.Count == 0 || enemyPath.Count == 0)
            return true;

        // �߰� �������� ���Ͽ� ���̰� ū�� Ȯ��
        for (int i = 1; i < enemyPath.Count - 1; i++)
        {
            float minDistance = float.MaxValue;

            foreach (var mainPoint in mainPath)
            {
                float distance = Vector3.Distance(enemyPath[i], mainPoint);
                minDistance = Mathf.Min(minDistance, distance);
            }

            // �� �����̶� ���ذ����� �ָ� �ٸ� ��η� �Ǵ�
            if (minDistance > pathDistanceGap)
                return true;
        }

        return false;
    }

    // ���̺� ����� ���� �����
    private void HidePathLines()
    {
        if (mainPathRenderer != null)
        {
            mainPathRenderer.enabled = false;
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.pathRenderer != null)
            {
                enemy.pathRenderer.enabled = false;
            }
        }
    }

    public void CleanUp()
    {
        if(mainPathRenderer != null)
        {
            mainPathRenderer.positionCount = 0;
        }
        mainPath.Clear();

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.pathRenderer != null)
            {
                enemy.pathRenderer.positionCount = 0;
            }
        }

        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (moveSpeeds.IsCreated) moveSpeeds.Dispose();

        // ��� Ȱ��ȭ�� enemy�� Ǯ�� ��ȯ
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];

            if (enemy != null)
            {
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
            }
        }

        enemyPaths.Clear();
        enemyPathIndex.Clear();
        enemies.Clear();
        activeEnemies.Clear();

        StageManager.Instance.OnWaveFinish -= HidePathLines;

    }

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