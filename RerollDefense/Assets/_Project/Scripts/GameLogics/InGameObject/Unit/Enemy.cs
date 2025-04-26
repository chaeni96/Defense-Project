using BGDatabaseEnum;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class DistanceChecker
{
    public BuffTimeBase linkedTimeBaseBuff;
    public float maxDistance;

    public void InitiliazeDistanceChecker(Enemy targetObject)
    {
        targetObject.OnUpdateDistanceCheck += OnUpdate;
    }

    public void OnUpdate()
    {

    }


}

public class Enemy : BasicObject
{

    public SpriteRenderer spriteRenderer;
    public Collider2D enemyCollider;

    public bool isReadyToMove = false; // �̵� �غ� �Ϸ� ����


    [SerializeField] private EnemyType enemyType;//�ν����Ϳ��� ���ε����ֱ�
    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    public LineRenderer pathRenderer;  // Inspector���� �Ҵ�

    private D_EnemyData enemyData;

    private bool isReach;
    private bool isActive;
    private Vector3 originalScale;


    public Action OnUpdateDistanceCheck;

    public UnitController attackTarget = null;
    public bool isAttackAnimationPlaying = false;
    private float lastAttackTime = 0f;

    public override void Initialize()
    {
        base.Initialize();
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        originalScale = transform.localScale;

        UpdateHpBar();

        InitializeLineRenderer();

        ChangeState(new EnemyIdleState());

    }

    private void InitializeLineRenderer()
    {
        pathRenderer.positionCount = 0;
        pathRenderer.startWidth = 0.03f;
        pathRenderer.endWidth = 0.03f;
        pathRenderer.sortingOrder = 1; // ��ΰ� Ÿ�ϸ� ���� �׷�������
    }

    public void InitializeEnemyInfo(D_EnemyData data)
    {
        isEnemy = true;

        enemyData = data;


        baseStats.Clear();
        currentStats.Clear();

        // StatSubject�� ���� ���� �ջ�
        foreach (var subject in enemyData.f_statSubject)
        {
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);

            foreach (var stat in subjectStats)
            {
                if (!baseStats.ContainsKey(stat.statName))
                {
                    baseStats[stat.statName] = new StatStorage
                    {
                        statName = stat.statName,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    baseStats[stat.statName].value += stat.value;
                    baseStats[stat.statName].multiply *= stat.multiply;
                }
            }

            AddSubject(subject);
        }

        // ���� ���� �ʱ�ȭ
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                statName = baseStat.Value.statName,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }


        // currentHP�� maxHP�� �ʱ�ȭ
        if (!currentStats.ContainsKey(StatName.CurrentHp))
        {
            var maxHp = GetStat(StatName.MaxHP);
            currentStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }

        isActive = true;
        isReach = false;

