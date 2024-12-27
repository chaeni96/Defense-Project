using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitController : PlacedObject
{

    public LayerMask targetLayer; // Ÿ������ �� Layer

    public BasicObject targetObject;

    public bool isAttacking;

    [HideInInspector]
    public float attackTimer = 0f;  // Ÿ�̸� �߰�

    public override void Initialize()
    {
        base.Initialize();

    }

    //TODO : ��Ȱ��ȭ �Ҷ� ȣ������ߵ�
    private void OnDestroy()
    {
        UnitManager.Instance.UnregisterUnit(this);
    }
}
