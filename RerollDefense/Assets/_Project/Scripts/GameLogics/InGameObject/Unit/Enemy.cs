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
                // 현재 색상 저장
                Color originalColor = spriteRenderer.color;

                // 색상 변경 시퀀스
                DOTween.Sequence()
                    .Append(spriteRenderer.DOColor(Color.red, 0.1f))  // 0.1초 동안 빨간색으로
                    .Append(spriteRenderer.DOColor(originalColor, 0.1f));  // 0.1초 동안 원래 색으로
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
        if(enemyType == EnemyType.Boss)
        {

            GameObject explosion = PoolingManager.Instance.GetObject("ExplosionEffect", this.transform.position);

            explosion.GetComponent<EffectExplosion>().InitializeEffect(this, 10);
        }

        isActive = false;
        EnemyManager.Instance.RemoveEnemy(this);  // 추가
        PoolingManager.Instance.ReturnObject(controller.gameObject);
    }


}
