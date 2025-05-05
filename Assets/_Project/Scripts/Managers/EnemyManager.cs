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

    //��� ����
    private List<Enemy> enemies = new List<Enemy>();

    public event System.Action OnEnemyDeath;

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

        enemies = new List<Enemy> { };
    }


    public void NotifyEnemyDead()
    {
        // �̺�Ʈ �߻�
        OnEnemyDeath?.Invoke();
    }

    public void SpawnEnemy(D_EnemyData enemyData, Vector2 spawnPos, List<D_EventDummyData> events = null)
    {
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
            RegisterEnemy(enemy);
        }
    }

    // ��� enemy List ������ ����
    public List<Enemy> GetAllEnemys() => enemies;

    //Ȱ��ȭ�� ���ʹ� ���� ���������
    public int GetActiveEnemyCount()
    {
        return enemies.Count(enemy => enemy.isActive);
    }
       
    public void RegisterEnemy(Enemy enemy)
    {

        if (!enemies.Contains(enemy))
        {
            enemy.isActive = true;
            enemies.Add(enemy);
        }
    }

    // Enemy ����
    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            enemy.isActive = false;
            PoolingManager.Instance.ReturnObject(enemy.gameObject);
        }
    }

    public BasicObject GetNearestEnemy(Vector2 position)
    {
        BasicObject nearest = null;
        float minDistance = float.MaxValue;

        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            float distance = Vector2.Distance(
                position,
                new Vector2(enemy.transform.position.x, enemy.transform.position.y)
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }

        return nearest;
    }

    public void CleanUp()
    {
        // ��� Ȱ��ȭ�� enemy�� Ǯ�� ��ȯ
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var enemy = enemies[i];

            if (enemy != null)
            {
                PoolingManager.Instance.ReturnObject(enemy.gameObject);
            }
        }

        enemies.Clear();
    }

}