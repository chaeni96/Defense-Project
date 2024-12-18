using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheAOE : MonoBehaviour
{

    private UnitController owner;
    //생각해봐야할거 -> 범위공격했을때 이미 피해준 몬스터에는 데미지 또 가하면 안됨

    private HashSet<Collider2D> damagedTargets = new HashSet<Collider2D>(); // 이미 피해를 준 타겟 저장

    public void Initialize(Vector2 center, float radius, UnitController unit)
    {

        owner = unit;

        // AOE 범위의 크기 설정
        transform.position = center;
        transform.localScale = new Vector3(radius * 2, radius * 2, 1);

        // 일정 시간 후 AOE 범위 제거
        StartCoroutine(DestroyAfterDuration(0.5f));
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // 이미 피해를 준 타겟은 무시
        if (damagedTargets.Contains(collider)) return;

        // DamageableObject를 가진 오브젝트인지 확인
        DamageableObject damageable = collider.GetComponent<DamageableObject>();

        if (damageable != null && collider.gameObject.layer == 10)
        {
            // 데미지 적용
            damageable.onDamaged(owner, owner.attack); // 유닛이나 null을 전달
            damagedTargets.Add(collider); // 타겟을 기록
        }
    }

    private IEnumerator DestroyAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject); // AOE 범위를 제거
    }
}
