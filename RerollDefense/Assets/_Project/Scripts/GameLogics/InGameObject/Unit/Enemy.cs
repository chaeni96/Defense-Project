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
    
    //enemy Stat -> �����տ� �����صα�
    public float maxHP;
    public float HP;
    public float attackPower;
    public float moveSpeed;
    private bool isActive = true;
    
    [SerializeField] private EnemyType enemyType;//�ν����Ϳ��� ���ε����ֱ�
    [SerializeField] private Slider hpBar;  // Inspector���� �Ҵ�

    [SerializeField] private Canvas hpBarCanvas;  // Inspector���� �Ҵ�

    public override void Initialize()
    {
        base.Initialize();
        HP = maxHP;
        EnemyManager.Instance.RegisterEnemy(this, enemyCollider);

        hpBarCanvas.worldCamera = GameManager.Instance.mainCamera;
        UpdateHpText();
    }

 

    // Ȱ��ȭ ���� Ȯ�� �޼���
    public bool IsActive() => isActive;

    // ���� ���� �޼���
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

            //attacker�� ���ݷ� 
            HP -= damage;

            // �������� ������ ���������� ������
            if (spriteRenderer != null)
            {
                // ���� ���� ������
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                    .Append(spriteRenderer.DOColor(Color.white, 0.1f));  // 0.1�� ���� ���� ������
            }
        }

        if (HP <= 0)
        {
            HP = 0;

            // �ױ� ���� �� Enemy�� ���� ���ƿ��� ��� Projectile ����
            var projectiles = ProjectileManager.Instance.GetProjectilesTargetingEnemy(this);
            onDead(this);
        }

        UpdateHpText();
    }

    public void onDead(BasicObject controller)
    {
        if (enemyType == EnemyType.Boss)
        {
            //effect �߻�, enemy spawn
            SpawnMinions(10);

            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffect", transform.position);
            explosion.GetComponent<EffectExplosion>().InitializeEffect(this);
        }

        PoolingManager.Instance.ReturnObject(gameObject);
    }



    private void SpawnMinions(int spawnEnemyCount)
    {

        //enemy �����Ҷ� ��ȿ�� Ÿ������ �����ؾߵ�, bossŸ�ϰ� �����¿� ��ȿ�� Ÿ�Ͽ� ����

        //���� boss Ÿ��
        Vector3Int centerTile = TileMapManager.Instance.tileMap.WorldToCell(transform.position);

        List<Vector3Int> directions = new List<Vector3Int> //üũ�� ��ġ
           {
               Vector3Int.zero,  // ���� ���� ��ġ
               Vector3Int.up,
               Vector3Int.right,
               Vector3Int.down,
               Vector3Int.left
           };

        // ��ȿ�� Ÿ�� ��ġ ������ ����Ʈ
        List<Vector3> validPositions = new List<Vector3>();

        //����Ʈ�� �� ���⿡ ���� �ݺ��ؼ� üũ�ؾߵ�
        foreach (var dir in directions)
        {
            Vector3Int checkPos = centerTile + dir;

            //�ش���ġ Ÿ�� ������ ��������
            TileData tileData = TileMapManager.Instance.GetTileData(checkPos);

            //Ÿ���� �ְ�, ��ġ �����ϸ� ��ġ���� �����ǿ� �ֱ�
            if (tileData != null && tileData.isAvailable)
            {
                validPositions.Add(TileMapManager.Instance.tileMap.GetCellCenterWorld(checkPos));
            }
        }

        if (validPositions.Count > 0)
        {
            // �� ���⿡ �յ��ϰ� �й�
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
