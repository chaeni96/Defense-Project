using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
    void Start()
    {
        int totalCount = 0;
        int currentCount = 0;

        //Prefabs�� �ִ� ��� ���ҽ� �ε�
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, total) =>
        {
            currentCount = count;
            totalCount = total;

            Debug.Log($"{key} {count} / {total}");

            // ��� ���ҽ� �ε尡 �Ϸ�Ǹ�
            if (currentCount == totalCount)
            {
                //�Ŵ����� �ʱ�ȭ
                PoolingManager.Instance.InitializeAllPools();
                SaveLoadManager.Instance.LoadData();

                TileMapManager.Instance.InitializeManager();
                UnitManager.Instance.InitializeManager();   
                EnemyManager.Instance.InitializeMnanager();

                GameManager.Instance.InitGameManager();
                GameManager.Instance.ChangeState(new GamePlayState());
                StageManager.Instance.StartStage(1);
            }
        });

        
    }
}
