using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicObject : MonoBehaviour
{
    //최상위 오브젝트

    //FSM 사용할거라 ChangeState, update 필요

    public State currentState;


    //모든 오브젝트에 들어갈만한 변수 name, position, ObjId
    //Data로 받아와야함
    [HideInInspector]
    public string objName;
    [HideInInspector]
    public int objCost;

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
        currentState?.UpdateState(this);
    }

    public void ChangeState<T>(T state) where T : State
    {
        currentState?.ExitState(this);
        currentState = state;
        currentState?.EnterState(this);
    }

  
}
