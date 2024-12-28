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
            if (_instance == null && !isQuitting)
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

        // ���� Ÿ���� ���� ����ü ó���� ����
        for (int i = activeProjectiles.Count - 1; i >= 0; i--)
        {
            TheProjectile projectile = activeProjectiles[i];
            // Ÿ���� ���ų� ��Ȱ��ȭ�� ���
            if (projectile.target == null || !projectile.target.gameObject.activeSelf)
            {
                PoolingManager.Instance.ReturnObject(projectile.gameObject);
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

        // ���� �迭 ����
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        // Persistent �Ҵ��ڷ� ����
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
            if (!projectile.gameObject.activeSelf) continue;

            float dist = Vector2.Distance(projectile.transform.position, projectile.target.transform.position);
            if (dist < 0.1f)
            {
                // ��Ʈ ó�� ���� Ÿ�� ��ȿ�� �ѹ� �� üũ
                if (projectile.target != null && projectile.target.gameObject.activeSelf)
                {
                    projectile.target.onDamaged(projectile.owner, projectile.damage);
                }

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

    // Ư�� enemy�� Ÿ������ �ϴ� ��� projectile ��ȯ
    public List<TheProjectile> GetProjectilesTargetingEnemy(Enemy enemy)
    {
        return activeProjectiles.Where(p => p != null && p.target == enemy).ToList();
    }

    private void OnDestroy()
    {
        if (transformAccessArray.isCreated) transformAccessArray.Dispose();
        if (targetPositions.IsCreated) targetPositions.Dispose();
        if (speeds.IsCreated) speeds.Dispose();

        activeProjectiles.Clear();
    }

    private static bool isQuitting = false;

    private void OnApplicationQuit()
    {
        isQuitting = true;
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