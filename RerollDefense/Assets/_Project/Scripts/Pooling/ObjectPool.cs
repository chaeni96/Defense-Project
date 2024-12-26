using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public string objName { get; private set; }
    private string addressablePath; //������ ��巹���� �ּ�
    public int initialPoolSize = 10; // �ʱ� Ǯ ũ��, ������Ʈ Ǯ �����Ϳ��� ���� ����
    private Queue<PooledObject> availableObjects; // ��� ������ ������Ʈ ť

    // ������Ʈ Ǯ �ʱ�ȭ
    public void InitializePool(string objectName, string prefabPath, int initialPoolSize)
    {
        availableObjects = new Queue<PooledObject>();
        objName = objectName;
        addressablePath = prefabPath;
        this.initialPoolSize = initialPoolSize;
        gameObject.name = $"{objName}_Pool"; // Ǯ �̸� ����

        // ������ Ǯ ũ�⸸ŭ ������Ʈ ������ ť�� �߰�
        for (int i = 0; i < initialPoolSize; i++)
        {
            availableObjects.Enqueue(CreateObject());
        }
    }

    // ������Ʈ Ǯ���� ������Ʈ ��������
    public GameObject GetPooledObject()
    {
        if (availableObjects.Count > 0)
        {
            var pooledObject = availableObjects.Dequeue();
            return pooledObject.gameObject; //ť���� ������ ������Ʈ ��ȯ
        }
        else
        {
            // ��� ������ ������Ʈ�� ������ ���� ����
            return CreateObject().gameObject;
        }
    }

    // �� ������Ʈ ����, �ش� ������ �ν��Ͻ�ȭ�Ͽ� ������ �� ������Ʈ ��Ȱ�� ���·� ��ȯ
    public PooledObject CreateObject()
    {
        GameObject obj = ResourceManager.Instance.Instantiate(addressablePath, transform);

        var pooledObj = obj.AddComponent<PooledObject>();
        pooledObj.objectName = objName;
        obj.SetActive(false);
        return pooledObj;
    }

    // ������Ʈ�� Ǯ�� ��ȯ
    public void ReturnToPool(GameObject obj)
    {
        PooledObject pooledObject = obj.GetComponent<PooledObject>();
        if (pooledObject != null)
        {
            pooledObject.OnReturnToPool(); // ������Ʈ�� Ǯ�� ���ư� �� ������ ����, layer ����Ʈ�� ����, ������Ʈ ��Ȱ��ȭ
            if (pooledObject.transform.parent != this)
            {
                pooledObject.transform.SetParent(transform); // Ǯ�� �ڽ����� ����
            }
            pooledObject.transform.position = Vector3.zero; // ��ġ �ʱ�ȭ
            availableObjects.Enqueue(pooledObject); // ��� ������ ������Ʈ ť�� �߰�
        }
    }

    // Ǯ �ı�
    public void DestroyPool()
    {
        // availableObjects ť�� �ִ� ��� ������Ʈ ����
        while (availableObjects.Count > 0)
        {
            PooledObject obj = availableObjects.Dequeue();
            Destroy(obj.gameObject);
        }
        // ������Ʈ Ǯ ���� ������Ʈ ����
        Destroy(gameObject);
    }
}
