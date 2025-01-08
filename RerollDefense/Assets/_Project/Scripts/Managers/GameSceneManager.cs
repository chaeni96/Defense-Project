using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{

    //�̱��� ����

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

    //�ε��� �ʿ��ִ� ����ȯ
    public void LoadSceneWithLoading(SceneKind type)
    {
        StartCoroutine(LoadSceneAsync(type));
    }


    private IEnumerator LoadSceneAsync(SceneKind type)
    {
        // ���� �񵿱� ������� �ε�
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(GetSceneName(type));

        // �ε尡 �Ϸ�� ������ ���
        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // �� �ε尡 �Ϸ�� �� �����ؾߵɰ�

    }

    //�ε��� �ʿ���� �� ��ȯ
    public void LoadScene(SceneKind type)
    {
        SceneManager.LoadScene(GetSceneName(type));
    }

    //����ȯ �Ϸ� �ƴ��� �ʿ�

    string GetSceneName(SceneKind type)
    {
        string name = System.Enum.GetName(typeof(SceneKind), type);
        return name;
    }

}
