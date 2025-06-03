using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheDefaultProjectile : SkillBase
{
    [Header("����ü ����")]
    public float projectileSpeed = 10f;       // ����ü �ӵ�
    public GameObject hitEffect;             // ��Ʈ ����Ʈ (���û���)

    private Vector3 direction;               // �̵� ����
    private float timer = 0f;                // ���� Ÿ�̸�
    private Rigidbody2D rb;                  // ������ٵ�

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0;
            rb.freezeRotation = true;
        }
    }

    public override void Fire(BasicObject target)
    {
        if (target != null && target.isActive)
        {
            // Ÿ���� �ִ� ��� ���� Ÿ�� ��ġ�� ���� ����
            direction = (target.transform.position - transform.position).normalized;
        }
        else
        {
            DestroySkill();
        }
        timer = 0f;

        // �浹 ���̾� ���� (�����ڿ� �ٸ� ���̾ �浹)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // ����ü Ȱ��ȭ
        gameObject.SetActive(true);
    }

    protected virtual void Update()
    {
        if (ownerObj == null)
        {
            Destroy(gameObject);
            return;
        }

        // �ð� �ʰ� Ȯ��
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }

        // �̵� ó��
        Vector2 movement = direction * projectileSpeed * Time.deltaTime;
        rb.MovePosition(rb.position + movement);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // �����ڰ� �ƴ��� Ȯ��
        if (ownerObj == null)
        {
            DestroySkill();
            return;
        }

        if (collision.gameObject == ownerObj.gameObject) return;


        // �� ���̾� Ȯ��
        BasicObject hitObject = collision.GetComponent<BasicObject>();
        if (hitObject != null && hitObject.isEnemy != ownerObj.isEnemy)
        {
            // ������ ����
            ApplyDamage(hitObject);

            // ȿ�� ��� (���û���)
            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }

            // ����ü ����
            DestroySkill();
        }
    }

    protected virtual void ApplyDamage(BasicObject target)
    {
        // ���� ������ ���
        float actualDamage = CalculateDamage();

        // ������ ����
        target.OnDamaged(actualDamage);
    }

    protected virtual float CalculateDamage()
    {
        // �⺻ ������ + �������� ���ݷ� ����
        return damage * (1 + ownerObj.GetStat(StatName.ATK) / 100f);
    }

  
    //protected virtual void DestroyProjectile()
    //{
    //    // Ǯ�� �ý��� ��� �� ��ȯ, �ƴϸ� �ı�
    //    Destroy(gameObject);
    //}
}
