using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheTargetDamageSkill : SkillBase
{
    [Header("타겟 스킬 설정")]
    [SerializeField] private float damage = 1f;        // 기본 데미지
    [SerializeField] private float duration = 0.5f;    // 스킬 지속 시간
    private float timer = 0f;                         // 지속 시간 타이머
    private BasicObject targetObject;                 // 타겟 오브젝트 저장

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        timer = 0f;
        targetObject = target;

        transform.position = targetPos;

        // 타겟이 존재하면 즉시 데미지 적용
        if (targetObject != null && targetObject.isEnemy != owner.isEnemy)
        {
            ApplyDamageToTarget();
        }
        else
        {
            // 타겟이 없으면 스킬 즉시 파괴
            DestroySkill();
        }
    }

    private void Update()
    {
        if (owner == null || targetObject == null)
        {
            DestroySkill();
            return;
        }

        // 지속 시간 체크
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }
    }

    private void ApplyDamageToTarget()
    {
        if (targetObject != null)
        {
            // 데미지 적용
            targetObject.OnDamaged(owner, damage);
            Debug.Log($"타겟 스킬 데미지 적용: {targetObject.name}, 데미지={damage}");
        }
    }

    public override void DestroySkill()
    {
        base.DestroySkill();
        owner = null;
        targetObject = null;
    }
}
