using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheDefaultProjectile : SkillBase
{
    [Header("투사체 설정")]
    public float projectileSpeed = 10f;       // 투사체 속도
    public float lifeTime = 3f;              // 최대 생존 시간
    public float damage = 10f;               // 기본 데미지
    public GameObject hitEffect;             // 히트 이펙트 (선택사항)

    private Vector3 direction;               // 이동 방향
    private float timer = 0f;                // 생존 타이머
    private Rigidbody2D rb;                  // 리지드바디

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }
    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        direction = targetDirection.normalized;
        timer = 0f;

        // 방향에 맞게 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 충돌 레이어 설정 (소유자와 다른 레이어만 충돌)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // 투사체 활성화
        gameObject.SetActive(true);
    }

    protected void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // 시간 초과 확인
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            DestroySkill();
            return;
        }

        // 이동 처리
        Vector2 movement = direction * projectileSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 소유자가 아닌지 확인
        if (collision.gameObject == owner.gameObject) return;

        // 적 레이어 확인
        BasicObject hitObject = collision.GetComponent<BasicObject>();
        if (hitObject != null && hitObject.isEnemy != owner.isEnemy)
        {
            // 데미지 적용
            ApplyDamage(hitObject);

            // 효과 재생 (선택사항)
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // 투사체 제거
            DestroySkill();
        }
    }

    protected virtual void ApplyDamage(BasicObject target)
    {
        // 실제 데미지 계산
        float actualDamage = CalculateDamage();

        // 데미지 적용
        target.OnDamaged(owner, actualDamage);
    }

    protected virtual float CalculateDamage()
    {
        // 기본 데미지 + 소유자의 공격력 비율
        return damage * (1 + owner.GetStat(StatName.ATK) / 100f);
    }

    public override void DestroySkill()
    {
        owner = null;
        PoolingManager.Instance.ReturnObject(gameObject);

    }
    //protected virtual void DestroyProjectile()
    //{
    //    // 풀링 시스템 사용 시 반환, 아니면 파괴
    //    Destroy(gameObject);
    //}
}
