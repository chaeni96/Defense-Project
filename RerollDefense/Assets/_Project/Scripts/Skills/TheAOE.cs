using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase
{
    private UnitController owner;
    private bool hasDealtDamage = false;
    [SerializeField] float damageDelay = 0f;
    [SerializeField] float totalFXDelay = 0.7f;
    [SerializeField] Collider2D collider;
    [SerializeField] LayerMask enemyMask;

    private List<Collider2D> enemys = new List<Collider2D>();


    public void Initialize(UnitController unit, List<Enemy> enemies)
    {
        owner = unit;
        enemys.Clear();
        float radius = unit.attackRange * 2;
        transform.localScale = new Vector3(radius, radius, radius);

        if (!hasDealtDamage)
        {
            ApplyDamageToEnemies(enemies);
            hasDealtDamage = true;
        }

        StartCoroutine(DestroyAfterDuration(totalFXDelay));
    }
    public override void Fire(Vector3 targetPosition)
    {
        return;
    }

    public void CheckEnemyInCollider()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemyMask;

        int enemyCount = collider.OverlapCollider(filter, enemys);

        for(int i = 0; i < enemyCount; i++)
        {
            var enemy = EnemyManager.Instance.GetActiveEnemy(enemys[i]);
            if (enemy != null)
            {
                // 데미지 적용
                enemy.onDamaged(owner, owner.attack);
            }
        }
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
