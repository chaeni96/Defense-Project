using BGDatabaseEnum;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.CullingGroup;

public class Enemy : BasicObject
{


    public SpriteRenderer spriteRenderer;
    public Collider2D enemyCollider;
    
    [SerializeField] private EnemyType enemyType;//�ν����Ϳ��� ���ε����ֱ�
    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�


    private D_EnemyData enemyData;

    private bool isActive = true;

    public override void Initialize()
    {
        base.Initialize();
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        UpdateHpBar();
    }

  public void InitializeEnemyInfo(D_EnemyData data)
    {
        enemyData = data;

        // StatSubject�� ���� ���� �ջ�
        Dictionary<StatName, StatStorage> mergedStats = new Dictionary<StatName, StatStorage>();

        foreach (var subject in enemyData.f_statSubject)
        {
            var subjectStats = StatManager.Instance.GetAllStatsForSubject(subject);
            foreach (var stat in subjectStats)
            {
                if (!mergedStats.ContainsKey(stat.stat))
                {
                    mergedStats[stat.stat] = new StatStorage
                    {
                        stat = stat.stat,
                        value = stat.value,
                        multiply = stat.multiply
                    };
                }
                else
                {
                    mergedStats[stat.stat].value += stat.value;
                    mergedStats[stat.stat].multiply *= stat.multiply;
                }
            }
            AddSubject(subject);
        }

        // �ջ�� ������ �⺻������ ����
        baseStats = mergedStats;

        // ���� ���� �ʱ�ȭ
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                stat = baseStat.Value.stat,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }
        Debug.Log($"{GetStat(StatName.CurrentHp)}");

        UpdateHpBar();
    }

    public override void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        base.OnStatChanged(subject, statChange);

        
        // ü�� ���� ������ ����Ǿ��� ��
        if (statChange.stat == StatName.CurrentHp || statChange.stat == StatName.MaxHP)
        {
            // ü�� ��ȭ ���ĸ� ���Ͽ� ������ ó��
            float currentHp = GetStat(StatName.CurrentHp);

            if (statChange.stat == StatName.CurrentHp)
            {
                // �������� �Ծ��� ��� �����̴� ȿ�� ����
                DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                .Append(spriteRenderer.DOColor(Color.white, 0.1f))  // 0.1�� ���� ���� ������
                .OnComplete(() =>
                {
                    // ������ ȿ���� ���� �� ü���� 0 �������� Ȯ���ϰ� ���� ó��
                    if (GetStat(StatName.CurrentHp) <= 0)
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


    // Ȱ��ȭ ���� Ȯ�� �޼���
    public bool IsActive() => isActive;

    // ���� ���� �޼���
    public void SetActive(bool active)
    {
        isActive = active;
    }


    public void OnReachEndTile()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(GetStat(StatName.ATK));
            isActive = false;
        }
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
        if (enemyType == EnemyType.Boss)
        {
            //effect �߻�, enemy spawn
            SpawnMinions(10);

            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffectObject", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        PoolingManager.Instance.ReturnObject(gameObject);

    }



    private void SpawnMinions(int spawnEnemyCount)
    {
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
