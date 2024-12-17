using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
    // test용
    void Start()
    {
        //Prefabs에 있는 모든 리소스 로드
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, totalCount) =>
        {
            Debug.Log($"{key} {count} / {totalCount}");
        });
    }

}
