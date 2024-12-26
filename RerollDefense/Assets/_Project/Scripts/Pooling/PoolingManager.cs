using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor.U2D.Animation;

public class PoolingManager : MonoBehaviour
{
    public static PoolingManager _instance;

    private Dictionary<string, ObjectPool> poolDictionary = new Dictionary<string, ObjectPool>(); // ID와 해당 오브젝트 풀을 매핑하는 딕셔너리

    // 싱글톤 패턴 구현
    public static PoolingManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<PoolingManager>();

                // 인스턴스가 없으면 새로운 게임 오브젝트를 생성하여 PoolingManager 컴포넌트를 추가
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("PoolingManager");
                    _instance = singleton.AddComponent<PoolingManager>();
                    DontDestroyOnLoad(singleton); // 씬이 변경되어도 파괴되지 않도록 설정
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

    }


    public void InitializeAllPools()
    {
        var poolDataList = D_ObjectPoolData.FindEntities(data => true);

        foreach (var poolData in poolDataList)
        {
            if (!string.IsNullOrEmpty(poolData.f_AdrressablePath))
            {
                CreatePool(poolData.f_name, poolData.f_AdrressablePath, poolData.f_initialSize);
            }
        }
    }

    private void CreatePool(string name, string addressablePath, int initialSize)
    {
        if (!poolDictionary.ContainsKey(name))
        {
            GameObject poolGO = new GameObject($"{name}_Pool");
            poolGO.transform.SetParent(transform);

            ObjectPool pool = poolGO.AddComponent<ObjectPool>();
            pool.InitializePool(name, addressablePath, initialSize);
            poolDictionary.Add(name, pool);
        }
    }

    public GameObject GetObject(string id, Vector3? position = null, int layer = 0)
    {
        if (!poolDictionary.ContainsKey(id))
        {
            var poolData = D_ObjectPoolData.GetEntity(id);
            if (poolData != null)
            {
                CreatePool(poolData.f_name, poolData.f_AdrressablePath, poolData.f_initialSize);
            }
            else
            {
                Debug.LogError($"No pool data found for ID: {id}");
                return null;
            }
        }

        GameObject obj = poolDictionary[id].GetPooledObject();

        if (obj == null)
        {
            // 오브젝트 풀에서 사용 가능한 객체를 얻지 못한 경우
            Debug.LogWarning($"Could not retrieve object from pool for ID: {id}");

            // 대체 방안: 직접 인스턴스화
            var poolData = D_ObjectPoolData.GetEntity(id);
            if (poolData != null)
            {
                obj = ResourceManager.Instance.Instantiate(poolData.f_AdrressablePath);
            }

            if (obj == null)
            {
                Debug.LogError($"Failed to create object for ID: {id}");
                return null;
            }
        }

        // position이 null이 아닌 경우에만 위치 설정
        if (position.HasValue)
        {
            obj.transform.position = position.Value;
        }

        obj.layer = layer;
        obj.SetActive(true);

        return obj;
    }
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        PooledObject pooledObj = obj.GetComponent<PooledObject>();
        if (pooledObj != null && poolDictionary.ContainsKey(pooledObj.objectName))
        {
            poolDictionary[pooledObj.objectName].ReturnToPool(obj);
        }
    }

    private void OnDestroy()
    {
        if (poolDictionary != null)
        {
            foreach (var pool in poolDictionary.Values)
            {
                if (pool != null)
                    pool.DestroyPool();
            }
            poolDictionary.Clear();
        }
        _instance = null;
    }
}