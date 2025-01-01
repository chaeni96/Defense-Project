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
        // Ÿ���� Enemy ������Ʈ�� ������ �ִ��� üũ
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