using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager _instance;

    [SerializeField] private string enemyPoolId;
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalThreshold = 0.1f;
    [SerializeField] private bool showDebugPath = true;

    private List<Enemy> enemies = new List<Enemy>();
    private Dictionary<Enemy, List<Vector3>> enemyPaths; // �� enemy�� ���� ���
    private Dictionary<Enemy, int> enemyPathIndices;     // �� enemy�� ���� ��� �ε���
    private List<Vector3> currentPath;

    //enemy spawn

    [SerializeField] private float spawnInterval = 2f;  // �� ���� ����
    [SerializeField] private int enemiesPerWave = 5;   // ���̺�� �� ����
    private bool isSpawning = false;
    private Coroutine spawnCoroutine;

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
        enemyPathIndices = new Dictionary<Enemy, int>();


    }
    public void StartSpawning()
    {
        if (!isSpawning)
        {
            currentPath = PathFindingManager.Instance.GetCurrentPath();
            Debug.Log($"Starting enemy spawn with path count: {currentPath.Count}");

            isSpawning = true;
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        int enemiesSpawned = 0;

        while (isSpawning)
        {
            if (enemiesSpawned < enemiesPerWave)
            {
                SpawnSingleEnemy();
                enemiesSpawned++;
                yield return new WaitForSeconds(spawnInterval);
            }
            else
            {
                // ��� ���� �����Ǹ� ��� ��� �� ���� ���̺� ����
                enemiesSpawned = 0;
                yield return new WaitForSeconds(spawnInterval * 3); // ���̺� �� ����
            }
        }
    }

    private void SpawnSingleEnemy()
    {
        Vector3 startPos = PathFindingManager.Instance.GetStartPosition();
        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyPoolId, startPos);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemies.Add(enemy);

            // �ʱ� ��� ����
            List<Vector3> initialPath = PathFindingManager.Instance.FindPathFromPosition(startPos);
            enemyPaths[enemy] = initialPath;
            enemyPathIndices[enemy] = 0;

            Debug.Log($"Spawned enemy at position: {startPos}");
        }
    }


    private void Update()
    {
        if (enemies.Count == 0) return;


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

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            List<Vector3> enemyPath = enemyPaths[enemy];
            int pathIndex = enemyPathIndices[enemy];

            if (pathIndex < enemyPath.Count - 1)
            {
                Vector3 targetPos = enemyPath[pathIndex + 1];
                Vector3 currentPos = enemy.transform.position;

                Vector3 direction = (targetPos - currentPos).normalized;
                enemy.transform.position += direction * moveSpeed * Time.deltaTime;

                if (Vector3.Distance(currentPos, targetPos) < arrivalThreshold)
                {
                    enemyPathIndices[enemy]++;
                    Debug.Log($"Enemy reached waypoint {pathIndex + 1}");
                }
            }
            else
            {
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
                enemies.RemoveAt(i);
                enemyPaths.Remove(enemy);
                enemyPathIndices.Remove(enemy);
            }
        }
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
                enemyPathIndices[enemy] = 0;
            }
        }
    }

    private void OnGUI()
    {
        if (!showDebugPath) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label($"Active Enemies: {enemies.Count}");

        if (enemies.Count > 0)
        {
            foreach (var enemy in enemies)
            {
                GUILayout.Label($"Enemy Path Points: {enemyPaths[enemy].Count}");
                GUILayout.Label($"Enemy at path index: {enemyPathIndices[enemy]}");

                // ���� ��ġ�� ��ǥ ��ġ�� ǥ��
                if (enemyPathIndices[enemy] < enemyPaths[enemy].Count - 1)
                {
                    Vector3 currentPos = enemy.transform.position;
                    Vector3 targetPos = enemyPaths[enemy][enemyPathIndices[enemy] + 1];
                    GUILayout.Label($"Current Pos: {currentPos:F1}");
                    GUILayout.Label($"Target Pos: {targetPos:F1}");
                    GUILayout.Label($"Distance: {Vector3.Distance(currentPos, targetPos):F2}");
                }
                GUILayout.Space(10);  // �� enemy ���� ���̿� ���� �߰�
            }
        }

        GUILayout.EndArea();
    }
}