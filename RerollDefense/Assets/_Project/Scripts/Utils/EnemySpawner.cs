using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // �� ������
    public Vector3Int spawnTilePosition; // Ÿ�ϸ� ���� ���� ��ġ
    public Vector3Int goalTilePosition; // Ÿ�ϸ� ���� ��ǥ ��ġ

    public float spawnInterval = 2.0f; // �� ���� ����
    private bool spawning = true;

    private void Start()
    {
        StartCoroutine(SpawnEnemies());
    }

    private IEnumerator SpawnEnemies()
    {
        while (spawning)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnEnemy()
    {
        // Ÿ�ϸ� ��ǥ�� ���� ��ǥ�� ��ȯ�Ͽ� ���� ���� ��ġ ����
        Vector3 spawnPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(spawnTilePosition);

        // �� ������ ����
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Enemy ��ũ��Ʈ �ʱ�ȭ (��� Ž�� �������� ��ǥ ����)
        EnemyObject enemy = enemyInstance.GetComponent<EnemyObject>();
        if (enemy != null)
        {
            enemy.Initialize(spawnTilePosition, goalTilePosition);
        }
        else
        {
            Debug.LogError("�� �����տ� Enemy ��ũ��Ʈ�� �����ϴ�!");
        }
    }

    // ������ ����
    public void StopSpawning()
    {
        spawning = false;
    }
}
