using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public string objName { get; private set; }
    private string addressableKey; //프리팹 어드레서블키
    public int initialPoolSize = 10; // 초기 풀 크기, 오브젝트 풀 데이터에서 변경 가능
    private Queue<PooledObject> availableObjects; // 사용 가능한 오브젝트 큐

    // 오브젝트 풀 초기화
    public void InitializePool(string prefabKey, int initialPoolSize)
    {
        availableObjects = new Queue<PooledObject>();
        objName = prefabKey;
        addressableKey = prefabKey;
        this.initialPoolSize = initialPoolSize;
        gameObject.name = $"{objName}_Pool"; // 풀 이름 설정

        // 설정한 풀 크기만큼 오브젝트 생성해 큐에 추가
        for (int i = 0; i < initialPoolSize; i++)
        {
            availableObjects.Enqueue(CreateObject());
        }
    }

    // 오브젝트 풀에서 오브젝트 가져오기
    public GameObject GetPooledObject()
    {
        if (availableObjects.Count > 0)
        {
            var pooledObject = availableObjects.Dequeue();
            return pooledObject.gameObject; //큐에서 꺼내온 오브젝트 반환
        }
        else
        {
            // 사용 가능한 오브젝트가 없으면 새로 생성
            return CreateObject().gameObject;
        }
    }

    // 새 오브젝트 생성, 해당 프리팹 인스턴스화하여 생성한 뒤 오브젝트 비활성 상태로 반환
    public PooledObject CreateObject()
    {
        GameObject obj = ResourceManager.Instance.Instantiate(addressableKey, transform);

        var pooledObj = obj.AddComponent<PooledObject>();
        pooledObj.objectName = objName;
        obj.SetActive(false);
        return pooledObj;
    }

    // 오브젝트를 풀로 반환
    public void ReturnToPool(GameObject obj)
    {
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if (pooledObject != null)
        {
            pooledObject.OnReturnToPool(); // 오브젝트가 풀로 돌아갈 때 실행할 로직, layer 디폴트로 설정, 오브젝트 비활성화
            if (pooledObject.transform.parent != this)
            {
                pooledObject.transform.SetParent(transform); // 풀의 자식으로 설정
            }
            pooledObject.transform.position = Vector3.zero; // 위치 초기화
            availableObjects.Enqueue(pooledObject); // 사용 가능한 오브젝트 큐에 추가
        }
    }

    // 풀 파괴
    public void DestroyPool()
    {
        // 큐에 있는 모든 오브젝트의 참조를 리스트로 복사
        List<GameObject> objectsToDestroy = new List<GameObject>();
        while (availableObjects.Count > 0)
        {
            PooledObject obj = availableObjects.Dequeue();
            if (obj != null && obj.gameObject != null)
            {
                objectsToDestroy.Add(obj.gameObject);
            }
        }

        // 큐를 비운 후에 오브젝트들을 파괴
        foreach (var obj in objectsToDestroy)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        // 오브젝트 풀 게임 오브젝트 제거
        Destroy(gameObject);
    }
}
