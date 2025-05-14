using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TheGuideProjectile : TheDefaultProjectile
{
    [Header("���� ����")]
    public float rotationSpeed = 200f;         // ȸ�� �ӵ� (��/��)
    public float maxTrackingAngle = 90f;       // �ִ� ���� ����

    private BasicObject target;                // ���� ���
    private bool hasTarget = false;            // ��� ���� ����

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        base.Fire(user, targetPos, targetDirection, target);

        // Ÿ�� ���� ->state���� ���ֱ�
        this.target = target;
        hasTarget = target != null;
    }

    protected  override void Update()
    {
        if (owner == null)
        {
            Destroy(gameObject);
            return;
        }

        // �ð� �ʰ� Ȯ��
        float timer = 0f;
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            DestroySkill();
            return;
        }

        // Ÿ�� ����
        if (hasTarget && target != null && target.isActive)
        {
            Vector3 directionToTarget = (target.transform.position - transform.position).normalized;

            // Ÿ�ٰ��� ���� ���
            float angleToTarget = Vector3.Angle(transform.right, directionToTarget);

            // �ִ� ���� ���� ����
            if (angleToTarget <= maxTrackingAngle)
            {
                // ȸ�� ���� ���� (�ð�/�ݽð�)
                float rotationDirection = Vector3.Cross(transform.right, directionToTarget).z < 0 ? -1 : 1;

                // ������ ȸ��
                float rotationAmount = rotationSpeed * Time.deltaTime * rotationDirection;
                transform.Rotate(0, 0, rotationAmount);
            }

            // ������Ʈ�� ���� ���� ���
            Vector3 newDirection = transform.right;

            // �̵� ó��
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            Vector2 movement = (Vector2)newDirection * projectileSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + movement);
        }
        else
        {
            // Ÿ���� ���ų� ��Ȱ��ȭ�� ��� ���� �̵�
            base.Update();
        }
    }

    protected override void ApplyDamage(BasicObject hitObject)
    {
        // ������ Ÿ������ Ȯ��
        if (hasTarget && hitObject != target)
        {
            // ������ Ÿ���� �ƴϸ� ����
            return;
        }

        // �⺻ ������ ����
        base.ApplyDamage(hitObject);
    }
}
