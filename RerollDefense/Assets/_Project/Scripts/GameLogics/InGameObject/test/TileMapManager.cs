using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileMapManager : MonoBehaviour
{


    public static TileMapManager _instance;


    [SerializeField] private Tilemap groundTilemap;
    private HashSet<Vector3Int> occupiedTiles = new HashSet<Vector3Int>();
    private HashSet<Vector3Int> tempOccupiedTiles = new HashSet<Vector3Int>();
    
    public static TileMapManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TileMapManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("TileMapManager");
                    _instance = singleton.AddComponent<TileMapManager>();
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

    public bool IsValidTile(Vector3Int position)
    {
        return groundTilemap.HasTile(position);
    }

    public bool IsTileOccupied(Vector3Int position)
    {
        return occupiedTiles.Contains(position) || tempOccupiedTiles.Contains(position);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return groundTilemap.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        return groundTilemap.GetCellCenterWorld(cellPosition);
    }

    public void AddTempOccupied(Vector3Int position)
    {
        if (IsValidTile(position) && !IsTileOccupied(position))
        {
            tempOccupiedTiles.Add(position);
        }
    }

    public void RemoveTempOccupied(Vector3Int position)
    {
        tempOccupiedTiles.Remove(position);
    }

    public bool OccupyTile(Vector3Int position)
    {
        if (IsValidTile(position) && !IsTileOccupied(position))
        {
            occupiedTiles.Add(position);
            return true;
        }
        return false;
    }

    public void FreeTile(Vector3Int position)
    {
        occupiedTiles.Remove(position);
    }

    public bool CanPlaceObjectAt(Vector3Int position)
    {
        return IsValidTile(position) && !IsTileOccupied(position);
    }
}
