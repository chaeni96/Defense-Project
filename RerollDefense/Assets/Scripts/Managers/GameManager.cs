using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public virtual void EnterState()
    {

    }
    public virtual void UpdateState()
    {

    }
    public virtual void ExitState()
    {

    }
}


public class GameManager : MonoBehaviour
{
    public static GameManager _instance;

    public Camera mainCamera;

    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("GameManager");
                    _instance = singleton.AddComponent<GameManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        SaveLoadManager.Instance.LoadData();

        //TODO : 나중에 레디씬에 넣어주기
        InitGameManager();
    }

    public void InitGameManager()
    {
        mainCamera = Camera.main; 
    }

}
