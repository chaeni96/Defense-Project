using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase
{
    [Header("AOE ����")]
    [SerializeField] private float radius = 1f;                // AOE ���� �ݰ�

    private float timer = 0f;                // ���� �ð� Ÿ�̸�
    private HashSet<int> damagedTargets;     // �̹� �������� ���� ��� ����

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
        damagedTargets = new HashSet<int>();
    }

    public override void Fire(BasicObject target)
    {
        timer = 0f;
        damagedTargets.Clear();

        // Ÿ����ġ��, Ÿ�� ��ġ�� state���� �Ѱ��ֱ�
        transform.position = target.transform.position;

        // ��� ù ��° ������ ����
        ApplyDamageToTargetsInRange();
    }

    private void Update()
    {
        if (ownerObj == null)
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
    }

    private void ApplyDamageToTargetsInRange()
    {
        // ���� ��ġ���� radius �ݰ� ���� ��� �ݶ��̴� �˻�
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D collider in colliders)
        {
            // ���� ���� ������Ʈ ����
            if (collider.gameObject == gameObject) continue;

            // �̹� �������� ���� ��� ����
            int targetId = collider.gameObject.GetInstanceID();
            if (damagedTargets.Contains(targetId)) continue;

            // ����� BasicObject���� Ȯ��
            BasicObject targetObj = collider.GetComponent<BasicObject>();
            if (targetObj == null)
                targetObj = collider.GetComponentInParent<BasicObject>();

            if (targetObj != null && targetObj.isEnemy != ownerObj.isEnemy)
            {
                // ������ ����
                targetObj.OnDamaged(damage);

                // �������� ���� ��� ���
                damagedTargets.Add(targetId);

                Debug.Log($"AOE ��󿡰� ������: {targetObj.name}, ������={damage}");
            }
        }
    }


    public override void DestroySkill()
    {
        base.DestroySkill();

        ownerObj = null;

        damagedTargets.Clear();
    }

    // ����׿� �ݰ� �׸���
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }


}
