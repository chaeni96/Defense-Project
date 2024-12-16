using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab; // 적 프리팹
    public Vector3Int spawnTilePosition; // 타일맵 상의 스폰 위치
    public Vector3Int goalTilePosition; // 타일맵 상의 목표 위치

    public float spawnInterval = 2.0f; // 적 생성 간격
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
        // 타일맵 좌표를 월드 좌표로 변환하여 적의 스폰 위치 설정
        Vector3 spawnPosition = TileMapManager.Instance.tileMap.GetCellCenterWorld(spawnTilePosition);

        // 적 프리팹 생성
        GameObject enemyInstance = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        // Enemy 스크립트 초기화 (경로 탐색 시작점과 목표 전달)
        EnemyObject enemy = enemyInstance.GetComponent<EnemyObject>();
        if (enemy != null)
        {
            enemy.Initialize(spawnTilePosition, goalTilePosition);
        }
        else
        {
            Debug.LogError("적 프리팹에 Enemy 스크립트가 없습니다!");
        }
    }

    // 스포너 중지
    public void StopSpawning()
    {
        spawning = false;
    }
}
