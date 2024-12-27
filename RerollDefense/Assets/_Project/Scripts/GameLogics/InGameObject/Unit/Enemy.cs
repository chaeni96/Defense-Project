using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;

public class Enemy : BasicObject
{


    public TMP_Text hpText;

    //enemy Stat -> �����տ� �����صα�
    public float maxHP;
    public float HP;
    public float attackPower;
    public float moveSpeed;

    public override void Initialize()
    {
        base.Initialize();

        HP = maxHP;
        UpdateHpText();
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
        }
    }

    public void onDamaged(BasicObject attacker, int damage = 0)
    {
        if (attacker != null)

            //attacker�� ���ݷ� 
            HP -= damage;

        if (HP <= 0)
        {
            HP = 0;
            onDead(this);
        }

        UpdateHpText();
    }

    public void onDead(BasicObject controller)
    {
        PoolingManager.Instance.ReturnObject(controller.gameObject);
    }
}