        UpdateHpBar();
    }
    //�̺�Ʈ ���
    public void InitializeEvents(List<D_EventDummyData> events)
    {
        if (events == null || events.Count == 0) return;

        foreach (D_EventDummyData eventData in events)
        {
            // �̺�Ʈ ID
            string eventId = eventData.Id.ToString();

            // �̺�Ʈ ��ü ����
            IEvent gameEvent = EventManager.Instance.CreateEventFromData(eventData);

            if (gameEvent != null)
            {
                // �̺�Ʈ �Ŵ����� �̺�Ʈ ���
                EventManager.Instance.RegisterEvent(eventId, gameEvent);

                // �� ��ü�� �̺�Ʈ ����
                EventManager.Instance.AssignEventToObject(gameObject, eventData.f_eventTriggerType, eventId);
            }
        }
    }

    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (GetStat(StatName.CurrentHp) <= 0) return;  // �̹� �׾��ų� �״� ���̸� ���� ���� ����

        base.OnStatChanged(subject, statChange);
  
        // ü�� ���� ������ ����Ǿ��� ��
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            if (statChange.statName == StatName.CurrentHp)
            {
                // �������� �Ծ��� ��� �����̴� ȿ�� ����
                DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                .Append(spriteRenderer.DOColor(Color.white, 0.1f))  // 0.1�� ���� ���� ������
                .OnComplete(() =>
                {
                    // ������ ȿ���� ���� �� ü���� 0 �������� Ȯ���ϰ� ���� ó��
                    if (GetStat(StatName.CurrentHp) <= 0 && !isActive)
                    {
                        OnDead();
                    }
                });
            }

            // HP �� ������Ʈ
            UpdateHpBar();
        }
    }

    // �̵� ���� ����
    public void SetReadyToMove(bool ready)
    {
        isReadyToMove = ready;
    }

    private void UpdateHpBar()
    {
        float currentHp = GetStat(StatName.CurrentHp);
        float maxHp = GetStat(StatName.MaxHP);

        if (hpBar != null && maxHp > 0)
        {
            hpBar.value = currentHp / maxHp;
        }
    }

    // ���� ���� �޼���
    public void SetActive(bool active)
    {
        isActive = active;
    }


    public void OnReachEndTile()
    {
        //enemy�� ���ݷ¸�ŭ player�� hp���� -> ���ȸŴ��� ���ؼ� �� ����
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.CurrentHp,
            value = currentStats[StatName.ATK].value * -1 ,
            multiply = currentStats[StatName.ATK].multiply
        });

        isReach = true;
        OnDead();
    }

    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker�� ���ݷ� 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
                HitEffect();
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            OnDead();
        }
    }

    public void HitEffect()
    {

        // ���� Ʈ���� ���� ���̸� �ߴ�
        transform.DOKill(true);

        // ���� ũ��� �ʱ�ȭ
        transform.localScale = originalScale;

        // ��ġ ������ ȿ�� ����
        transform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 1, 1)
            .SetEase(Ease.OutQuart);

        // �������� ������ ���������� ������
        if (spriteRenderer != null)
        {
            // ���� ���� ������
            DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1�� ���� ���� ������
        }
    }

    public void OnDead()
    {
        //TODO : ���߿� ���� boss�� ���� �ȵ��������� ����ó�� �ʿ���
        if (enemyType == EnemyType.Boss && !isReach)
        {
            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffectObject", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        // �̺�Ʈ �Ŵ����� ���� OnDeath �̺�Ʈ Ʈ����
        EventManager.Instance.TriggerEvent(gameObject, EventTriggerType.OnDeath, transform.position);

        isActive = false;
        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        PoolingManager.Instance.ReturnObject(gameObject);

        // ���� ���̺꿡 �� ���� �˸�
        StageManager.Instance.NotifyEnemyDecrease();
    }

    // ���� �޼���
    public void AttackTarget()
    {
        float attackCooldown = GetStat(StatName.AttackSpeed);

        // �ִϸ��̼��� ���� ���̰ų� ��ٿ��� ������ �ʾ����� �������� ����
        if (isAttackAnimationPlaying || Time.time - lastAttackTime < attackCooldown)
            return;

        // Ÿ�� Ȯ�� (�̹� ���� ���·� ���� ���¿��� Ȯ�� �ߺ��̱� ������ ������ġ�� ����)
        if (attackTarget == null || !attackTarget.canAttack ||
            attackTarget.GetStat(StatName.CurrentHp) <= 0 ||
            Vector2.Distance(transform.position, attackTarget.transform.position) > GetStat(StatName.AttackRange))
        {
            // Ÿ���� ������ų� �׾��ų� ���� ������ ����� �̵� ���·� ���ư�
            attackTarget = null; // Ÿ�� ����
            ChangeState(new EnemyMoveState());
            return;
        }

        // ���� �ٶ󺸵��� ���� ��ȯ
        bool shouldFlip = transform.position.x > attackTarget.transform.position.x;
        spriteRenderer.flipX = shouldFlip;

        // ���� �ð� ���
        lastAttackTime = Time.time;

        // ���� �ִϸ��̼� ����
        isAttackAnimationPlaying = true;
        animator.CrossFade(TriggerKeyword.Attack.ToString(), 0.1f);

        // ����: �������� �ִϸ��̼� �̺�Ʈ�� ApplyDamage �޼��忡�� �����
    }

    public void ApplyDamage()
    {
        if (attackTarget != null && attackTarget.canAttack)
        {
            float damage = GetStat(StatName.ATK);
            attackTarget.onDamaged(this, damage);
        }
    }

    // �ִϸ��̼� ���� �̺�Ʈ���� ȣ��� �޼���
    public void OnAttackAnimationEnd()
    {
        isAttackAnimationPlaying = false;
    }

}
