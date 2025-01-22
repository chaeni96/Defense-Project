using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase
{
    [SerializeField] float totalFXDelay = 0.5f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] Collider2D myCollider;

    private List<Collider2D> enemys = new List<Collider2D>();
    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();  // �̹� �������� �� ���� 

    private ContactFilter2D filter;

    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);

        // ����Ʈ �ʱ�ȭ
        enemys.Clear();

        float radius = owner.GetStat(StatName.AttackRange) * 2f;
        transform.localScale = new Vector3(radius, radius, radius);

        // filter �ʱ�ȭ
        filter = new ContactFilter2D();
        filter.useLayerMask = true;
        filter.layerMask = enemyMask;
    }

    public override void Fire(Vector3 targetPosition)
    {
        transform.position = owner.transform.position;
        StartCoroutine(CheckEnemiesInRange());
    }

    private IEnumerator CheckEnemiesInRange()
    {
        try
        {
            float checkInterval = 0.1f;  // üũ �ֱ�
            float elapsedTime = 0f;

            while (elapsedTime < totalFXDelay)
            {
                enemys.Clear();
                myCollider.OverlapCollider(filter, enemys);
                float damage = owner.GetStat(StatName.ATK);

                foreach (var enemyCollider in enemys)
                {
                    var enemy = EnemyManager.Instance.GetActiveEnemys(enemyCollider);
                    if (enemy != null && !damagedEnemies.Contains(enemy))
                    {
                        enemy.onDamaged(owner, damage);
                        damagedEnemies.Add(enemy);  // �������� �� �� ���
                    }
                }

                elapsedTime += checkInterval;
                yield return new WaitForSeconds(checkInterval);
            }
        }
        finally
        {
            CleanUp();

            PoolingManager.Instance.ReturnObject(gameObject);
        }
       
    }

  
    public void CleanUp()
    {
        // �ڷ�ƾ ����
        StopAllCoroutines();

        // ����Ʈ ����
        enemys.Clear();
        damagedEnemies.Clear();

    }


}
