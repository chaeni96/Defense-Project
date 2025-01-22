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

    // 범위 체크용 메서드 추가
    public void CheckRangeObjects()
    {
        overlappedColliders.Clear();
        rangeCollider.OverlapCollider(filter, overlappedColliders);

        HashSet<BasicObject> newObjects = new HashSet<BasicObject>();

        // 현재 범위 안의 오브젝트들 확인
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

        // 범위를 벗어난 오브젝트들 처리
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
