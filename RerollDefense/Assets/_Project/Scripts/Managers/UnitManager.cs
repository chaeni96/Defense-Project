using BGDatabaseEnum;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    private static UnitManager _instance;
    public static UnitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<UnitManager>();
                if (_instance == null)
                {
                    GameObject singleton = new GameObject("UnitManager");
                    _instance = singleton.AddComponent<UnitManager>();
                    DontDestroyOnLoad(singleton);
                }
            }
            return _instance;
        }
    }

    private List<UnitController> units = new List<UnitController>();

    //job System º¯¼ö
    private NativeArray<float3> unitPositions;
    private NativeArray<float3> enemyPositions;
    private NativeArray<float> attackRanges;
    private NativeArray<int> targetIndices;
    private NativeArray<float> attackTimers;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        units = new List<UnitController>();
    }

    public void RegisterUnit(UnitController unit)
    {
        if (!units.Contains(unit))
        {
            units.Add(unit);
        }
    }

    public void UnregisterUnit(UnitController unit)
    {
        if (units.Contains(unit))
        {
            units.Remove(unit);
        }
    }

    private void Update()
    {
        if (units.Count == 0) return;

        List<Enemy> enemies = EnemyManager.Instance.GetEnemies();
        if (enemies.Count == 0) return;

        InitializeArrays(units.Count, enemies.Count);

        for (int i = 0; i < units.Count; i++)
        {
            unitPositions[i] = units[i].transform.position;
            attackRanges[i] = units[i].attackRange;
            attackTimers[i] = units[i].attackTimer;
        }

        for (int i = 0; i < enemies.Count; i++)
        {
            enemyPositions[i] = enemies[i].transform.position;
        }

        var attackJob = new UnitAttackJob
        {
            UnitPositions = unitPositions,
            EnemyPositions = enemyPositions,
            AttackRanges = attackRanges,
            TargetIndices = targetIndices,
            AttackTimers = attackTimers,
            DeltaTime = Time.deltaTime
        };

        JobHandle jobHandle = attackJob.Schedule(units.Count, 64);
        jobHandle.Complete();

        for (int i = 0; i < units.Count; i++)
        {
            int targetIndex = targetIndices[i];
            if (targetIndex != -1 && attackTimers[i] >= units[i].attackCoolDown)
            {
                UnitController unit = units[i];
                float attackRange = unit.attackRange;

                if (unit.attackType == SkillAttackType.Projectile)
                {
                    AttackSkillManager.Instance.ActiveSkill(unit, enemies[targetIndex]);
                }
                else if (unit.attackType == SkillAttackType.AOE)
                {
                    AttackSkillManager.Instance.ActiveSkill(unit, enemies);
                }

                unit.attackTimer = 0f;
            }
            else
            {
                units[i].attackTimer = attackTimers[i];
            }
        }

        DisposeArrays();
    }

    private void InitializeArrays(int unitCount, int enemyCount)
    {
        if (unitPositions.IsCreated) unitPositions.Dispose();
        if (enemyPositions.IsCreated) enemyPositions.Dispose();
        if (attackRanges.IsCreated) attackRanges.Dispose();
        if (targetIndices.IsCreated) targetIndices.Dispose();
        if (attackTimers.IsCreated) attackTimers.Dispose();

        unitPositions = new NativeArray<float3>(unitCount, Allocator.TempJob);
        enemyPositions = new NativeArray<float3>(enemyCount, Allocator.TempJob);
        attackRanges = new NativeArray<float>(unitCount, Allocator.TempJob);
        targetIndices = new NativeArray<int>(unitCount, Allocator.TempJob);
        attackTimers = new NativeArray<float>(unitCount, Allocator.TempJob);
    }

    private void DisposeArrays()
    {
        if (unitPositions.IsCreated) unitPositions.Dispose();
        if (enemyPositions.IsCreated) enemyPositions.Dispose();
        if (attackRanges.IsCreated) attackRanges.Dispose();
        if (targetIndices.IsCreated) targetIndices.Dispose();
        if (attackTimers.IsCreated) attackTimers.Dispose();
    }

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        DisposeArrays();
        units.Clear();
    }

    private void OnApplicationQuit()
    {
        CleanUp();
    }

    private void OnDisable()
    {
        CleanUp();
    }
}

public struct UnitAttackJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> UnitPositions;
    [ReadOnly] public NativeArray<float3> EnemyPositions;
    [ReadOnly] public NativeArray<float> AttackRanges;
    [ReadOnly] public float DeltaTime;

    public NativeArray<int> TargetIndices;
    public NativeArray<float> AttackTimers;

    public void Execute(int index)
    {
        float3 unitPos = UnitPositions[index];
        float attackRange = AttackRanges[index];
        float nearestDist = float.MaxValue;
        int nearestEnemyIndex = -1;

        for (int i = 0; i < EnemyPositions.Length; i++)
        {
            float3 enemyPos = EnemyPositions[i];
            float dist = math.distance(unitPos, enemyPos);

            if (dist <= attackRange && dist < nearestDist)
            {
                nearestDist = dist;
                nearestEnemyIndex = i;
            }
        }

        TargetIndices[index] = nearestEnemyIndex;

        if (nearestEnemyIndex != -1)
        {
            AttackTimers[index] += DeltaTime;
        }
    }
}