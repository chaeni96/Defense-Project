using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{
    [SerializeField] float totalFXDelay = 0.5f;
    [SerializeField] LayerMask enemyMask;
    [SerializeField] Collider2D myCollider;

    private HashSet<Enemy> damagedEnemies = new HashSet<Enemy>();  // 이미 데미지를 준 적들 
    private int currentScheduleUID;
    private float radius;

    public override void Initialize(UnitController unit)
    {
        base.Initialize(unit);

        radius = owner.GetStat(StatName.AttackRange);
        transform.localScale = new Vector3(radius * 2f, radius * 2f, radius * 2f);

    }

    public override void Fire(Vector3 targetPosition)
    {
        transform.position = owner.transform.position;

        currentScheduleUID = TimeTableManager.Instance.RegisterSchedule(totalFXDelay);
        TimeTableManager.Instance.AddTimeChangeSubscriber(this);
        TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, currentScheduleUID);
        //soundEffect.PlaySound();
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (currentScheduleUID != scheduleUID) return;

        // 원형 범위 내의 모든 콜라이더 감지
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, radius, enemyMask);

        float damage = owner.GetStat(StatName.ATK);
        foreach (var hitCollider in hitColliders)
        {
            var enemy = EnemyManager.Instance.GetActiveEnemys(hitCollider);
            if (enemy != null && !damagedEnemies.Contains(enemy))
            {
                enemy.onDamaged(owner, damage);
                damagedEnemies.Add(enemy);
            }
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (currentScheduleUID != scheduleUID) return;
        CleanUp();
        PoolingManager.Instance.ReturnObject(gameObject);
    }

    public override void CleanUp()
    {
        TimeTableManager.Instance.RemoveTimeChangeSubscriber(this);
        TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(currentScheduleUID);

        // 리스트 정리
        damagedEnemies.Clear();

    }


}
