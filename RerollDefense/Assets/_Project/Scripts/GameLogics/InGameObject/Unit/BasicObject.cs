using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicObject : MonoBehaviour
{
    //최상위 오브젝트

    public State currentState;

    //TODO : 어드레서블로 불러와야됨, 수정
    [HideInInspector]
    public string prefabPath;

    public BGId ID; // BGDatabase 고유 식별자

    void Awake()
    {
        Initialize();
        
    }

    public virtual void Initialize()
    {
    }

    public virtual void Update()
    {
    }

    public void ChangeState<T>(T state) where T : State
    {
        currentState?.ExitState(this);
        currentState = state;
        currentState?.EnterState(this);
    }

  
}
