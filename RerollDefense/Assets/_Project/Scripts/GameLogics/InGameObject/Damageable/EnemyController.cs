using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyController : DamageableObject
{
    public float moveSpeed; // 이동 속도

    private List<Vector3> path; // 경로 리스트
    private int currentPathIndex = 0; // 현재 이동 중인 경로 인덱스

    public override void Initialize()
    {
        base.Initialize();
    }

    //test용
    public void SetEnemyPath(Vector3Int startTilePosition, Vector3Int goalTilePosition)
    {
        // 경로 탐색 실행
        path = DemoPathfindingManager.Instance.FindPath(startTilePosition, goalTilePosition);
        HP = maxHP;

        if (path == null || path.Count == 0)
        {
            Debug.LogError("경로를 찾을 수 없습니다!");
        }
        else
        {
            StartCoroutine(FollowPath());
        }
    }

    public override void Update()
    {
        base.Update();
    }

    private IEnumerator FollowPath()
    {
        while (currentPathIndex < path.Count)
        {
            Vector2 targetPosition = (Vector2)path[currentPathIndex]; //목표지점

            while (Vector3.Distance(myBody.position, targetPosition) > 0.1f)
            {
                Vector2 direction = (targetPosition - myBody.position).normalized; // 이동 방향
                myBody.MovePosition(myBody.position + direction * Time.fixedDeltaTime * moveSpeed);

                // 경로 재탐색이 필요한 경우
                if (ShouldRecalculatePath())
                {
                    RecalculatePath();
                    yield break; // 기존 코루틴 종료 후 재탐색된 경로로 이동 시작
                }

                yield return new WaitForFixedUpdate(); // FixedUpdate와 동기화
            }

            // 다음 지점으로 이동
            currentPathIndex++;
        }

        // 목표에 도달한 경우
        OnReachGoal();
    }

    private bool ShouldRecalculatePath()
    {
        // 현재 경로가 막혔는지 확인
        if (currentPathIndex < path.Count)
        {
            Vector3Int currentTile = DemoTileMapManager.Instance.tileMap.WorldToCell(myBody.position);
            Vector3Int nextTile = DemoTileMapManager.Instance.tileMap.WorldToCell(path[currentPathIndex]);

            // 현재 또는 다음 타일이 유효하지 않으면 경로 재탐색 필요
            if (!(DemoTileMapManager.Instance.GetTileData(currentTile)?.isAvailable ?? false) ||
     !(DemoTileMapManager.Instance.GetTileData(nextTile)?.isAvailable ?? false))
            {
                return true;
            }
        }

        return false;
    }

    private void RecalculatePath()
    {
        Vector3Int currentTile = DemoTileMapManager.Instance.tileMap.WorldToCell(myBody.position);
        Vector3Int goalTile = DemoTileMapManager.Instance.tileMap.WorldToCell(path[path.Count - 1]);

        // 새로운 경로 계산
        List<Vector3> newPath = DemoPathfindingManager.Instance.FindPath(currentTile, goalTile);

        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            currentPathIndex = 0; // 경로 초기화
            StartCoroutine(FollowPath()); // 새로운 경로로 이동 시작
        }
        else
        {
            Debug.LogError("경로를 재탐색할 수 없습니다!");
        }
    }

    private void OnReachGoal()
    {
        // 목표 지점에 도달했을 때 처리
        Debug.Log("적이 목표에 도달했습니다!");

        // 예: 적 제거
        Destroy(gameObject);
    }
}
