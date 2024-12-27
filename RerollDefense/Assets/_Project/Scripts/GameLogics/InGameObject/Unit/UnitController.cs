using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : PlacedObject
{

    public LayerMask targetLayer; // 타겟으로 할 Layer

    public BasicObject targetObject;

    public bool isAttacking;

    [HideInInspector]
    public float attackTimer = 0f;  // 타이머 추가

    public override void Initialize()
    {
        base.Initialize();

    }

    //TODO : 비활성화 할때 호출해줘야됨
    private void OnDestroy()
    {
        UnitManager.Instance.UnregisterUnit(this);
    }
}
