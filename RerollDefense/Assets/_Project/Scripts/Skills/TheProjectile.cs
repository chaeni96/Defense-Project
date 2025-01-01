using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : SkillBase
{
    public Enemy target { get; private set; }

    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);
        ProjectileManager.Instance.RegisterProjectile(this);
    }

    public override void Fire(Vector3 targetPosition)
    {
        // 타겟이 Enemy 컴포넌트를 가지고 있는지 체크
        target = Physics2D.OverlapPoint(targetPosition)?.GetComponent<Enemy>();
        if (target == null) return;

        transform.position = owner.transform.position;
    }

    private void OnDisable()
    {
        owner = null;
        target = null;
    }
}