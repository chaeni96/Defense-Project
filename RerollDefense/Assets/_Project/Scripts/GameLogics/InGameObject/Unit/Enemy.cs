using BGDatabaseEnum;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : BasicObject
{


    public SpriteRenderer spriteRenderer;
    public Collider2D enemyCollider;
    
    //enemy Stat -> 프리팹에 저장해두기
    public float maxHP;
    public float HP;
    public float attackPower;
    public float moveSpeed;
    private bool isActive = true;
    
    [SerializeField] private EnemyType enemyType;//인스펙터에서 바인딩해주기
    [SerializeField] private Slider hpBar;  // Inspector에서 할당

    [SerializeField] private Canvas hpBarCanvas;  // Inspector에서 할당


    public override void Initialize()
    {
        base.Initialize();
        HP = maxHP;
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);
        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        UpdateHpText();
    }

 

    // 활성화 상태 확인 메서드
    public bool IsActive() => isActive;

    // 상태 변경 메서드
    public void SetActive(bool active)
    {
        isActive = active;
    }

    public void UpdateHpText()
    {
        var hp = HP;


        if (hpBar != null)
        {
            hpBar.value = hp / maxHP;

        }
    }


    public void OnReachEndTile()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TakeDamage(attackPower);
            isActive = false;
        }
    }

    public void onDamaged(BasicObject attacker, float damage = 0)
    {
        if (attacker != null)
        {

            //attacker의 공격력 
            HP -= damage;

            // 데미지를 입으면 빨간색으로 깜빡임
            if (spriteRenderer != null)
            {
                // 색상 변경 시퀀스
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                    .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1초 동안 원래 색으로
            }
        }

        if (HP <= 0)
        {
            HP = 0;

            onDead(this);
        }

        UpdateHpText();
    }

    public void onDead(BasicObject controller)
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
                    EnemyManager.Instance.SpawnEnemy("Enemy_Normal", validPositions[i]);
                }
            }
        }
    }

}
