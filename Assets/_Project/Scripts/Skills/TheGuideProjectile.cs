using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheGuideProjectile : TheDefaultProjectile
{
    [Header("유도 설정")]
    public float rotationSpeed = 200f;         // 회전 속도 (도/초)
    public float maxTrackingAngle = 90f;       // 최대 추적 각도

    private BasicObject target;                // 추적 대상
    private bool hasTarget = false;            // 대상 존재 여부

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        base.Fire(user, targetPos, targetDirection, target);

        // 타겟 설정 ->state에서 해주기
        this.target = target;
        hasTarget = target != null;
    }

    protected  override void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // 시간 초과 확인
        float timer = 0f;
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            DestroySkill();
            return;
        }

        // 타겟 추적
        if (hasTarget && target != null && target.isActive)
        {
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

            // 타겟과의 각도 계산
            float angleToTarget = Vector3.Angle(transform.right, directionToTarget);

            // 최대 추적 각도 제한
            if (angleToTarget <= maxTrackingAngle)
            {
                // 회전 방향 결정 (시계/반시계)
                float rotationDirection = Vector3.Cross(transform.right, directionToTarget).z < 0 ? -1 : 1;

                // 점진적 회전
                float rotationAmount = rotationSpeed * Time.deltaTime * rotationDirection;
                transform.Rotate(0, 0, rotationAmount);
            }

            // 업데이트된 방향 벡터 계산
            Vector3 newDirection = transform.right;

            // 이동 처리
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            Vector2 movement = (Vector2)newDirection * projectileSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        else
        {
            // 타겟이 없거나 비활성화된 경우 직선 이동
            base.Update();
        }
    }

    protected override void ApplyDamage(BasicObject hitObject)
    {
        // 지정된 타겟인지 확인
        if (hasTarget && hitObject != target)
        {
            // 지정된 타겟이 아니면 무시
            return;
        }

        // 기본 데미지 적용
        base.ApplyDamage(hitObject);
    }
}
