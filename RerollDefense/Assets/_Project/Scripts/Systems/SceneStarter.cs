using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
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
        TileMapManager.Instance.InitializeManager();
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

        // UI �ʱ�ȭ
        UIManager.Instance.InitializeUIManager(BGDatabaseEnum.SceneType.Game);

        GameManager.Instance.InitGameManager();
        // ���� ����
        GameManager.Instance.ChangeState(new GamePlayState());
        StageManager.Instance.StartStage(1);
    }

}
