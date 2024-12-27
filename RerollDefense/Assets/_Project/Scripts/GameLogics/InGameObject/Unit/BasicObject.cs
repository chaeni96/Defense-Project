using BansheeGz.BGDatabase;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicObject : MonoBehaviour
{
    //�ֻ��� ������Ʈ

    public State currentState;

    //TODO : ��巹����� �ҷ��;ߵ�, ����
    [HideInInspector]
    public string prefabPath;

    public BGId ID; // BGDatabase ���� �ĺ���

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
