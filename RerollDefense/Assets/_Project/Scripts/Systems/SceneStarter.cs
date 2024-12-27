using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
    void Start()
    {
        int totalCount = 0;
        int currentCount = 0;

        //Prefabs에 있는 모든 리소스 로드
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, total) =>
        {
            currentCount = count;
            totalCount = total;

            Debug.Log($"{key} {count} / {total}");

            // 모든 리소스 로드가 완료되면
            if (currentCount == totalCount)
            {
                PoolingManager.Instance.InitializeAllPools();

                //stage 1부터 -> 나중에 변수로 바꾸기
                StageManager.Instance.StartStage(1);
                
            }
        });

        
    }
}
