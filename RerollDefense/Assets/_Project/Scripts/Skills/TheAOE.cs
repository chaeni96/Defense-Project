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

        StartCoroutine(DestroyAfterDuration(0.1f));
    }

    private void ApplyDamageToEnemies(List<Enemy> targets)
    {
        foreach (var enemy in targets)
        {
            // 오브젝트 사이의 거리 계산
            float distance = Vector3.Distance(transform.position, enemy.transform.position);

            // 공격 범위 내에 있는 경우에만 데미지 적용
            if (distance <= owner.attackRange)
            {
                enemy.onDamaged(owner, owner.attack);
            }
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        PoolingManager.Instance.ReturnObject(this.gameObject);
    }
}
