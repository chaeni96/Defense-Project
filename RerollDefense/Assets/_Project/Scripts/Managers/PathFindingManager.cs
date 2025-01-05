using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFindingManager : MonoBehaviour
{

    //경로 찾기만 담당
    public static PathFindingManager _instance;

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

    }


    //enemy 현재의 position에서 endTile까지 최단거리 구하기
    public List<Vector3> FindPathFromPosition(Vector3 worldPosition)
    {
        // 월드 좌표를 타일 좌표로 변환
        Vector3Int startPos = TileMapManager.Instance.tileMap.WorldToCell(worldPosition);
        return FindPath(startPos, TileMapManager.Instance.GetEndTilePosition());
    }


    public bool HasValidPath(Vector3Int start, Vector3Int end)
    {
        var path = FindPath(start, end);
        return path.Count > 0;
    }

    //경로찾기 메서드
    public List<Vector3> FindPath(Vector3Int start, Vector3Int goal)
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
                // 타일 유효성 검사
                var tileData = TileMapManager.Instance.GetTileData(neighborPosition);
                if (tileData == null || !tileData.isAvailable ||
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
                    (IsDiagonalMove(currentNode.Position, neighborPosition) ? 1.414f : 1.0f);

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
            path.Add(TileMapManager.Instance.tileMap.GetCellCenterWorld(currentNode.Position));
            currentNode = currentNode.Parent;
        }
        path.Add(TileMapManager.Instance.tileMap.GetCellCenterWorld(startNode.Position));
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
            neighbors.Add(position + new Vector3Int(1, 1, 0)); 
            neighbors.Add(position + new Vector3Int(-1, 1, 0)); 
            neighbors.Add(position + new Vector3Int(1, -1, 0)); 
            neighbors.Add(position + new Vector3Int(-1, -1, 0)); 
        }

        return neighbors;
    }

    private bool IsDiagonalMove(Vector3Int from, Vector3Int to)
    {
        return from.x != to.x && from.y != to.y;
    }


    private bool IsDiagonalMoveBlocked(Vector3Int current, Vector3Int neighbor)
    {
        Vector3Int delta = neighbor - current;

        if (Mathf.Abs(delta.x) == 1 && Mathf.Abs(delta.y) == 1)
        {
            Vector3Int side1 = new Vector3Int(current.x + delta.x, current.y, 0);
            Vector3Int side2 = new Vector3Int(current.x, current.y + delta.y, 0);

            var tileData1 = TileMapManager.Instance.GetTileData(side1);
            var tileData2 = TileMapManager.Instance.GetTileData(side2);

            if (tileData1 == null || !tileData1.isAvailable ||
                tileData2 == null || !tileData2.isAvailable)
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