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

    public bool allowDiagonal = true; // 대각선 이동 허용 여부
    public bool dontCrossCorner = true; // 코너 통과 금지 여부

    public List<Vector3> FindPath(Vector3Int start, Vector3Int goal)
    {
        // A* 알고리즘 구현
        List<Vector3> path = new List<Vector3>();

        // OpenList와 ClosedList 초기화
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        // 시작 노드 추가
        Node startNode = new Node(start);
        Node goalNode = new Node(goal);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            // F 값이 가장 낮은 노드 선택
            Node currentNode = GetLowestFCostNode(openList);

            // 목표 지점에 도달한 경우 경로 반환
            if (currentNode.Equals(goalNode))
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            // 이웃 노드 탐색
            foreach (Vector3Int neighborPosition in GetNeighbors(currentNode.Position))
            {
                // 이동 가능 여부 확인
                if (!TileMapManager.Instance.GetTileData(neighborPosition)?.isAvailable == true ||
                    closedList.Contains(new Node(neighborPosition)))
                {
                    continue;
                }

                // 대각선 이동에 대한 추가 검증
                if (allowDiagonal && dontCrossCorner)
                {
                    if (IsDiagonalMoveBlocked(currentNode.Position, neighborPosition))
                    {
                        continue; // 코너 통과 불가 시 이동 차단
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

        // 경로를 찾을 수 없는 경우
        Debug.LogError("경로를 찾을 수 없습니다!");
        return null;
    }

    private bool IsDiagonalMoveBlocked(Vector3Int current, Vector3Int neighbor)
    {
        Vector3Int delta = neighbor - current;

        // 대각선인지 확인
        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            // 대각선 이동 시 두 방향의 직선 타일이 모두 비어 있어야 함
            Vector3Int side1 = new Vector3Int(current.x + delta.x, current.y, 0);
            Vector3Int side2 = new Vector3Int(current.x, current.y + delta.y, 0);

            if (!TileMapManager.Instance.GetTileData(side1)?.isAvailable == true ||
                !TileMapManager.Instance.GetTileData(side2)?.isAvailable == true)
            {
                return true; // 대각선 이동 차단
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

        path.Reverse(); // 경로를 역순으로 반환
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
            neighbors.Add(position + new Vector3Int(1, 1, 0)); // ↘
            neighbors.Add(position + new Vector3Int(-1, 1, 0)); // ↙
            neighbors.Add(position + new Vector3Int(1, -1, 0)); // ↗
            neighbors.Add(position + new Vector3Int(-1, -1, 0)); // ↖
        }

        return neighbors;
    }
}
