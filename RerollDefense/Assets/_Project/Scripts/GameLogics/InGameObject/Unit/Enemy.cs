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
    
    [SerializeField] private EnemyType enemyType;//인스펙터에서 바인딩해주기
    [SerializeField] private Slider hpBar;  // Inspector에서 할당

    [SerializeField] private Canvas hpBarCanvas;  // Inspector에서 할당


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

        // StatSubject에 따른 스탯 합산
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

        // 합산된 스탯을 기본값으로 설정
        baseStats = mergedStats;

        // 현재 스탯 초기화
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

        
        // 체력 관련 스탯이 변경되었을 때
        if (statChange.stat == StatName.CurrentHp || statChange.stat == StatName.MaxHP)
        {
            // 체력 변화 전후를 비교하여 데미지 처리
            float currentHp = GetStat(StatName.CurrentHp);

            if (statChange.stat == StatName.CurrentHp)
            {
                // 데미지를 입었을 경우 깜빡이는 효과 적용
                DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                .Append(spriteRenderer.DOColor(Color.white, 0.1f))  // 0.1초 동안 원래 색으로
                .OnComplete(() =>
                {
                    // 깜빡임 효과가 끝난 후 체력이 0 이하인지 확인하고 죽음 처리
                    if (GetStat(StatName.CurrentHp) <= 0)
                    {
                        OnDead();
                    }
                });
            }

            // HP 바 업데이트
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


    // 활성화 상태 확인 메서드
    public bool IsActive() => isActive;

    // 상태 변경 메서드
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

    //TODO : projectile과 aoe도 StatManager의 메서드를 부르도록 바꾸기
    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {
            //attacker의 공격력 
            if (currentStats.TryGetValue(StatName.CurrentHp, out var hpStat))
            {
                hpStat.value -= (int)damage;
                UpdateHpBar();
            }

            // 데미지를 입으면 빨간색으로 깜빡임
            if (spriteRenderer != null)
            {
                // 색상 변경 시퀀스
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                    .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1초 동안 원래 색으로
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
            //effect 발생, enemy spawn
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
            Vector2.zero,  // 보스 현재 위치
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
