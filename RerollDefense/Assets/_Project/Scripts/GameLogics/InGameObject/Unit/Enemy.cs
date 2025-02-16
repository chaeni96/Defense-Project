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
    
    [SerializeField] private EnemyType enemyType;//�ν����Ϳ��� ���ε����ֱ�
    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    public LineRenderer pathRenderer;  // Inspector���� �Ҵ�

    private D_EnemyData enemyData;

    private bool isReach;
    private bool isActive;

    public Action OnUpdateDistanceCheck;

    private void Update()
    {
        OnUpdateDistanceCheck?.Invoke();
    }
    public override void Initialize()
    {
        base.Initialize();
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        UpdateHpBar();

        InitializeLineRenderer();

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

    //TODO : projectile�� aoe�� StatManager�� �޼��带 �θ����� �ٲٱ�
    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker�� ���ݷ� 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
            }

            // �������� ������ ���������� ������
            if (spriteRenderer != null)
            {
                // ���� ���� ������
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                    .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1�� ���� ���� ������
            }
        }

        if (GetStat(StatName.CurrentHp) <= 0)
        {
            OnDead();
        }
    }

    public void OnDead()
    {
        if (enemyType == EnemyType.Boss && !isReach)
        {
            //effect �߻�, enemy spawn
            SpawnMinions(5);


            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffectObject", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        isActive = false;
        baseStats.Clear();
        currentStats.Clear();
        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        PoolingManager.Instance.ReturnObject(gameObject);
        StageManager.Instance.DecreaseEnemyCount();
    }



    private void SpawnMinions(int spawnEnemyCount)
    {
        // ���� ������ enemy ����ŭ remainEnemies ����
        StageManager.Instance.AddRemainingEnemies(spawnEnemyCount);

        Vector2 centerPos = TileMapManager.Instance.GetWorldToTilePosition(transform.position);

        List<Vector2> directions = new List<Vector2>
        {
            Vector2.zero,  // ���� ���� ��ġ
            Vector2.up,
            Vector2.right,
            Vector2.down,
            Vector2.left
        };

        List<Vector3> validPositions = new List<Vector3>();

        foreach (var dir in directions)
        {
            Vector2 checkPos = centerPos + dir;
            TileData tileData = TileMapManager.Instance.GetTileData(checkPos);

            if (tileData != null && tileData.isAvailable)
            {
                validPositions.Add(TileMapManager.Instance.GetTileToWorldPosition(checkPos));
            }
        }

        if (validPositions.Count > 0)
        {
            int enemiesPerPosition = spawnEnemyCount / validPositions.Count;
            int remainingEnemies = spawnEnemyCount % validPositions.Count;

            for (int i = 0; i < validPositions.Count; i++)
            {
                int spawnCount = enemiesPerPosition;
                if (remainingEnemies > 0)
                {
                    spawnCount++;
                    remainingEnemies--;
                }

                for (int j = 0; j < spawnCount; j++)
                {
                   EnemyManager.Instance.SpawnEnemy(enemyData.f_DeathSpawnEnemyData, validPositions[i]);
                }
            }
        }
    }

}
