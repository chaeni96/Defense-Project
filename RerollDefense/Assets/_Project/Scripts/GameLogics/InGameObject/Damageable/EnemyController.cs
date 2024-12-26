using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyController : DamageableObject
{
    public float moveSpeed; // �̵� �ӵ�

    private List<Vector3> path; // ��� ����Ʈ
    private int currentPathIndex = 0; // ���� �̵� ���� ��� �ε���

    public override void Initialize()
    {
        base.Initialize();
    }

    //test��
    public void SetEnemyPath(Vector3Int startTilePosition, Vector3Int goalTilePosition)
    {
        // ��� Ž�� ����
        path = DemoPathfindingManager.Instance.FindPath(startTilePosition, goalTilePosition);
        HP = maxHP;

        if (path == null || path.Count == 0)
        {
            Debug.LogError("��θ� ã�� �� �����ϴ�!");
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
            Vector2 targetPosition = (Vector2)path[currentPathIndex]; //��ǥ����

            while (Vector3.Distance(myBody.position, targetPosition) > 0.1f)
            {
                Vector2 direction = (targetPosition - myBody.position).normalized; // �̵� ����
                myBody.MovePosition(myBody.position + direction * Time.fixedDeltaTime * moveSpeed);

                // ��� ��Ž���� �ʿ��� ���
                if (ShouldRecalculatePath())
                {
                    RecalculatePath();
                    yield break; // ���� �ڷ�ƾ ���� �� ��Ž���� ��η� �̵� ����
                }

                yield return new WaitForFixedUpdate(); // FixedUpdate�� ����ȭ
            }

            // ���� �������� �̵�
            currentPathIndex++;
        }

        // ��ǥ�� ������ ���
        OnReachGoal();
    }

    private bool ShouldRecalculatePath()
    {
        // ���� ��ΰ� �������� Ȯ��
        if (currentPathIndex < path.Count)
        {
            Vector3Int currentTile = DemoTileMapManager.Instance.tileMap.WorldToCell(myBody.position);
            Vector3Int nextTile = DemoTileMapManager.Instance.tileMap.WorldToCell(path[currentPathIndex]);

            // ���� �Ǵ� ���� Ÿ���� ��ȿ���� ������ ��� ��Ž�� �ʿ�
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

        // ���ο� ��� ���
        List<Vector3> newPath = DemoPathfindingManager.Instance.FindPath(currentTile, goalTile);

        if (newPath != null && newPath.Count > 0)
        {
            path = newPath;
            currentPathIndex = 0; // ��� �ʱ�ȭ
            StartCoroutine(FollowPath()); // ���ο� ��η� �̵� ����
        }
        else
        {
            Debug.LogError("��θ� ��Ž���� �� �����ϴ�!");
        }
    }

    private void OnReachGoal()
    {
        // ��ǥ ������ �������� �� ó��
        Debug.Log("���� ��ǥ�� �����߽��ϴ�!");

        // ��: �� ����
        Destroy(gameObject);
    }
}
