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


    public override void Fire(Vector3 targetPosition)
    {
        transform.position = owner.transform.position;
        StartCoroutine(ApplyDamageSequence());
    }

    private IEnumerator ApplyDamageSequence()
    {
        yield return new WaitForSeconds(damageDelay);
        float radius = owner.attackRange * 2;
        transform.localScale = new Vector3(radius, radius, radius);

        CheckEnemyInCollider();
        StartCoroutine(DestroyAfterDuration(totalFXDelay));
    }


    public void CheckEnemyInCollider()
    {

        Debug.Log($"LayerMask: {enemyMask.value}");
        Debug.Log($"Enemy Layer: {LayerMask.NameToLayer("Enemy")}");


        ContactFilter2D filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemyMask;

        int enemyCount = myCollider.OverlapCollider(filter, enemys);

        for(int i = 0; i < enemyCount; i++)
        {
            var enemy = EnemyManager.Instance.GetEnemyCollider(enemys[i]);
            if (enemy != null)
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
