using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : MonoBehaviour
{
    private UnitController owner;
    private bool hasDealtDamage = false;

    public void Initialize(UnitController unit, List<Enemy> enemies)
    {
        owner = unit;
        float radius = unit.attackRange * 2;
        transform.localScale = new Vector3(radius, radius, radius);

        if (!hasDealtDamage)
        {
            ApplyDamageToEnemies(enemies);
            hasDealtDamage = true;
        }

        StartCoroutine(DestroyAfterDuration(0.15f));
    }

    private void ApplyDamageToEnemies(List<Enemy> targets)
    {
        foreach (var enemy in targets)
        {
            // ������Ʈ ������ �Ÿ� ���
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // ���� ���� ���� �ִ� ��쿡�� ������ ����
            if (distance <= owner.attackRange)
            {
                enemy.onDamaged(owner, owner.attack);
            }
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // �� �信�� ���� ������ �ð�ȭ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, owner.attackRange);
    }
}
