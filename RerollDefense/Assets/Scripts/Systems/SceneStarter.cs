using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneStarter : MonoBehaviour
{
    // test��
    void Start()
    {
        //Prefabs�� �ִ� ��� ���ҽ� �ε�
        ResourceManager.Instance.LoadAllAsync<GameObject>("Prefabs", (key, count, totalCount) =>
        {
            Debug.Log($"{key} {count} / {totalCount}");
        });
    }

}
