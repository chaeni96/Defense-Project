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

    //경로 변수
    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Collider2D, Enemy> activeEnemies = new Dictionary<Collider2D, Enemy>();

    // 각 enemy의 개별 경로, 각 enemy의 전체 이동 경로를 저장하는 딕셔너리
    private Dictionary<Enemy, List<Vector3>> enemyPaths = new Dictionary<Enemy, List<Vector3>>();


    // 각 enemy의 현재 경로 인덱스 -> 현재 몇번째 경로 포인트로 가고있는지, 몇번째 지점인지
    private Dictionary<Enemy, int> enemyPathIndex = new Dictionary<Enemy, int>();     

    //job System 변수
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;
    private NativeArray<float> moveSpeeds;  // 추가


    private LineRenderer mainPathRenderer;  // 메인 경로용 LineRenderer
    private float pathDistanceGap = 0.5f;  // 경로 이탈 판정하는 수치,enemy의 개인경로가 메인경로와 얼마나 달라야 개별 경로를 그릴지 결정하는 기준값
                                                  
    private List<Vector3> mainPath = new List<Vector3>();

    private const int BASE_ORDER = 0; // Order in Layer의 기본값

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
        //기존 데이터가 있다면 먼저 정리
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

        // OnWaveFinish 이벤트 구독
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

    public void SpawnEnemy(D_EnemyData enemyData, Vector2? customStartTilePos = null, Vector2? spawnOffset = null, List<D_EventDummyData> events = null)
    {
        // 시작 타일 위치 (기본값 또는 지정된 값 사용)
        Vector2 startTilePos = customStartTilePos ?? TileMapManager.Instance.GetStartPosition();

        // 시작 위치를 월드 좌표로 변환
        Vector3 startWorldPos = TileMapManager.Instance.GetTileToWorldPosition(startTilePos);

        // 스폰 오프셋 적용 (기본값 또는 지정된 값 사용)
        Vector2 offset = spawnOffset ?? Vector2.zero;
        Vector3 spawnPos = new Vector3(startWorldPos.x + offset.x, startWorldPos.y + offset.y, startWorldPos.z);

        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyData.f_ObjectPoolKey.f_PoolObjectAddressableKey, spawnPos, (int)ObjectLayer.Enemy);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = spawnPos;
            enemy.Initialize();
            enemy.InitializeEnemyInfo(enemyData);

            if (events != null && events.Count() > 0)
            {
                enemy.InitializeEvents(events);
            }

            // 경로 설정: 스폰 위치 -> 시작 타일 -> 끝 타일
            List<Vector3> path = new List<Vector3>();

            // 오프셋이 있으면 시작 타일을 첫 경유지로 추가
            if (offset != Vector2.zero)
            {
                path.Add(startWorldPos);
            }

            // 시작 타일에서 끝 타일까지의 경로 가져오기
            List<Vector3> mainPath = PathFindingManager.Instance.FindPathFromPosition(startWorldPos);

            // 경로 합치기
            path.AddRange(mainPath);

            // 경로 설정
            enemyPaths[enemy] = path;
            enemyPathIndex[enemy] = 0;

            UpdateEnemiesPath();
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

    public Enemy GetEnemyAtPosition(Vector3 position)
    {
        return activeEnemies
            .FirstOrDefault(kvp => kvp.Value.transform.position == position)
            .Value;
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
            enemy.pathRenderer.positionCount = 0;
            enemies.Remove(enemy);
            enemyPaths.Remove(enemy);
            enemyPathIndex.Remove(enemy);
        }
    }


    private void Update()
    {
        if (enemies.Count == 0 || Time.timeScale == 0) return; //enemy 없거나 비활성화면 리턴해야됨

        try
        {
            // TransformAccessArray, NativeArray 초기화
            InitializeArrays(enemies.Count);

            //jobSystem에 필요한 데이터 설정
            for (int i = 0; i < enemies.Count; i++)
            {
                Enemy enemy = enemies[i];

                // 이동 준비가 완료된 적만 처리
                if (!enemy.isReadyToMove)
                {
                    // 아직 이동 준비가 안된 적은 현재 위치 유지
                    targetPositions[i] = enemy.transform.position;
                    moveSpeeds[i] = 0; // 속도를 0으로 설정하여 이동하지 않게 함
                    continue;
                }


                List<Vector3> enemyPath = enemyPaths[enemy];
                int pathIndex = enemyPathIndex[enemy];

                moveSpeeds[i] = enemy.GetStat(StatName.MoveSpeed);

                if (pathIndex < enemyPath.Count - 1) // 다음 경로 포인트 있을 경우
                {
                    // 다음 목표 지점으로 이동 시작할 때 방향 체크
                    Vector3 currentPos = enemyPath[pathIndex];
                    Vector3 nextPos = enemyPath[pathIndex + 1];

                    float directionX = nextPos.x - currentPos.x;

                    if (Mathf.Abs(directionX) > 0.01f)
                    {
                        enemy.spriteRenderer.flipX = directionX < 0;
                    }

                    targetPositions[i] = enemyPath[pathIndex + 1]; // 다음 목표 위치 설정
                }
                else
                {
                    targetPositions[i] = enemy.transform.position; //마지막 위치면 현재 위치 유지
                }
            }

            // Job 실행
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

                if (enemy == null) continue;


                // Sorting Order 업데이트
                UpdateEnemySortingOrder(enemy);

                // 기존 도착 체크 로직
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
                    // 마지막 지점 도달
                    enemy.OnReachEndTile();

                }
            }

        }
        finally
        {
            // 항상 NativeArray들 정리
            if (transformAccessArray.isCreated) transformAccessArray.Dispose();
            if (targetPositions.IsCreated) targetPositions.Dispose();
            if (moveSpeeds.IsCreated) moveSpeeds.Dispose();
        }
    }
    public void UpdateEnemiesPath()
    {
        // 메인 경로 업데이트
        var startPos = TileMapManager.Instance.GetTileToWorldPosition(TileMapManager.Instance.GetStartPosition());
        mainPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
       
        // 경로 라인 활성화 및 업데이트
        if (mainPathRenderer != null)
        {
            mainPathRenderer.enabled = true;
            UpdateMainPathVisual();
        }

        // 각 enemy의 경로 업데이트
        foreach (var enemy in enemies)
        {
            List<Vector3> newPath = PathFindingManager.Instance.FindPathFromPosition(enemy.transform.position);
            if (newPath.Count > 0)
            {
                enemyPaths[enemy] = newPath;
                enemyPathIndex[enemy] = 0;

                // 메인 경로와 크게 다른 경우에만 개별 경로 표시
                bool shouldShowPath = IsPathDifferentMainPath(newPath);
                UpdateEnemyPathVisual(enemy, shouldShowPath ? newPath : null);
            }
        }
    }


    private void UpdateEnemySortingOrder(Enemy enemy)
    {
        Vector2 enemyTilePos = TileMapManager.Instance.GetWorldToTilePosition(enemy.transform.position);
        TileData tileData = TileMapManager.Instance.GetTileData(new Vector2(enemyTilePos.x, Mathf.Floor(enemyTilePos.y)));

        if (tileData != null && tileData.placedUnit != null)
        {
            UnitController unit = tileData.placedUnit;
            float unitY = unit.transform.position.y;
            float enemyY = enemy.transform.position.y;

            if (enemyY > unitY + 0.1f)
            {
                // Enemy가 Unit보다 위에 있으면 뒤로
                enemy.spriteRenderer.sortingOrder = BASE_ORDER - Mathf.RoundToInt((enemyY - unitY) * 10);
            }
            else if (Mathf.Abs(enemyY - unitY) <= 0.1f)
            {
                // 같은 높이면 Enemy가 앞에
                enemy.spriteRenderer.sortingOrder = BASE_ORDER + 1;
            }
            else
            {
                // Enemy가 Unit보다 아래에 있으면 앞으로
                enemy.spriteRenderer.sortingOrder = BASE_ORDER + Mathf.RoundToInt((unitY - enemyY) * 10);
            }
        }
        else
        {
            enemy.spriteRenderer.sortingOrder = BASE_ORDER;
        }
    }

    //메인경로 라인 그리기
    private void UpdateMainPathVisual()
    {
        mainPathRenderer.positionCount = mainPath.Count;
        mainPathRenderer.SetPositions(mainPath.ToArray());
    }

    private void UpdateEnemyPathVisual(Enemy enemy, List<Vector3> path)
    {
        if (path == null)
        {
            // 메인 경로와 비슷하면 개별 경로를 지움
            // 경로가 null이면 LineRenderer 비활성화
            if (enemy.pathRenderer != null)
            {
                enemy.pathRenderer.positionCount = 0;
            }
        }
        else
        {
            // 메인 경로와 많이 다르면 개별 경로를 그림
            if (enemy.pathRenderer != null)
            {
                enemy.pathRenderer.positionCount = path.Count;
                enemy.pathRenderer.SetPositions(path.ToArray());
            }
        }
    }

    //메인 경로와 차이 검사
    private bool IsPathDifferentMainPath(List<Vector3> enemyPath)
    {
        if (mainPath.Count == 0 || enemyPath.Count == 0)
            return true;

        // 중간 지점들을 비교하여 차이가 큰지 확인
        for (int i = 1; i < enemyPath.Count - 1; i++)
        {
            float minDistance = float.MaxValue;

            foreach (var mainPoint in mainPath)
            {
                float distance = Vector3.Distance(enemyPath[i], mainPoint);
                minDistance = Mathf.Min(minDistance, distance);
            }

            // 한 지점이라도 기준값보다 멀면 다른 경로로 판단
            if (minDistance > pathDistanceGap)
                return true;
        }

        return false;
    }

    // 웨이브 종료시 라인 숨기기
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

        // 모든 활성화된 enemy를 풀로 반환
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