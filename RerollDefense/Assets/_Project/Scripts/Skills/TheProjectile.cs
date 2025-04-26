using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheProjectile : SkillBase
{
    public Enemy target { get; private set; }

    public float storedSpeed; // 발사 시점의 속도 저장
    public float storedDamage; // 발사 시점의 공격력 저장


    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);
        // 발사 시점의 스탯 저장
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