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

    //test용 변수
    [SerializeField] private float arrivalDist = 0.1f;
    [SerializeField] private bool showDebugPath = true;

    //경로 변수
    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Collider2D, Enemy> activeEnemies = new Dictionary<Collider2D, Enemy>();
    private Dictionary<Enemy, List<Vector3>> enemyPaths = new Dictionary<Enemy, List<Vector3>>(); // 각 enemy의 개별 경로
    private Dictionary<Enemy, int> enemyPathIndex = new Dictionary<Enemy, int>();     // 각 enemy의 현재 경로 인덱스 -> 현재 몇번째 경로 포인트로 가고있는지

    //job System 변수
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;
    private NativeArray<float> moveSpeeds;  // 추가

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

    //초기화 작업 해주기 

    public void InitializeMnanager()
    {
        enemyPaths = new Dictionary<Enemy, List<Vector3>>();
        enemyPathIndex = new Dictionary<Enemy, int>();
        activeEnemies = new Dictionary<Collider2D, Enemy>();
    }

    public void SpawnEnemy(string enemyName, Vector3? initPos = null)
    {

        Vector3 startPos = initPos ?? PathFindingManager.Instance.GetStartPosition();
        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyName, startPos, 10);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemy.Initialize();

            // 초기 경로 설정
            List<Vector3> initialPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
            enemyPaths[enemy] = initialPath;
            enemyPathIndex[enemy] = 0;
        }
    }

    // 모든 enemy List 가지고 오기
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

    //특정 조건의 enemy 반환
    public Enemy GetActiveEnemys(Collider2D collider)
    {
        Enemy enemy = null;
        activeEnemies.TryGetValue(collider, out enemy);
        return enemy;
    }

    // Collider2D를 키로 사용하여 Enemy 등록
    public void RegisterEnemy(Enemy enemy, Collider2D collider)
    {
        if (!activeEnemies.ContainsKey(collider))
        {
            activeEnemies.Add(collider, enemy);
            enemies.Add(enemy);
        }
    }

    // Enemy 해제
    public void UnregisterEnemy(Collider2D collider)
    {
        if (activeEnemies.ContainsKey(collider))
        {
            Enemy enemy = activeEnemies[collider];
            activeEnemies.Remove(collider);
            enemies.Remove(enemy);
            enemyPaths.Remove(enemy);
            enemyPathIndex.Remove(enemy);
        }
    }

    private void Update()
    {
        if (enemies.Count == 0 || Time.timeScale == 0) return; //enemy 없거나 비활성화면 리턴해야됨

        ShowDebug(); //디버깅용, scene에서 길 보여줌

        // TransformAccessArray, NativaArray 생성하고 이전것들은 Dispose해줘야됨
        // Enemy가 중간에 죽거나 새로 생성되는등 동적으로 관리해야되기때문에 크기 고정하면안됨
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        Transform[] transforms = enemies.Select(e => e.transform).ToArray();
        transformAccessArray = new TransformAccessArray(transforms);

        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (moveSpeeds.IsCreated) moveSpeeds.Dispose();
        targetPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);
        moveSpeeds = new NativeArray<float>(enemies.Count, Allocator.TempJob);

        //jobSystem에 필요한 데이터 설정
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            List<Vector3> path = enemyPaths[enemy];
            int pathIndex = enemyPathIndex[enemy];

            moveSpeeds[i] = enemy.moveSpeed;

            if (pathIndex < path.Count - 1) // 다음 경로 포인트 있을 경우
            {
                // 다음 목표 지점으로 이동 시작할 때 방향 체크
                Vector3 currentPos = path[pathIndex];
                Vector3 nextPos = path[pathIndex + 1];
                float directionX = nextPos.x - currentPos.x;

                if (Mathf.Abs(directionX) > 0.01f)
                {
                    enemy.spriteRenderer.flipX = directionX < 0;
                }

                targetPositions[i] = path[pathIndex + 1]; // 다음 목표 위치 설정
            }
            else
            {
                targetPositions[i] = enemy.transform.position; //마지막 위치면 현재 위치 유지
            }
        }

        // Job 실행, 매 프레임마다 job 실행
        var moveJob = new MoveEnemiesJob
        {
            TargetPositions = targetPositions,
            DeltaTime = Time.deltaTime,
            MoveSpeeds = moveSpeeds,
            ArrivalDist = arrivalDist
        };

        JobHandle jobHandle = moveJob.Schedule(transformAccessArray);
        jobHandle.Complete();

        // 이동 후 처리 (도착 체크 및 enemy 관리)
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (i >= enemies.Count) continue;

            Enemy enemy = enemies[i];
            if (enemy == null) continue; // null 체크

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
                // 마지막 지점 도달
                enemy.OnReachEndTile();
                UnregisterEnemy(enemy.enemyCollider);  // Dictionary에서도 제거
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

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();

        enemyPaths.Clear();
        enemyPathIndex.Clear();
        enemies.Clear();
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

public struct MoveEnemiesJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float> MoveSpeeds;  // 각 Enemy의 속도
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