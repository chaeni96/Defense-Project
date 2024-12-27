using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheAOE : MonoBehaviour
{
    private UnitController owner;
    private bool hasDealtDamage = false;

    public void Initialize(UnitController unit, List<Enemy> targets)
    {
        owner = unit;
        float radius = unit.attackRange;
        transform.localScale = new Vector3(radius, radius, 1);

        if (!hasDealtDamage)
        {
            ApplyDamageToEnemies(targets);
            hasDealtDamage = true;
        }

        StartCoroutine(DestroyAfterDuration(0.5f));
    }

    private void ApplyDamageToEnemies(List<Enemy> enemies)
    {
        foreach (var enemy in enemies)
        {
            enemy.onDamaged(owner, owner.attack);
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}
