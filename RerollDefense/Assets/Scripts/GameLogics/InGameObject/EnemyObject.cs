using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyObject : MonoBehaviour
{
    public float speed = 0.5f; // 이동 속도

    private List<Vector3> path; // 경로 리스트
    private int currentPathIndex = 0; // 현재 이동 중인 경로 인덱스

    public void Initialize(Vector3Int startTilePosition, Vector3Int goalTilePosition)
    {
        // 경로 탐색 실행
        path = PathfindingManager.Instance.FindPath(startTilePosition, goalTilePosition);

        if (path == null || path.Count == 0)
        {
            Debug.LogError("경로를 찾을 수 없습니다!");
        }
        else
        {
            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator FollowPath()
    {
        while (currentPathIndex < path.Count)
        {
            Vector3 targetPosition = path[currentPathIndex];

            // 현재 위치에서 목표 지점으로 이동
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }

            // 다음 지점으로 이동
            currentPathIndex++;
        }

        // 목표에 도달한 경우
        OnReachGoal();
    }

    private void OnReachGoal()
    {
        // 목표 지점에 도달했을 때 처리
        Debug.Log("적이 목표에 도달했습니다!");

        // 예: 적 제거
        Destroy(gameObject);
    }
}
