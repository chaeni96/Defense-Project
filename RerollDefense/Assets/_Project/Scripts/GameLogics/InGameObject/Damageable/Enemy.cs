using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Enemy : DamageableObject
{

    public float moveSpeed;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update()
    {
        base.Update();
    }

    public void OnReachEndTile()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(attackPower);
        }
    }
}
