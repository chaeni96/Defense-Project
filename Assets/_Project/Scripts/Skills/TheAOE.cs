using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TheAOE : SkillBase
{
    [Header("AOE 설정")]
    [SerializeField] private float radius = 1f;                // AOE 범위 반경
    [SerializeField] private float damage = 1f;               // 기본 데미지
    [SerializeField] private float duration = 1.0f;            // 지속 시간 (1초)

    private float timer = 0f;                // 지속 시간 타이머
    private HashSet<int> damagedTargets;     // 이미 데미지를 입힌 대상 추적

    public override void Initialize(BasicObject unit)
    {
        base.Initialize(unit);
        damagedTargets = new HashSet<int>();
    }

    public override void Fire(BasicObject user, Vector3 targetPos, Vector3 targetDirection, BasicObject target = null)
    {
        owner = user;
        timer = 0f;
        damagedTargets.Clear();

        // 타겟위치로, 타겟 위치는 state에서 넘겨주기
        transform.position = targetPos;


        // 즉시 첫 번째 데미지 적용
        ApplyDamageToTargetsInRange();
    }

    private void Update()
    {
        if (owner == null)
        {
            DestroySkill();
            return;
        }

        // 지속 시간 체크
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }
    }

    private void ApplyDamageToTargetsInRange()
    {
        // 현재 위치에서 radius 반경 내의 모든 콜라이더 검색
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);

        foreach (Collider2D collider in colliders)
        {
            // 같은 게임 오브젝트 제외
            if (collider.gameObject == gameObject) continue;

            // 이미 데미지를 입힌 대상 제외
            int targetId = collider.gameObject.GetInstanceID();
            if (damagedTargets.Contains(targetId)) continue;

            // 대상이 BasicObject인지 확인
            BasicObject targetObj = collider.GetComponent<BasicObject>();
            if (targetObj == null)
                targetObj = collider.GetComponentInParent<BasicObject>();

            if (targetObj != null && targetObj.isEnemy != owner.isEnemy)
            {
                // 데미지 적용
                targetObj.OnDamaged(owner, damage);

                // 데미지를 입힌 대상 기록
                damagedTargets.Add(targetId);

                Debug.Log($"AOE 대상에게 데미지: {targetObj.name}, 데미지={damage}");
            }
        }
    }


    public override void DestroySkill()
    {
        base.DestroySkill();

        owner = null;

        damagedTargets.Clear();
    }

    // 디버그용 반경 그리기
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radius);
    }


}
