using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrashCanObject : MonoBehaviour
{
    [SerializeField] private Vector3 initPosition = new Vector3(0f, 0f, 0f); // �⺻ ��ġ
    [SerializeField] private Collider2D myCollider; // �ν����Ϳ��� ���ε�

    public void InitializeTrashCan()
    {
        // ���� �� ��Ȱ��ȭ
        gameObject.SetActive(false);

        // �ݶ��̴��� �Ҵ���� �ʾҴٸ� �ڵ����� ã��
        if (myCollider == null)
        {
            myCollider = GetComponent<Collider2D>();
        }
    }

    // �������� Ȱ��ȭ �� ��ġ ����
    public void Show()
    {
        transform.position = initPosition;
        gameObject.SetActive(true);
    }

    // �������� ��Ȱ��ȭ
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Ư�� ��ġ�� �������� ���� �ȿ� �ִ��� Ȯ��
    public bool IsPositionOver(Vector3 worldPosition)
    {
        //��Ȱ���̾��ְų� �ݶ��̴� ������ �ȵ�
        if (!gameObject.activeSelf || myCollider == null)
            return false;

        Vector2 position2D = new Vector2(worldPosition.x, worldPosition.y);
        return myCollider.OverlapPoint(position2D);
    }

 
}
