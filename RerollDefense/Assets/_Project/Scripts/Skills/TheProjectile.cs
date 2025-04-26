using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : SkillBase
{
    public Enemy target { get; private set; }

    public float storedSpeed; // �߻� ������ �ӵ� ����
    public float storedDamage; // �߻� ������ ���ݷ� ����


    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);
        // �߻� ������ ���� ����
        if (owner != null)
        {
            storedSpeed = owner.GetStat(StatName.ProjectileSpeed);
            storedDamage = owner.GetStat(StatName.ATK);
        }


        ProjectileManager.Instance.RegisterProjectile(this);
    }

    public override void Fire(Vector3 targetPosition)
    {
        target = EnemyManager.Instance.GetEnemyAtPosition(targetPosition);

        if (target == null) return;

        transform.position = owner.transform.position;
        //soundEffect.PlaySound();
    }

    public override void CleanUp()
    {
        owner = null;
        target = null;
    }

}