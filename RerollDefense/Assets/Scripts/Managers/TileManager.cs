using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class TileManager : MonoBehaviour
{
    public static TileManager _instance;

    public Tilemap tileMap;
    private Dictionary<Vector3Int, TileData> tileDataMap = new Dictionary<Vector3Int, TileData>();

    public static TileManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TileManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("TileManager");
                    _instance = singleton.AddComponent<TileManager>();
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

        setTileMap();
    }

    //만들어둔 타일맵 초기세팅
    public void setTileMap()
    {
        //타일 데이터 초기화, 모든 타일 배치가능 상태로 설정

        foreach(var position in tileMap.cellBounds.allPositionsWithin) //타일맵의 범위 내 모든 셀 좌표
        {
            //해당 좌표에 타일이 실제 존재하면 배치가능 상태로 변경
            if(tileMap.HasTile(position))
            {
                tileDataMap[position] = new TileData();
            }

        }

        ResetTileColors(new Color(1,1,1,0));

    }

    //특정 타일의 데이터 가지고오기
    public TileData GetTileData(Vector3Int position)
    {
        return tileDataMap.ContainsKey(position) ? tileDataMap[position] : null;
    }

    //특정 타일 배치 가능한지 확인

    public bool IsTileAvailable(Vector3Int postion)
    {
        TileData tileData = GetTileData(postion);


        if(tileData != null && tileData.isAvailable)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //특정 타일색 변경
    public void SetTileColor(Vector3Int position, Color color)
    {
        if(tileMap.HasTile(position))
        {
            tileMap.SetTileFlags(position, TileFlags.None);

            tileMap.SetColor(position, color);
        }
    }


    //모든 타일색 초기화
    public void ResetTileColors(Color color)
    {
        foreach(var position in tileMap.cellBounds.allPositionsWithin)
        {
            if(tileMap.HasTile(position))
            {
                tileMap.SetTileFlags(position, TileFlags.None);
                tileMap.SetColor(position, color); //투명으로 바꾸기
            }
        }
    }

    //특정 타일 배치 불가능 상태로 변경
    public void SetTileUnavailable(Vector3Int position)
    {
        if(tileDataMap.ContainsKey(position))
        {
            tileDataMap[position].isAvailable = false;
        }
    }

}
