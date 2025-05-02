using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{

    //경로 찾기만 담당
    public static PathFindingManager _instance;

    public bool allowDiagonal = false; // 대각선 이동 허용 여부

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

    }

    public bool HasValidPath(Vector2 start, Vector2 end)
    {
        var path = FindPath(start, end);
        return path.Count > 0;
    }


    //경로찾기 메서드
    public List<Vector3> FindPath(Vector2 start, Vector2 end)
    {
        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        Node startNode = new Node(start);
        Node goalNode = new Node(end);
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

            foreach (Vector2 neighborPosition in GetNeighbors(currentNode.Position))
            {
                var tileData = TileMapManager.Instance.GetTileData(neighborPosition);
                // 타일이 없거나 지나갈 수 없는 타일이면 스킵
                if (tileData == null || !tileData.isAvailable || closedList.Contains(new Node(neighborPosition)))
                {
                    continue;
                }

                if (allowDiagonal)
                {
                    if (IsDiagonalMoveBlocked(currentNode.Position, neighborPosition))
                    {
                        continue;
                    }
                }

                Node neighborNode = new Node(neighborPosition);
                float tentativeGCost = currentNode.GCost +
                     (IsDiagonalMove(currentNode.Position, neighborPosition) ? 14 : 10);

                if (!openList.Contains(neighborNode) || tentativeGCost < neighborNode.GCost)
                {
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.HCost = Vector2.Distance(neighborPosition, end);
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
            path.Add(TileMapManager.Instance.GetTileToWorldPosition(currentNode.Position));
            currentNode = currentNode.Parent;
        }
        path.Add(TileMapManager.Instance.GetTileToWorldPosition(startNode.Position));
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

    private List<Vector2> GetNeighbors(Vector2 position)
    {
        List<Vector2> neighbors = new List<Vector2>();

        // 직선 방향을 먼저 추가
        neighbors.Add(position + Vector2.up);
        neighbors.Add(position + Vector2.right);
        neighbors.Add(position + Vector2.down);
        neighbors.Add(position + Vector2.left);

        // 대각선은 나중에 추가
        if (allowDiagonal)
        {
            neighbors.Add(position + new Vector2(1, 1));
            neighbors.Add(position + new Vector2(-1, 1));
            neighbors.Add(position + new Vector2(1, -1));
            neighbors.Add(position + new Vector2(-1, -1));
        }

        return neighbors;
    }
    private bool IsDiagonalMove(Vector2 from, Vector2 to)
    {
        return from.x != to.x && from.y != to.y;
    }

    private bool IsDiagonalMoveBlocked(Vector2 current, Vector2 neighbor)
    {
        Vector2 delta = neighbor - current;

        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            Vector2 side1 = current + new Vector2(delta.x, 0);
            Vector2 side2 = current + new Vector2(0, delta.y);

            var tileData1 = TileMapManager.Instance.GetTileData(side1);
            var tileData2 = TileMapManager.Instance.GetTileData(side2);

            // dontCrossCorner가 true일 때는 하나라도 막혀있으면 안됨
            // false일 때는 둘 다 막혀있을 때만 안됨
           
          
           return (tileData1 == null || !tileData1.isAvailable) && (tileData2 == null || !tileData2.isAvailable);
            
        }
        return false;
    }

    private class Node
    {
        public Vector2 Position;
        public Node Parent;
        public float GCost;
        public float HCost;
        public float FCost => GCost + HCost;

        public Node(Vector2 position)
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