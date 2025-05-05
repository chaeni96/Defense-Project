using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PooledObject : MonoBehaviour
{
    public string objectName { get; set; }
    public bool IsPooled;
    public void OnReturnToPool()
    {
        IsPooled = true;
        gameObject.layer = 0; //defaultLayer
        gameObject.SetActive(false);
    }
}