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

    public void onDamaged(BasicObject attacker, int damage = 0)
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

            // 죽기 전에 이 Enemy를 향해 날아오는 모든 Projectile 제거
            var projectiles = ProjectileManager.Instance.GetProjectilesTargetingEnemy(this);
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

            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffect", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        PoolingManager.Instance.ReturnObject(gameObject);
    }



    private void SpawnMinions(int spawnEnemyCount)
    {

        //enemy 스폰할때 유효한 타일위에 스폰해야됨, boss타일과 상하좌우 유효한 타일에 스폰

        //죽은 boss 타일
        Vector3Int centerTile = TileMapManager.Instance.tileMap.WorldToCell(transform.position);

        List<Vector3Int> directions = new List<Vector3Int> //체크할 위치
           {
               Vector3Int.zero,  // 보스 현재 위치
               Vector3Int.up,
               Vector3Int.right,
               Vector3Int.down,
               Vector3Int.left
           };

        // 유효한 타일 위치 저장할 리스트
        List<Vector3> validPositions = new List<Vector3>();

        //리스트의 각 방향에 대해 반복해서 체크해야됨
        foreach (var dir in directions)
        {
            Vector3Int checkPos = centerTile + dir;

            //해당위치 타일 데이터 가져오기
            TileData tileData = TileMapManager.Instance.GetTileData(checkPos);

            //타일이 있고, 배치 가능하면 배치가능 포지션에 넣기
            if (tileData != null && tileData.isAvailable)
            {
                validPositions.Add(TileMapManager.Instance.tileMap.GetCellCenterWorld(checkPos));
            }
        }

        if (validPositions.Count > 0)
        {
            // 각 방향에 균등하게 분배
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
                    EnemyManager.Instance.SpawnEnemy("EnemyNormal", validPositions[i]);
                }
            }
        }
    }


    private void OnDisable()
    {
        EnemyManager.Instance.UnregisterEnemy(enemyCollider);
        isActive = false;
    }

}
