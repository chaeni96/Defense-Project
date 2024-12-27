using UnityEngine;

public class TheProjectile : MonoBehaviour
{
    public UnitController owner { get; private set; }
    public Enemy target { get; private set; }
    public int damage { get; private set; }

    public void Initialize(UnitController unit, Enemy targetEnemy)
    {
        owner = unit;
        target = targetEnemy;
        damage = unit.attack;

        ProjectileManager.Instance.RegisterProjectile(this);
    }
}