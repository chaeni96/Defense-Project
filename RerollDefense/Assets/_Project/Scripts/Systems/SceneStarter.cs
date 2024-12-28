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
        // 첫 번째 리소스 로드 완료 대기
        bool prefabsLoaded = false;
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, total) =>
        {
            if (count == total)
                prefabsLoaded = true;
        });
        yield return new WaitUntil(() => prefabsLoaded);

        // 기본 매니저 초기화
        PoolingManager.Instance.InitializeAllPools();
        SaveLoadManager.Instance.LoadData();
        TileMapManager.Instance.InitializeManager();
        UnitManager.Instance.InitializeManager();
        EnemyManager.Instance.InitializeMnanager();

        // UI 리소스 로드 완료 대기
        bool uiLoaded = false;
        ResourceManager.Instance.LoadAllAsync<GameObject>("GameSceneUI", (key, count, total) =>
        {
            if (count == total)
                uiLoaded = true;
        });
        yield return new WaitUntil(() => uiLoaded);

        // UI 초기화
        UIManager.Instance.InitializeUIManager(BGDatabaseEnum.SceneType.Game);

        GameManager.Instance.InitGameManager();
        // 게임 시작
        GameManager.Instance.ChangeState(new GamePlayState());
        StageManager.Instance.StartStage(1);
    }

}
