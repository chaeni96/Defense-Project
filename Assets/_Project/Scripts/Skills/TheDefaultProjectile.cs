using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheDefaultProjectile : SkillBase
{
    [Header("����ü ����")]
    public float projectileSpeed = 10f;       // ����ü �ӵ�
    public float lifeTime = 3f;              // �ִ� ���� �ð�
    public float damage = 10f;               // �⺻ ������
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

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        direction = targetDirection.normalized;
        timer = 0f;

        // ���⿡ �°� ȸ��
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // �浹 ���̾� ���� (�����ڿ� �ٸ� ���̾ �浹)
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }

        // ����ü Ȱ��ȭ
        gameObject.SetActive(true);
    }

    protected void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // �ð� �ʰ� Ȯ��
        timer += Time.deltaTime;
        if (timer >= lifeTime)
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
        if (collision.gameObject == owner.gameObject) return;

        // �� ���̾� Ȯ��
        BasicObject hitObject = collision.GetComponent<BasicObject>();
        if (hitObject != null && hitObject.isEnemy != owner.isEnemy)
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
        target.OnDamaged(owner, actualDamage);
    }

    protected virtual float CalculateDamage()
    {
        // �⺻ ������ + �������� ���ݷ� ����
        return damage * (1 + owner.GetStat(StatName.ATK) / 100f);
    }

    public override void DestroySkill()
    {
        owner = null;
        PoolingManager.Instance.ReturnObject(gameObject);

    }
    //protected virtual void DestroyProjectile()
    //{
    //    // Ǯ�� �ý��� ��� �� ��ȯ, �ƴϸ� �ı�
    //    Destroy(gameObject);
    //}
}
