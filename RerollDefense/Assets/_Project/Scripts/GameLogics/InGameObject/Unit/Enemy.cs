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
    
    [SerializeField] private EnemyType enemyType;//인스펙터에서 바인딩해주기
    [SerializeField] private Slider hpBar;  // Inspector에서 할당

    [SerializeField] private Canvas hpBarCanvas;  // Inspector에서 할당

    public LineRenderer pathRenderer;  // Inspector에서 할당

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
        pathRenderer.sortingOrder = 1; // 경로가 타일맵 위에 그려지도록
    }

    public void InitializeEnemyInfo(D_EnemyData data)
    {
        isEnemy = true;

        enemyData = data;


        baseStats.Clear();
        currentStats.Clear();

        // StatSubject에 따른 스탯 합산
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

        // 현재 스탯 초기화
        foreach (var baseStat in baseStats)
        {
            currentStats[baseStat.Key] = new StatStorage
            {
                statName = baseStat.Value.statName,
                value = baseStat.Value.value,
                multiply = baseStat.Value.multiply
            };
        }


        // currentHP를 maxHP로 초기화
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
        if (GetStat(StatName.CurrentHp) <= 0) return;  // 이미 죽었거나 죽는 중이면 스탯 변경 무시

        base.OnStatChanged(subject, statChange);
  
        // 체력 관련 스탯이 변경되었을 때
        if (statChange.statName == StatName.CurrentHp || statChange.statName == StatName.MaxHP)
        {
            if (statChange.statName == StatName.CurrentHp)
            {
                // 데미지를 입었을 경우 깜빡이는 효과 적용
                DOTween.Sequence()
                .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                .Append(spriteRenderer.DOColor(Color.white, 0.1f))  // 0.1초 동안 원래 색으로
                .OnComplete(() =>
                {
                    // 깜빡임 효과가 끝난 후 체력이 0 이하인지 확인하고 죽음 처리
                    if (GetStat(StatName.CurrentHp) <= 0 && !isActive)
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

    // 상태 변경 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }


    public void OnReachEndTile()
    {
        //enemy의 공격력만큼 player의 hp감소 -> 스탯매니저 통해서 값 변경
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.CurrentHp,
            value = currentStats[StatName.ATK].value * -1 ,
            multiply = currentStats[StatName.ATK].multiply
        });

        isReach = true;
        OnDead();
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
        if (enemyType == EnemyType.Boss && !isReach)
        {
            //effect 발생, enemy spawn
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
        // 실제 스폰된 enemy 수만큼 remainEnemies 증가
        StageManager.Instance.AddRemainingEnemies(spawnEnemyCount);

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
