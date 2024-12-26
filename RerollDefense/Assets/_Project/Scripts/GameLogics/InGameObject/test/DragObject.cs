using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragObject : MonoBehaviour
{
    private bool isDragging = false;
    private Vector3 offset;
    private Vector3Int previousTilePos;
    private SpriteRenderer spriteRenderer;

    [SerializeField] private Color validColor = Color.green;
    [SerializeField] private Color invalidColor = Color.red;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        isDragging = true;
        offset = transform.position - GetMouseWorldPosition();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mousePos = GetMouseWorldPosition();
        transform.position = mousePos + offset;

        Vector3Int currentTilePos = TileMapManager.Instance.WorldToCell(transform.position);

        if (currentTilePos != previousTilePos)
        {
            bool canPlace = CheckPlacement(currentTilePos);
            UpdateVisual(canPlace);
            previousTilePos = currentTilePos;
        }
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3Int tilePos = TileMapManager.Instance.WorldToCell(transform.position);
        if (CheckPlacement(tilePos))
        {
            PlaceObject(tilePos);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool CheckPlacement(Vector3Int tilePos)
    {
        if (!TileMapManager.Instance.CanPlaceObjectAt(tilePos))
            return false;

        return PathFindingManager.Instance.CanPlaceObstacle(tilePos);
    }

    private void PlaceObject(Vector3Int tilePos)
    {
        transform.position = TileMapManager.Instance.CellToWorld(tilePos);
        TileMapManager.Instance.OccupyTile(tilePos);
        PathFindingManager.Instance.UpdateCurrentPath();
        Destroy(this);
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -Camera.main.transform.position.z;
        return Camera.main.ScreenToWorldPoint(mousePos);
    }

    private void UpdateVisual(bool canPlace)
    {
        spriteRenderer.color = canPlace ? validColor : invalidColor;
    }
}
