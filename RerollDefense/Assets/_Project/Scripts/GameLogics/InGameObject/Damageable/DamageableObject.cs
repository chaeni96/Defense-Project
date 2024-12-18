using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DamageableObject : BasicObject
{

    //���ع������ִ� ��ü -> hp ���� ��ü, �÷��̾� ����, �� ����

    public TMP_Text hpText;


    public float maxHP = 10;
    public float HP;

    public Collider2D hitCollider;


    public override void Initialize()
    {
        base.Initialize();
        myBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        HP = maxHP;
        UpdateHpText();
    }

    public override void Update()
    {
        base.Update();

    }

    public void UpdateHpText()
    {
        var hp = HP;

        hpText.text = hp.ToString();
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

        Destroy(controller.gameObject);
        //TODO : ������Ʈ Ǯ�� ��ȯ �ؾߵ�
        
    }
}
