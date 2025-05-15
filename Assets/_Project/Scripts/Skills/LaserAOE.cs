using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserAOE : SkillBase
{
    [Header("레이저 AOE 설정")]
    [SerializeField] private bool straightLine = false;            // 일직선 레이저 모드 (타겟 위치 무시하고 직선 방향으로 발사)
    [SerializeField] private Vector2 size = new Vector2(10f, 1f);  // 레이저 크기 (길이, 폭)
    [SerializeField] private float damage = 30f;                   // 기본 데미지
    [SerializeField] private float duration = 1.0f;                // 지속 시간
    [SerializeField] private bool isDOT = false;                   // 지속 데미지 여부
    [SerializeField] private float delayBetweenDamage = 0.1f;      // 데미지 적용 간격 (DoT 효과용)
    [SerializeField] private bool followOwnerRotation = false;     // 타겟 따라서 계속 회전할지 여부
    [SerializeField] private float offsetFromPlayer = 1.0f;        // 플레이어로부터의 거리 오프셋

    protected float timer = 0f;                  // 지속 시간 타이머
    protected float damageTimer = 0f;            // 데미지 타이머
    protected Vector3 direction;                 // 레이저 방향
    protected HashSet<int> damagedTargets;       // 이미 데미지를 입힌 대상 추적


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

        // 방향 설정
        if (straightLine)
        {
            // 일직선 레이저 모드: 플레이어의 현재 방향(right)을 사용
            direction = user.transform.right.normalized;
        }
        else
        {
            // 타겟 방향 모드: 전달받은 타겟 방향 사용
            direction = targetDirection.normalized;
        }

        // 레이저 위치 및 회전 설정
        UpdateLaserPositionAndRotation();

        // 즉시 첫 번째 데미지 적용
        ApplyDamageToTargetsInRange();

        // 소유자 추적 회전 시작 (활성화된 경우)
        if (followOwnerRotation)
        {
            StartCoroutine(FollowOwnerRotation());
        }
    }

    // 레이저 위치와 회전 업데이트 메서드 분리 (재사용성 향상)
    protected virtual void UpdateLaserPositionAndRotation()
    {
        // 1. 먼저 플레이어로부터 offsetFromPlayer 거리만큼 떨어진 위치 계산
        Vector3 laserStartPos = owner.transform.position + direction * offsetFromPlayer;

        // 2. 레이저 중심점 = 시작점 + (레이저 길이 / 2) -> OverlapBoxAll 함수때문에 중심으로 해야함
        transform.position = laserStartPos + direction * (size.x / 2f);

        // 회전 설정
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

        // 지속 시간 체크
        timer += Time.deltaTime;
        if (timer >= duration)
        {
            DestroySkill();
            return;
        }

        // DoT 데미지 적용 (활성화된 경우)
        if (isDOT)
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= delayBetweenDamage)
            {
                damageTimer = 0f;

                // DoT이면 damagedTargets을 초기화해서 같은 대상에게 반복 데미지 적용
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
                // 일직선 레이저 모드: 방향은 항상 플레이어의 현재 방향
                direction = owner.transform.right.normalized;
            }
            else
            {
                // 타겟 방향 모드: 방향은 소유자의 현재 방향
                direction = owner.transform.right.normalized;
            }

            // 플레이어로부터 offsetFromPlayer 거리만큼 떨어진 위치 계산
            Vector3 laserStartPos = owner.transform.position + direction * offsetFromPlayer;

            // 레이저 중심점 = 시작점 + (레이저 길이 / 2)
            transform.position = laserStartPos + direction * (size.x / 2f);

            // 회전 업데이트
            float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            yield return null;
        }
    }

    protected virtual void ApplyDamageToTargetsInRange()
    {
        // 회전된 사각형 영역에 있는 모든 콜라이더 검색
        float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position, size, rotationAngle);

        foreach (Collider2D collider in colliders)
        {
            // 같은 게임 오브젝트 제외
            if (collider.gameObject == gameObject) continue;

            // 이미 데미지를 입힌 대상 제외 (DoT이 아닌 경우)
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
            }
        }
    }

    public override void DestroySkill()
    {
        // 모든 코루틴 정지
        StopAllCoroutines();

        // 기본 정리
        base.DestroySkill();
        owner = null;
        damagedTargets.Clear();
    }

    // 디버그용 레이저 영역 그리기
    private void OnDrawGizmos()
    {
        transform.localScale = new Vector3(size.x, size.y, 1f);

        Gizmos.color = Color.blue;
        Matrix4x4 originalMatrix = Gizmos.matrix;

        // 회전 각도 계산 (플레이 모드가 아닐 때도 작동하도록)
        float rotationAngle = 0f;
        if (Application.isPlaying && direction != Vector3.zero)
        {
            rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        }
        else
        {
            // 에디터 모드에서는 오브젝트의 forward 방향 사용
            rotationAngle = transform.eulerAngles.z;
        }

        // 회전 행렬 적용
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(
            transform.position,
            Quaternion.Euler(0, 0, rotationAngle),
            Vector3.one
        );

        Gizmos.matrix = rotationMatrix;

        // 레이저 사이즈 그리기
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.1f));

        // 레이저 방향 표시
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, new Vector3(size.x / 2, 0, 0));

        // 레이저 시작점과 끝점 표시
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(new Vector3(-size.x / 2, 0, 0), 0.1f); // 시작점
        Gizmos.DrawSphere(new Vector3(size.x / 2, 0, 0), 0.1f);  // 끝점

        // 레이저 폭 표시
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(0, -size.y / 2, 0), new Vector3(0, size.y / 2, 0));

        // 일직선 레이저 모드일 경우 추가 표시
        if (Application.isPlaying && straightLine)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }

        // 행렬 복원
        Gizmos.matrix = originalMatrix;
    }
}