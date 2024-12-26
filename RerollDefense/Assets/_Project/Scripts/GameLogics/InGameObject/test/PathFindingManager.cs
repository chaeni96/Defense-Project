using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{
    public static PathFindingManager _instance;

    [SerializeField] private Vector3Int startTilePosition;
    [SerializeField] private Vector3Int endTilePosition;

    private List<Vector3> currentPath = new List<Vector3>();
    private List<Vector3> tempPath = new List<Vector3>();

    public bool allowDiagonal = true; // 대각선 이동 허용 여부
    public bool dontCrossCorner = true; // 코너 통과 금지 여부

    public static PathFindingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PathFindingManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("PathFindingManager");
                    _instance = singleton.AddComponent<PathFindingManager>();
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

        currentPath = FindPath(startTilePosition, endTilePosition);
    }

    public bool CanPlaceObstacle(Vector3Int obstaclePos)
    {
        TileMapManager.Instance.AddTempOccupied(obstaclePos);
        tempPath = FindPath(startTilePosition, endTilePosition);
        TileMapManager.Instance.RemoveTempOccupied(obstaclePos);
        return tempPath.Count > 0;
    }

    public void UpdateCurrentPath()
    {
        currentPath = tempPath;
        EnemyManager.Instance.UpdateEnemiesPath(currentPath);
    }

    public List<Vector3> GetCurrentPath()
    {
        return currentPath;
    }

    private List<Vector3> FindPath(Vector3Int start, Vector3Int goal)
    {
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        Node startNode = new Node(start);
        Node goalNode = new Node(goal);
        openList.Add(startNode);

        while (openList.Count > 0)
        {
            Node currentNode = GetLowestFCostNode(openList);

            if (currentNode.Equals(goalNode))
            {
                return RetracePath(startNode, currentNode);
            }

            openList.Remove(currentNode);
            closedList.Add(currentNode);

            foreach (Vector3Int neighborPosition in GetNeighbors(currentNode.Position))
            {
                if (!TileMapManager.Instance.IsValidTile(neighborPosition) ||
                    TileMapManager.Instance.IsTileOccupied(neighborPosition) ||
                    closedList.Contains(new Node(neighborPosition)))
                {
                    continue;
                }

                if (allowDiagonal && dontCrossCorner)
                {
                    if (IsDiagonalMoveBlocked(currentNode.Position, neighborPosition))
                    {
                        continue;
                    }
                }

                Node neighborNode = new Node(neighborPosition);
                float tentativeGCost = currentNode.GCost +
                    (currentNode.Position.x != neighborPosition.x && currentNode.Position.y != neighborPosition.y ? 1.414f : 1.0f);

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

        return new List<Vector3>();
    }

    private List<Vector3> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3> path = new List<Vector3>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(TileMapManager.Instance.CellToWorld(currentNode.Position));
            currentNode = currentNode.Parent;
        }
        path.Add(TileMapManager.Instance.CellToWorld(startNode.Position));
        path.Reverse();

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

    private bool IsDiagonalMoveBlocked(Vector3Int current, Vector3Int neighbor)
    {
        Vector3Int delta = neighbor - current;

        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            Vector3Int side1 = new Vector3Int(current.x + delta.x, current.y, 0);
            Vector3Int side2 = new Vector3Int(current.x, current.y + delta.y, 0);

            if (!TileMapManager.Instance.IsValidTile(side1) ||
                !TileMapManager.Instance.IsValidTile(side2) ||
                TileMapManager.Instance.IsTileOccupied(side1) ||
                TileMapManager.Instance.IsTileOccupied(side2))
            {
                return true;
            }
        }

        return false;
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
}