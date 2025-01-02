using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SceneStarter : MonoBehaviour
{
    [SerializeField]private Tilemap placedTileMap;
    [SerializeField] private Transform tileMapGrid;  // Inspector���� �Ҵ�

    void Start()
    {
        StartCoroutine(InitializeGame());
    }

    private IEnumerator InitializeGame()
    {
        // ù ��° ���ҽ� �ε� �Ϸ� ���
        bool prefabsLoaded = false;
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, total) =>
        {
            if (count == total)
                prefabsLoaded = true;
        });
        yield return new WaitUntil(() => prefabsLoaded);

        // �⺻ �Ŵ��� �ʱ�ȭ
        PoolingManager.Instance.InitializeAllPools();
        SaveLoadManager.Instance.LoadData();
        UnitManager.Instance.InitializeManager();
        EnemyManager.Instance.InitializeMnanager();

        // UI ���ҽ� �ε� �Ϸ� ���
        bool uiLoaded = false;
        ResourceManager.Instance.LoadAllAsync<GameObject>("GameSceneUI", (key, count, total) =>
        {
            if (count == total)
                uiLoaded = true;
        });
        yield return new WaitUntil(() => uiLoaded);

        GameManager.Instance.InitGameManager();
        // UI �ʱ�ȭ
        UIManager.Instance.InitializeUIManager(BGDatabaseEnum.SceneType.Game);

        // ���� ����
        GameManager.Instance.ChangeState(new GamePlayState());
        StageManager.Instance.Initialize(placedTileMap, tileMapGrid);
        StageManager.Instance.StartStage(1);
    }

}
