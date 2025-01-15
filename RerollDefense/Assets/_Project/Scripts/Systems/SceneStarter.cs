using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SceneStarter : MonoBehaviour
{

    //씬별로 바인딩
    public Canvas fullWindowCanvas;
    public Canvas filedCanvas;

    public SceneKind scenekind;

    //GameScene에서만 바인딩
    [SerializeField]private Tilemap placedTileMap;
    [SerializeField] private Transform tileMapGrid;  // Inspector에서 할당



    void Start()
    {

        UIManager.Instance.fullWindowCanvas = fullWindowCanvas;
        UIManager.Instance.fieldUICanvas = filedCanvas;

        //data 로드
        SaveLoadManager.Instance.LoadData();
        ResourceManager.Instance.InitializeManager();
        GameManager.Instance.InitializeManager();
        // UI 초기화
        UIManager.Instance.InitializeManager(scenekind);


        if (scenekind == SceneKind.Lobby)
        {

        }

        if(scenekind == SceneKind.InGame)
        {
            StartCoroutine(InitializeGameScene());
        }


    }

    private IEnumerator InitializeGameScene()
    {
        // 게임씬에 사용할 리소스 로드
        bool prefabsLoaded = false;

        ResourceManager.Instance.LoadAllAsync<GameObject>("InGameScenePrefabs", (key, count, total) =>
        {
            if (count == total)
                prefabsLoaded = true;
        });
        yield return new WaitUntil(() => prefabsLoaded);


        // 기본 매니저 초기화
        PoolingManager.Instance.InitializeManager();
        UnitManager.Instance.InitializeManager();
        EnemyManager.Instance.InitializeMnanager();
        ProjectileManager.Instance.InitializeManager();


        StageManager.Instance.InitializeManager(placedTileMap, tileMapGrid);
        StatManager.Instance.InitializeManager();
        //TODO : 로비에서 선택한 스테이지를 인자값으로 넘겨줘야됨

        StageManager.Instance.StartStage(1);

        // 게임 시작
        GameManager.Instance.ChangeState(new GamePlayState());

    }

}
