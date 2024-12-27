using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Enemy : DamageableObject
{
    public float3 StartPosition { get; set; }
    public float3 EndPosition { get; set; }
    public float Speed { get; set; }

    public string objectName;

    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update()
    {
        base.Update();
    }

    public void UpdatePosition(float3 newPosition)
    {
        transform.position = new Vector3(newPosition.x, newPosition.y, newPosition.z);
    }
}
