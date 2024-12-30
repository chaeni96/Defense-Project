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
                // ���� ���� ����
                Color originalColor = spriteRenderer.color;

                // ���� ���� ������
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1�� ���� ����������
                    .Append(spriteRenderer.DOColor(originalColor, 0.1f));  // 0.1�� ���� ���� ������
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
        if(enemyType == EnemyType.Boss)
        {

            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffect", this.transform.position);

            explosion.GetComponent<EffectExplosion>().InitializeEffect(this, 10);
        }

        isActive = false;
        EnemyManager.Instance.RemoveEnemy(this);  // �߰�
        PoolingManager.Instance.ReturnObject(controller.gameObject);
    }


}
