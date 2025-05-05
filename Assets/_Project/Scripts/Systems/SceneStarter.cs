using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SceneStarter : MonoBehaviour
{

    //������ ���ε�
    public Canvas fullWindowCanvas;
    public Canvas filedCanvas;

    public SceneKind scenekind;

    //GameScene������ ���ε�
    [SerializeField]private Tilemap placedTileMap;
    [SerializeField] private Transform tileMapGrid;  // Inspector���� �Ҵ�


    // ���� BGM ����
    [SerializeField] private string bgmAddressableKey;


    void Start()
    {

        UIManager.Instance.fullWindowCanvas = fullWindowCanvas;
        UIManager.Instance.fieldUICanvas = filedCanvas;

        //data �ε�
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
            // �κ� �ʱ�ȭ �۾��� �ִٸ� ���⼭ ����
        }
        if (scenekind == SceneKind.InGame)
        {
            yield return StartCoroutine(InitializeGameScene());
        }

        if (!string.IsNullOrEmpty(bgmAddressableKey))
        {
            AudioManager.Instance.PlayBGM(bgmAddressableKey);
        }

        // ��� �ʱ�ȭ�� ���� �� UI �ʱ�ȭ

        UIManager.Instance.InitUIForScene(scenekind);
    }

    private IEnumerator InitializeGameScene()
    {
        // ���Ӿ��� ����� ���ҽ� �ε�
        bool prefabsLoaded = false;

        ResourceManager.Instance.LoadAllAsync<GameObject>("InGameScenePrefabs", (key, count, total) =>
        {
            if (count == total)
                prefabsLoaded = true;
        });
        yield return new WaitUntil(() => prefabsLoaded);


        // �⺻ �Ŵ��� �ʱ�ȭ
        PoolingManager.Instance.InitializeManager();
        UnitManager.Instance.InitializeManager();
        EnemyManager.Instance.InitializeMnanager();
        ProjectileManager.Instance.InitializeManager();


        StageManager.Instance.InitializeManager(placedTileMap, tileMapGrid);
        StatManager.Instance.InitializeManager();

        GameManager.Instance.LoadSystemStats();


        StageManager.Instance.StartStage(GameManager.Instance.SelectedStageNumber);

        // ���� ����
        GameManager.Instance.ChangeState(new GamePlayState());

    }

}
