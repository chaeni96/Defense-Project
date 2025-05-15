using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserAOE : SkillBase
{
    [Header("������ AOE ����")]
    [SerializeField] private bool straightLine = false;            // ������ ������ ��� (Ÿ�� ��ġ �����ϰ� ���� �������� �߻�)
    [SerializeField] private Vector2 size = new Vector2(10f, 1f);  // ������ ũ�� (����, ��)
    [SerializeField] private float damage = 30f;                   // �⺻ ������
    [SerializeField] private float duration = 1.0f;                // ���� �ð�
    [SerializeField] private bool isDOT = false;                   // ���� ������ ����
    [SerializeField] private float delayBetweenDamage = 0.1f;      // ������ ���� ���� (DoT ȿ����)
    [SerializeField] private bool followOwnerRotation = false;     // Ÿ�� ���� ��� ȸ������ ����
    [SerializeField] private float offsetFromPlayer = 1.0f;        // �÷��̾�κ����� �Ÿ� ������

    protected float timer = 0f;                  // ���� �ð� Ÿ�̸�
    protected float damageTimer = 0f;            // ������ Ÿ�̸�
    protected Vector3 direction;                 // ������ ����
    protected HashSet<int> damagedTargets;       // �̹� �������� ���� ��� ����


    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
        damagedTargets = new HashSet<int>();

        //transform.localScale = new Vector3(size.x, size.y, 1f);
    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        timer = 0f;
        damageTimer = 0f;
        damagedTargets.Clear();

        // ���� ����
        if (straightLine)
        {
            // ������ ������ ���: �÷��̾��� ���� ����(right)�� ���
            direction = user.transform.right.normalized;
        }
        else
        {
            // Ÿ�� ���� ���: ���޹��� Ÿ�� ���� ���
            direction = targetDirection.normalized;
        }

        // ������ ��ġ �� ȸ�� ����
        UpdateLaserPositionAndRotation();

        // ��� ù ��° ������ ����
        ApplyDamageToTargetsInRange();

        // ������ ���� ȸ�� ���� (Ȱ��ȭ�� ���)
        if (followOwnerRotation)
        {
            StartCoroutine(FollowOwnerRotation());
        }
    }

    // ������ ��ġ�� ȸ�� ������Ʈ �޼��� �и� (���뼺 ���)
    protected virtual void UpdateLaserPositionAndRotation()
    {
        // 1. ���� �÷��̾�κ��� offsetFromPlayer �Ÿ���ŭ ������ ��ġ ���
        Vector3 laserStartPos = owner.transform.position + direction * offsetFromPlayer;

        // 2. ������ �߽��� = ������ + (������ ���� / 2) -> OverlapBoxAll �Լ������� �߽����� �ؾ���
        transform.position = laserStartPos + direction * (size.x / 2f);

        // ȸ�� ����
        float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);
    }

    private void Update()
    {
        if (owner == null)
        {
            DestroySkill();
            return;
        }

        // ���� �ð� üũ
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }

        // DoT ������ ���� (Ȱ��ȭ�� ���)
        if (isDOT)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= delayBetweenDamage)
            {
                damageTimer = 0f;

                // DoT�̸� damagedTargets�� �ʱ�ȭ�ؼ� ���� ��󿡰� �ݺ� ������ ����
                damagedTargets.Clear();
                ApplyDamageToTargetsInRange();
            }
        }
    }

    protected IEnumerator FollowOwnerRotation()
    {
        while (owner != null && timer < duration)
        {
            if (straightLine)
            {
                // ������ ������ ���: ������ �׻� �÷��̾��� ���� ����
                direction = owner.transform.right.normalized;
            }
            else
            {
                // Ÿ�� ���� ���: ������ �������� ���� ����
                direction = owner.transform.right.normalized;
            }

            // �÷��̾�κ��� offsetFromPlayer �Ÿ���ŭ ������ ��ġ ���
            Vector3 laserStartPos = owner.transform.position + direction * offsetFromPlayer;

            // ������ �߽��� = ������ + (������ ���� / 2)
            transform.position = laserStartPos + direction * (size.x / 2f);

            // ȸ�� ������Ʈ
            float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            yield return null;
        }
    }

    protected virtual void ApplyDamageToTargetsInRange()
    {
        // ȸ���� �簢�� ������ �ִ� ��� �ݶ��̴� �˻�
        float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, size, rotationAngle);

        foreach (Collider2D collider in colliders)
        {
            // ���� ���� ������Ʈ ����
            if (collider.gameObject == gameObject) continue;

            // �̹� �������� ���� ��� ���� (DoT�� �ƴ� ���)
            int targetId = collider.gameObject.GetInstanceID();
            if (damagedTargets.Contains(targetId)) continue;

            // ����� BasicObject���� Ȯ��
            BasicObject targetObj = collider.GetComponent<BasicObject>();
            if (targetObj == null)
                targetObj = collider.GetComponentInParent<BasicObject>();

            if (targetObj != null && targetObj.isEnemy != owner.isEnemy)
            {
                // ������ ����
                targetObj.OnDamaged(owner, damage);

                // �������� ���� ��� ���
                damagedTargets.Add(targetId);
            }
        }
    }

    public override void DestroySkill()
    {
        // ��� �ڷ�ƾ ����
        StopAllCoroutines();

        // �⺻ ����
        base.DestroySkill();
        owner = null;
        damagedTargets.Clear();
    }

    // ����׿� ������ ���� �׸���
    private void OnDrawGizmos()
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);

        Gizmos.color = Color.blue;
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // ȸ�� ���� ��� (�÷��� ��尡 �ƴ� ���� �۵��ϵ���)
        float rotationAngle = 0f;
        if (Application.isPlaying && direction != Vector3.zero)
        {
            rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        else
        {
            // ������ ��忡���� ������Ʈ�� forward ���� ���
            rotationAngle = transform.eulerAngles.z;
        }

        // ȸ�� ��� ����
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            transform.position,
            Quaternion.Euler(0, 0, rotationAngle),
            Vector3.one
        );

        Gizmos.matrix = rotationMatrix;

        // ������ ������ �׸���
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.1f));

        // ������ ���� ǥ��
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, new Vector3(size.x / 2, 0, 0));

        // ������ �������� ���� ǥ��
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new Vector3(-size.x / 2, 0, 0), 0.1f); // ������
        Gizmos.DrawSphere(new Vector3(size.x / 2, 0, 0), 0.1f);  // ����

        // ������ �� ǥ��
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(0, -size.y / 2, 0), new Vector3(0, size.y / 2, 0));

        // ������ ������ ����� ��� �߰� ǥ��
        if (Application.isPlaying && straightLine)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        // ��� ����
        Gizmos.matrix = originalMatrix;
    }
}