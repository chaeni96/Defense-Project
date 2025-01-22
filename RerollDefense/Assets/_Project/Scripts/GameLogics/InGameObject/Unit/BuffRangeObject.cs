using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BuffRangeObject : MonoBehaviour
{
    [SerializeField] private CircleCollider2D rangeCollider;
    [SerializeField] private SpriteRenderer rangeIndicator;
    private BasicObject ownerObject;
    private RangeBuff parentBuff;
    private List<Collider2D> overlappedColliders = new List<Collider2D>();
    private HashSet<BasicObject> currentObjects = new HashSet<BasicObject>();
    private ContactFilter2D filter;

    public void Initialize(RangeBuff buff, float radius, BasicObject owner)
    {
        parentBuff = buff;
        ownerObject = owner;

        rangeCollider.radius = radius;
        if (rangeIndicator != null)
        {
            rangeIndicator.transform.localScale = new Vector3(radius * 2, radius * 2, 1);
        }

        filter = new ContactFilter2D();
        filter.useTriggers = true;
    }

    // ���� üũ�� �޼��� �߰�
    public void CheckRangeObjects()
    {
        overlappedColliders.Clear();
        rangeCollider.OverlapCollider(filter, overlappedColliders);

        HashSet<BasicObject> newObjects = new HashSet<BasicObject>();

        // ���� ���� ���� ������Ʈ�� Ȯ��
        foreach (var collider in overlappedColliders)
        {
            var targetObject = collider.GetComponent<BasicObject>();
            if (targetObject != null && targetObject.isEnemy != ownerObject.isEnemy)
            {
                newObjects.Add(targetObject);
                if (!currentObjects.Contains(targetObject))
                {
                    parentBuff.AddObjectInRange(targetObject);
                }
            }
        }

        // ������ ��� ������Ʈ�� ó��
        foreach (var obj in currentObjects)
        {
            if (!newObjects.Contains(obj))
            {
                parentBuff.RemoveObjectInRange(obj);
            }
        }

        currentObjects = newObjects;
    }

    private void OnDisable()
    {
        ownerObject = null;
        parentBuff = null;
    }

}
