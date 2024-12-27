using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : MonoBehaviour
{
    public Rigidbody2D myBody;
   
    private LayerMask targetLayer; // 적 타겟 레이어
    
    private UnitController owner; // 투사체를 발사한 유닛
    private BasicObject target; // 투사체가 타겟팅한 대상


    private Vector2 direction; // 투사체 이동 방향
    private float speed; // 투사체 속도
    private float lifetime = 5f; // 투사체의 생명 시간
    private int damage; // 투사체의 데미지

    public void Initialize(UnitController unit, BasicObject targetObj)
    {
        owner= unit;
        target = targetObj;
        damage = unit.attack;
        targetLayer = unit.targetLayer;

        direction = (target.transform.position - unit.transform.position).normalized;
        myBody = GetComponent<Rigidbody2D>();
        myBody.velocity = direction * unit.attackSpeed;

        // 투사체가 일정 시간 후에 사라지도록 설정
        Destroy(gameObject, lifetime);
    }


    private void OnTriggerEnter2D(Collider2D collider)
    {

        DamageableObject damageable = collider.GetComponent<DamageableObject>();

        // 충돌한 대상이 타겟 레이어에 속하는지 확인
        if ((collider.gameObject.layer == 10))
        {
            if (damageable != null)
            {
                damageable.onDamaged(owner, damage); // 피해 적용

                Destroy(gameObject);

                return;
            }
        }
    }
    private void FixedUpdate()
    {
        // 타겟 존재 여부 확인
        if (owner.targetObject == null)
        {
            Destroy(gameObject); // 타겟이 사라지면 투사체 삭제
        }
    }
}
