using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class Enemy : BasicObject
{


    public TMP_Text hpText;

    //enemy Stat -> 프리팹에 저장해두기
    public float maxHP;
    public float HP;
    public float attackPower;
    public float moveSpeed;

    private bool isActive = true;


    public override void Initialize()
    {
        base.Initialize();

        HP = maxHP;
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

        hpText.text = hp.ToString();
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

            //attacker의 공격력 
            HP -= damage;

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
        isActive = false;
        EnemyManager.Instance.RemoveEnemy(this);  // 추가
        PoolingManager.Instance.ReturnObject(controller.gameObject);
    }
}
