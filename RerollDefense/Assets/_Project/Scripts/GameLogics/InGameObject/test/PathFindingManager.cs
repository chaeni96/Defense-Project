﻿using System.Collections;
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


    public bool CanPlaceObstacle(List<Vector3Int> relativeTiles)
    {
        // 시작, 끝 타일과 겹치는지 확인
        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPos = relativeTile;
            if (checkPos == startTilePosition || checkPos == endTilePosition)
                return false;
        }

        // 임시로 타일들을 점유 상태로 설정
        Dictionary<Vector3Int, TileData> originalTileData = new Dictionary<Vector3Int, TileData>();

        foreach (var relativeTile in relativeTiles)
        {
            Vector3Int checkPos = relativeTile;
            TileData tileData = TileMapManager.Instance.GetTileData(checkPos);
            if (tileData != null)
            {
                originalTileData[checkPos] = new TileData
                {
                    isAvailable = tileData.isAvailable,
                    tileUniqueID = tileData.tileUniqueID
                };
                tileData.isAvailable = false;
            }
        }

        // 경로 찾기 시도
        tempPath = FindPath(startTilePosition, endTilePosition);

        // 타일 상태 복원
        foreach (var kvp in originalTileData)
        {
            TileData tileData = TileMapManager.Instance.GetTileData(kvp.Key);
            if (tileData != null)
            {
                tileData.isAvailable = kvp.Value.isAvailable;
                tileData.tileUniqueID = kvp.Value.tileUniqueID;
            }
        }

        return tempPath.Count > 0;
    }


    public void UpdateCurrentPath()
    {
        currentPath = tempPath;

        EnemyManager.Instance.UpdateEnemiesPath(currentPath);
        Debug.Log($"{currentPath}");
    }

    public List<Vector3> FindPathFromPosition(Vector3 worldPosition)
    {
        // 월드 좌표를 타일 좌표로 변환
        Vector3Int startPos = TileMapManager.Instance.tileMap.WorldToCell(worldPosition);
        return FindPath(startPos, endTilePosition);
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