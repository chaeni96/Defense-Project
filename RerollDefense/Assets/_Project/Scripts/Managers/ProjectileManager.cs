using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

public class ProjectileManager : MonoBehaviour
{
    private static ProjectileManager _instance;

    private List<TheProjectile> activeProjectiles = new List<TheProjectile>();
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;
    private NativeArray<float> speeds;

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

    private void Update()
    {
        if (activeProjectiles.Count == 0) return;

        // 죽은 타겟을 가진 투사체 제거
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            if (activeProjectiles[i].target == null)
            {
                PoolingManager.Instance.ReturnObject(activeProjectiles[i].gameObject);
                activeProjectiles.RemoveAt(i);
                continue;
            }
        }

        if (activeProjectiles.Count == 0) return;

        UpdateProjectileArrays();
        RunProjectileJob();
        CheckHits();
    }

    private void UpdateProjectileArrays()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        Transform[] transforms = activeProjectiles.Select(p => p.transform).ToArray();
        transformAccessArray = new TransformAccessArray(transforms);

        // 기존 배열 해제
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        // Persistent 할당자로 변경
        targetPositions = new NativeArray<float3>(activeProjectiles.Count, Allocator.Persistent);
        speeds = new NativeArray<float>(activeProjectiles.Count, Allocator.Persistent);

        for (int i = 0; i < activeProjectiles.Count; i++)
        {
            targetPositions[i] = activeProjectiles[i].target.transform.position;
            speeds[i] = activeProjectiles[i].owner.attackSpeed;
        }
    }

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

    private void CheckHits()
    {
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            TheProjectile projectile = activeProjectiles[i];
            float dist = Vector2.Distance(projectile.transform.position, projectile.target.transform.position);
            if (dist < 0.1f)
            {
                projectile.target.onDamaged(projectile.owner, projectile.damage);

                PoolingManager.Instance.ReturnObject(projectile.gameObject);
                activeProjectiles.RemoveAt(i);
            }
        }
    }

    public void RegisterProjectile(TheProjectile projectile)
    {
        if (!activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
        }
    }

    private void OnDestroy()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();
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

        float3 direction = math.normalize(targetPos - currentPos);
        float3 newPosition = currentPos + direction * Speeds[index] * DeltaTime;

        transform.position = newPosition;
    }
}