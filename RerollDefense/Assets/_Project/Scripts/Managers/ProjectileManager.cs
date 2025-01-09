// ProjectileManager: ����ü ���� �� �̵��� JobSystem�� ���� ó��
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


    // Ȱ��ȭ�� ����ü ��ϰ� JobSystem�� �迭��
    private List<TheProjectile> activeProjectiles = new List<TheProjectile>();
    private TransformAccessArray transformAccessArray;
    private NativeArray<float3> targetPositions;  // Ÿ�� ��ġ �迭 
    private NativeArray<float> speeds;  // �̵� �ӵ� �迭

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
        CleanUp();  // ���� ������ ����
        activeProjectiles = new List<TheProjectile>();
    }

    private void Update()
    {
        if (activeProjectiles.Count == 0) return;

        CheckDeadTargets();  // ���� Ÿ�� ó��
        if (activeProjectiles.Count == 0) return;

        UpdateProjectileArrays();  // JobSystem �迭 ������Ʈ
        RunProjectileJob();  // ����ü �̵� Job ����
        CheckHits();  // �浹 üũ
    }

    // ���� Ÿ���� ���� ����ü ó��
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

    // Ÿ�� ��ȿ�� �˻�
    private bool IsTargetValid(TheProjectile projectile)
    {
        return projectile.target != null && projectile.target.gameObject.activeSelf;
    }

    // JobSystem �迭 �ʱ�ȭ �� ������Ʈ
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

    // ����ü �̵� Job ����
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

    // Ÿ�ٰ��� �浹 üũ
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

    // �浹 �Ÿ� üũ
    private bool IsHit(TheProjectile projectile)
    {
        return Vector2.Distance(projectile.transform.position, projectile.target.transform.position) < 0.1f;
    }

    // ������ ����
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



    // ����ü ���
    public void RegisterProjectile(TheProjectile projectile)
    {
        if (!activeProjectiles.Contains(projectile))
        {
            activeProjectiles.Add(projectile);
        }
    }

    // ����ü ������Ʈ Ǯ ��ȯ
    private void ReturnProjectile(TheProjectile projectile)
    {
        projectile.CleanUp();
        PoolingManager.Instance.ReturnObject(projectile.gameObject);
        activeProjectiles.Remove(projectile);
    }

    private void CleanUp()
    {
        // Job System ���ҽ� ����
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        // Ȱ��ȭ�� ����ü ��� ����
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

        // ���� ��ġ���� Ÿ�ٱ����� ����
        float3 direction = math.normalize(targetPos - currentPos);

        // �̵��� �Ÿ� ���
        float moveDistance = Speeds[index] * DeltaTime;
        float distanceToTarget = math.distance(currentPos, targetPos);

        // Ÿ�ٱ��� �Ÿ��� �̵� �Ÿ����� ������ �ٷ� Ÿ�� ��ġ��
        if (distanceToTarget <= moveDistance)
        {
            transform.position = targetPos;
        }
        else
        {
            // ��Ȯ�� �������� �̵�
            float3 newPosition = currentPos + direction * moveDistance;
            transform.position = newPosition;
        }
    }



}