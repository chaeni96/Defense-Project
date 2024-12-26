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
    private List<int> enemyPathIndices = new List<int>();  // �� ���� ���� ��� �ε���
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
        Vector3 startPos = currentPath[0];
        GameObject enemyObj = PoolingManager.Instance.GetObject(enemyPoolId, startPos);

        if (enemyObj != null)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();
            enemy.transform.position = startPos;
            enemies.Add(enemy);
            enemyPathIndices.Add(0);
            Debug.Log($"Spawned enemy at position: {startPos}");
        }
    }


    private void Update()
    {

        if (enemies.Count == 0) return;

        // ����� ��� ǥ��
        if (showDebugPath && currentPath != null && currentPath.Count > 1)
        {
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Debug.DrawLine(currentPath[i], currentPath[i + 1], Color.yellow);
            }
        }

        // �� �̵� ó��
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];
            int currentIndex = enemyPathIndices[i];

            // ���� ��� ����Ʈ�� �ִ��� Ȯ��
            if (currentIndex < currentPath.Count - 1)
            {
                Vector3 targetPos = currentPath[currentIndex + 1];
                Vector3 currentPos = enemy.transform.position;

                // ��ǥ �������� �̵�
                Vector3 direction = (targetPos - currentPos).normalized;
                enemy.transform.position += direction * moveSpeed * Time.deltaTime;

                // ���� ��ǥ ���� ���� Ȯ��
                if (Vector3.Distance(currentPos, targetPos) < arrivalThreshold)
                {
                    enemyPathIndices[i]++;
                    Debug.Log($"Enemy {i} reached waypoint {currentIndex + 1}");
                }
            }
            else
            {
                // ������ ���� ����
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
                enemies.RemoveAt(i);
                enemyPathIndices.RemoveAt(i);
            }
        }
    }

    public void UpdateEnemiesPath(List<Vector3> newPath)
    {

        // �� enemy���� ���� ��ġ���� ���ο� ��� ���
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            List<Vector3> individualPath = PathFindingManager.Instance.FindPathFromPosition(enemy.transform.position);

            if (individualPath.Count > 0)
            {
                // ���� ��ġ������ �� ��� ����
                enemyPathIndices[i] = 0;  // �� ����� ���������� ����
                enemy.transform.position = individualPath[0];  // ���� ��ġ ����
            }
        }

        // ���� ������ enemy�� �⺻ ��� ������Ʈ
        currentPath = newPath;
    }

    //������
    private void OnGUI()
    {
        if (!showDebugPath) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 500));
        GUILayout.Label($"Active Enemies: {enemies.Count}");
        GUILayout.Label($"Current Path Points: {(currentPath != null ? currentPath.Count : 0)}");

        if (enemies.Count > 0)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                GUILayout.Label($"Enemy {i} at path index: {enemyPathIndices[i]}");
            }
        }
        GUILayout.EndArea();
    }
}