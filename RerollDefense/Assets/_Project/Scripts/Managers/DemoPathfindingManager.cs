using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoPathfindingManager : MonoBehaviour
{
    public static DemoPathfindingManager _instance;
    public static DemoPathfindingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DemoPathfindingManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("PathfindingManager");
                    _instance = singleton.AddComponent<DemoPathfindingManager>();
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
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private class Node
    {
        public Vector3Int Position;
        public Node Parent;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;

        public Node(Vector3Int position)
        {
            Position = position;
        }

        public override bool Equals(object obj)
        {
            return obj is Node node && Position == node.Position;
        }

        public override int GetHashCode()
        {
            return Position.GetHashCode();
        }
    }

    public bool allowDiagonal = true; // �밢�� �̵� ��� ����
    public bool dontCrossCorner = true; // �ڳ� ��� ���� ����

    public List<Vector3> FindPath(Vector3Int start, Vector3Int goal)
    {
        // A* �˰��� ����
        List<Vector3> path = new List<Vector3>();

        // OpenList�� ClosedList �ʱ�ȭ
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        // ���� ��� �߰�
        Node startNode = new Node(start);
        Node goalNode = new Node(goal);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // F ���� ���� ���� ��� ����
            Node currentNode = GetLowestFCostNode(openList);

            // ��ǥ ������ ������ ��� ��� ��ȯ
            if (currentNode.Equals(goalNode))
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // �̿� ��� Ž��
            foreach (Vector3Int neighborPosition in GetNeighbors(currentNode.Position))
            {
                // �̵� ���� ���� Ȯ��
                if (!TileMapManager.Instance.GetTileData(neighborPosition)?.isAvailable == true ||
                    closedList.Contains(new Node(neighborPosition)))
                {
                    continue;
                }

                // �밢�� �̵��� ���� �߰� ����
                if (allowDiagonal && dontCrossCorner)
                {
                    if (IsDiagonalMoveBlocked(currentNode.Position, neighborPosition))
                    {
                        continue; // �ڳ� ��� �Ұ� �� �̵� ����
                    }
                }

                Node neighborNode = new Node(neighborPosition);
                float tentativeGCost = currentNode.GCost + (currentNode.Position.x != neighborPosition.x && currentNode.Position.y != neighborPosition.y ? 1.414f : 1.0f);

                if (!openList.Contains(neighborNode) || tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = Vector3Int.Distance(neighborPosition, goal);
                    neighborNode.Parent = currentNode;

                    if (!openList.Contains(neighborNode))
                    {
                        openList.Add(neighborNode);
                    }
                }
            }
        }

        // ��θ� ã�� �� ���� ���
        Debug.LogError("��θ� ã�� �� �����ϴ�!");
        return null;
    }

    private bool IsDiagonalMoveBlocked(Vector3Int current, Vector3Int neighbor)
    {
        Vector3Int delta = neighbor - current;

        // �밢������ Ȯ��
        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            // �밢�� �̵� �� �� ������ ���� Ÿ���� ��� ��� �־�� ��
            Vector3Int side1 = new Vector3Int(current.x + delta.x, current.y, 0);
            Vector3Int side2 = new Vector3Int(current.x, current.y + delta.y, 0);

            if (!TileMapManager.Instance.GetTileData(side1)?.isAvailable == true ||
                !TileMapManager.Instance.GetTileData(side2)?.isAvailable == true)
            {
                return true; // �밢�� �̵� ����
            }
        }

        return false;
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(TileMapManager.Instance.tileMap.GetCellCenterWorld(currentNode.Position));
            currentNode = currentNode.Parent;
        }

        path.Reverse(); // ��θ� �������� ��ȯ
        return path;
    }

    private Node GetLowestFCostNode(List<Node> nodes)
    {
        Node lowestFCostNode = nodes[0];

        foreach (Node node in nodes)
        {
            if (node.FCost < lowestFCostNode.FCost)
            {
                lowestFCostNode = node;
            }
        }

        return lowestFCostNode;
    }

    private List<Vector3Int> GetNeighbors(Vector3Int position)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            position + Vector3Int.up,
            position + Vector3Int.down,
            position + Vector3Int.left,
            position + Vector3Int.right
        };

        if (allowDiagonal)
        {
            neighbors.Add(position + new Vector3Int(1, 1, 0)); // ��
            neighbors.Add(position + new Vector3Int(-1, 1, 0)); // ��
            neighbors.Add(position + new Vector3Int(1, -1, 0)); // ��
            neighbors.Add(position + new Vector3Int(-1, -1, 0)); // ��
        }

        return neighbors;
    }
}
