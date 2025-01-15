using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase
{
    [SerializeField] float damageDelay = 0f;
    [SerializeField] float totalFXDelay = 0.7f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] Collider2D myCollider;

    private readonly List<Collider2D> enemys = new List<Collider2D>();

    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);

        // 리스트 초기화
        enemys.Clear();

        float radius = owner.GetStat(StatName.AttackRange);
        transform.localScale = new Vector3(radius, radius, radius);
    }

    public override void Fire(Vector3 targetPosition)
    {
        transform.position = owner.transform.position;
        StartCoroutine(ApplyDamageSequence());
    }

    private IEnumerator ApplyDamageSequence()
    {
        yield return new WaitForSeconds(damageDelay);
        
        CheckEnemyInCollider();
        StartCoroutine(DestroyAfterDuration(totalFXDelay));
    }


    public void CheckEnemyInCollider()
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemyMask;

        int enemyCount = myCollider.OverlapCollider(filter, enemys);

        for(int i = 0; i < enemyCount; i++)
        {
            var enemy = EnemyManager.Instance.GetActiveEnemys(enemys[i]);
            if (enemy != null)
            {
                enemy.onDamaged(owner, owner.GetStat(StatName.ATK));
            }
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        CleanUp();
        PoolingManager.Instance.ReturnObject(this.gameObject);
    }

    public void CleanUp()
    {
        // 코루틴 정리
        StopAllCoroutines();

        // 리스트 정리
        enemys.Clear();
    }


}
