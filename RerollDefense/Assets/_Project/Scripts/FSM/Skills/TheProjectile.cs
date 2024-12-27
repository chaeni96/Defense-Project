using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : MonoBehaviour
{
    public Rigidbody2D myBody;
   
    private LayerMask targetLayer; // �� Ÿ�� ���̾�
    
    private UnitController owner; // ����ü�� �߻��� ����
    private BasicObject target; // ����ü�� Ÿ������ ���


    private Vector2 direction; // ����ü �̵� ����
    private float speed; // ����ü �ӵ�
    private float lifetime = 5f; // ����ü�� ���� �ð�
    private int damage; // ����ü�� ������

    public void Initialize(UnitController unit, BasicObject targetObj)
    {
        owner= unit;
        target = targetObj;
        damage = unit.attack;
        targetLayer = unit.targetLayer;

        direction = (target.transform.position - unit.transform.position).normalized;
        myBody = GetComponent<Rigidbody2D>();
        myBody.velocity = direction * unit.attackSpeed;

        // ����ü�� ���� �ð� �Ŀ� ��������� ����
        Destroy(gameObject, lifetime);
    }


    private void OnTriggerEnter2D(Collider2D collider)
    {

        DamageableObject damageable = collider.GetComponent<DamageableObject>();

        // �浹�� ����� Ÿ�� ���̾ ���ϴ��� Ȯ��
        if ((collider.gameObject.layer == 10))
        {
            if (damageable != null)
            {
                damageable.onDamaged(owner, damage); // ���� ����

                Destroy(gameObject);

                return;
            }
        }
    }
    private void FixedUpdate()
    {
        // Ÿ�� ���� ���� Ȯ��
        if (owner.targetObject == null)
        {
            Destroy(gameObject); // Ÿ���� ������� ����ü ����
        }
    }
}
