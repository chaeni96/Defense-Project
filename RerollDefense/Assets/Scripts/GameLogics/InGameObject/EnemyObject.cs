using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyObject : MonoBehaviour
{
    public float speed = 0.5f; // �̵� �ӵ�

    private List<Vector3> path; // ��� ����Ʈ
    private int currentPathIndex = 0; // ���� �̵� ���� ��� �ε���

    public void Initialize(Vector3Int startTilePosition, Vector3Int goalTilePosition)
    {
        // ��� Ž�� ����
        path = PathfindingManager.Instance.FindPath(startTilePosition, goalTilePosition);

        if (path == null || path.Count == 0)
        {
            Debug.LogError("��θ� ã�� �� �����ϴ�!");
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

            // ���� ��ġ���� ��ǥ �������� �̵�
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
                yield return null;
            }

            // ���� �������� �̵�
            currentPathIndex++;
        }

        // ��ǥ�� ������ ���
        OnReachGoal();
    }

    private void OnReachGoal()
    {
        // ��ǥ ������ �������� �� ó��
        Debug.Log("���� ��ǥ�� �����߽��ϴ�!");

        // ��: �� ����
        Destroy(gameObject);
    }
}
