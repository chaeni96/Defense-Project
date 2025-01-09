// ProjectileManager: 투사체 관리 및 이동을 JobSystem을 통해 처리
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Jobs;
using UnityEngine;
using System.Linq;

public class ProjectileManager : MonoBehaviour
{
    private static ProjectileManager _instance;


    // 활성화된 투사체 목록과 JobSystem용 배열들
    private List<TheProjectile> activeProjectiles = new List<TheProjectile>();
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;  // 타겟 위치 배열 
    private NativeArray<float> speeds;  // 이동 속도 배열

    public static ProjectileManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ProjectileManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("ProjectileManager");
                    _instance = singleton.AddComponent<ProjectileManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void InitializeManager()
    {
        CleanUp();  // 기존 데이터 정리
        activeProjectiles = new List<TheProjectile>();
    }

    private void Update()
    {
        if (activeProjectiles.Count == 0) return;

        CheckDeadTargets();  // 죽은 타겟 처리
        if (activeProjectiles.Count == 0) return;

        UpdateProjectileArrays();  // JobSystem 배열 업데이트
        RunProjectileJob();  // 투사체 이동 Job 실행
        CheckHits();  // 충돌 체크
    }

    // 죽은 타겟을 가진 투사체 처리
    private void CheckDeadTargets()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            TheProjectile projectile = activeProjectiles[i];
            if (!IsTargetValid(projectile))
            {
                ReturnProjectile(projectile);
            }
        }
    }

    // 타겟 유효성 검사
    private bool IsTargetValid(TheProjectile projectile)
    {
        return projectile.target != null && projectile.target.gameObject.activeSelf;
    }

    // JobSystem 배열 초기화 및 업데이트
    private void UpdateProjectileArrays()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        Transform[] transforms = activeProjectiles.Select(p => p.transform).ToArray();
        transformAccessArray = new TransformAccessArray(transforms);

        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        targetPositions = new NativeArray<float3>(activeProjectiles.Count, Allocator.Persistent);
        speeds = new NativeArray<float>(activeProjectiles.Count, Allocator.Persistent);

        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            targetPositions[i] = activeProjectiles[i].target.transform.position;
            speeds[i] = activeProjectiles[i].owner.GetStat(StatName.ProjectileSpeed);
        }
    }

    // 투사체 이동 Job 실행
    private void RunProjectileJob()
    {
        var projectileJob = new ProjectileMoveJob
        {
            TargetPositions = targetPositions,
            Speeds = speeds,
            DeltaTime = Time.deltaTime
        };

        JobHandle jobHandle = projectileJob.Schedule(transformAccessArray);
        jobHandle.Complete();
    }

    // 타겟과의 충돌 체크
    private void CheckHits()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            TheProjectile projectile = activeProjectiles[i];
            if (!projectile.gameObject.activeSelf) continue;

            if (IsHit(projectile))
            {
                ApplyDamage(projectile);
                ReturnProjectile(projectile);
            }
        }
    }

    // 충돌 거리 체크
    private bool IsHit(TheProjectile projectile)
    {
        return Vector2.Distance(projectile.transform.position, projectile.target.transform.position) < 0.1f;
    }

    // 데미지 적용
    private void ApplyDamage(TheProjectile projectile)
    {
        if (IsTargetValid(projectile))
        {
            projectile.target.onDamaged(projectile.owner, projectile.owner.GetStat(StatName.ATK));
        }
    }

    public List<TheProjectile> GetProjectilesTargetingEnemy(Enemy enemy)
    {
        return activeProjectiles.Where(p => p != null && p.target == enemy).ToList();
    }



    // 투사체 등록
    public void RegisterProjectile(TheProjectile projectile)
    {
        if (!activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
        }
    }

    // 투사체 오브젝트 풀 반환
    private void ReturnProjectile(TheProjectile projectile)
    {
        projectile.CleanUp();
        PoolingManager.Instance.ReturnObject(projectile.gameObject);
        activeProjectiles.Remove(projectile);
    }

    private void CleanUp()
    {
        // Job System 리소스 정리
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        // 활성화된 투사체 목록 정리
        activeProjectiles.Clear();
    }

}

public struct ProjectileMoveJob : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<float3> TargetPositions;
    [ReadOnly] public NativeArray<float> Speeds;
    public float DeltaTime;

    public void Execute(int index, TransformAccess transform)
    {
        float3 currentPos = transform.position;
        float3 targetPos = TargetPositions[index];

        // 현재 위치에서 타겟까지의 방향
        float3 direction = math.normalize(targetPos - currentPos);

        // 이동할 거리 계산
        float moveDistance = Speeds[index] * DeltaTime;
        float distanceToTarget = math.distance(currentPos, targetPos);

        // 타겟까지 거리가 이동 거리보다 작으면 바로 타겟 위치로
        if (distanceToTarget <= moveDistance)
        {
            transform.position = targetPos;
        }
        else
        {
            // 정확한 방향으로 이동
            float3 newPosition = currentPos + direction * moveDistance;
            transform.position = newPosition;
        }
    }



}