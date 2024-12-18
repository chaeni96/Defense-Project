using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheAOE : MonoBehaviour
{

    private UnitController owner;
    //�����غ����Ұ� -> �������������� �̹� ������ ���Ϳ��� ������ �� ���ϸ� �ȵ�

    private HashSet<Collider2D> damagedTargets = new HashSet<Collider2D>(); // �̹� ���ظ� �� Ÿ�� ����

    public void Initialize(Vector2 center, float radius, UnitController unit)
    {

        owner = unit;

        // AOE ������ ũ�� ����
        transform.position = center;
        transform.localScale = new Vector3(radius * 2, radius * 2, 1);

        // ���� �ð� �� AOE ���� ����
        StartCoroutine(DestroyAfterDuration(0.5f));
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // �̹� ���ظ� �� Ÿ���� ����
        if (damagedTargets.Contains(collider)) return;

        // DamageableObject�� ���� ������Ʈ���� Ȯ��
        DamageableObject damageable = collider.GetComponent<DamageableObject>();

        if (damageable != null && collider.gameObject.layer == 10)
        {
            // ������ ����
            damageable.onDamaged(owner, owner.attack); // �����̳� null�� ����
            damagedTargets.Add(collider); // Ÿ���� ���
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject); // AOE ������ ����
    }
}
