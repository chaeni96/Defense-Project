using System.Collections;
using _Project.Scripts.KFSM.Runtime.Services;
using AutoBattle.Scripts.DataController;
using AutoBattle.Scripts.Managers;
using Kylin.LWDI;
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


    // 씬별 BGM 설정
    [SerializeField] private string bgmAddressableKey;


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
        UIManager.Instance.InitializeManager();

        if (scenekind == SceneKind.Lobby)
        {
            // 로비 초기화 작업이 있다면 여기서 수행
            CurrencyDataController.Instance.InitializeController();
            RelicDataController.Instance.Initialize();
            AtlasManager.Instance.Initialize();
        }
        if (scenekind == SceneKind.InGame)
        {
            yield return StartCoroutine(InitializeGameScene());
        }

        if (!string.IsNullOrEmpty(bgmAddressableKey))
        {
            AudioManager.Instance.PlayBGM(bgmAddressableKey);
        }

        // 모든 초기화가 끝난 후 UI 초기화

        UIManager.Instance.InitUIForScene(scenekind);
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

        StageManager.Instance.InitializeManager(placedTileMap, tileMapGrid);
        StatManager.Instance.InitializeManager();

        GameManager.Instance.LoadSystemStats();

        DependencyContainer.Instance.Clear();
        DependencyContainer.Instance.Register<LowestHpDetectService, LowestHpDetectService>();
        DependencyContainer.Instance.Register<NearestEnemyDetectService, NearestEnemyDetectService>();


        StageManager.Instance.StartStage(GameManager.Instance.SelectedStageNumber);

        // 게임 시작
        GameManager.Instance.ChangeState(new GamePlayState());

    }
}
