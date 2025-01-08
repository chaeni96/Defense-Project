using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{

    //싱글톤 생성

    public static GameSceneManager _instance;
    public static GameSceneManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameSceneManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("GameSceneManager");
                    _instance = singleton.AddComponent<GameSceneManager>();
                    DontDestroyOnLoad(singleton);
                }
            }

            return _instance;
        }
    }

    //로딩이 필요있는 씬전환
    public void LoadSceneWithLoading(SceneKind type)
    {
        StartCoroutine(LoadSceneAsync(type));
    }


    private IEnumerator LoadSceneAsync(SceneKind type)
    {
        // 씬을 비동기 방식으로 로드
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(GetSceneName(type));

        // 로드가 완료될 때까지 대기
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // 씬 로드가 완료된 후 진행해야될거

    }

    //로딩이 필요없는 씬 전환
    public void LoadScene(SceneKind type)
    {
        SceneManager.LoadScene(GetSceneName(type));
    }

    //씬전환 완료 됐는지 필요

    string GetSceneName(SceneKind type)
    {
        string name = System.Enum.GetName(typeof(SceneKind), type);
        return name;
    }

}
