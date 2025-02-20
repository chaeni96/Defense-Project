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


        StartCoroutine(InitializeScene());

    }

    private IEnumerator InitializeScene()
    {
        if (scenekind == SceneKind.Lobby)
        {
            // 로비 초기화 작업이 있다면 여기서 수행
        }
        if (scenekind == SceneKind.InGame)
        {
            yield return StartCoroutine(InitializeGameScene());
        }

        // 모든 초기화가 끝난 후 UI 초기화
        UIManager.Instance.InitializeManager(scenekind);
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

        GameManager.Instance.LoadSystemStats();


        //TODO : 로비에서 선택한 스테이지를 인자값으로 넘겨줘야됨, episodeInfoUI에서 선택된 스테이지 넘버를 넘겨줘야함

        //StageManager.Instance.StartStage(GameManager.Instance.SelectedEpisodeNumber);

        // 게임 시작
        GameManager.Instance.ChangeState(new GamePlayState());

    }

}
