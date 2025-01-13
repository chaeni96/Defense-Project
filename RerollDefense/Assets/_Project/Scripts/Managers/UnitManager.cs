using BGDatabaseEnum;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class UnitManager : MonoBehaviour
{
    private static UnitManager _instance;

    private List<UnitController> units = new List<UnitController>();

    //job System ����
    private NativeArray<float3> unitPositions;
    private NativeArray<float3> enemyPositions;
    private NativeArray<float> attackRanges;
    private NativeArray<int> targetIndices;
    private NativeArray<float> attackTimers;


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

        // ���� ������ ����
        CleanUp();

        units = new List<UnitController>();

    }

    public List<UnitController> GetUnits() => units;

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

    private void Update()
    {
        //enemy�� �Ѹ����� ������ return
        if (EnemyManager.Instance.GetEnemyCount() == 0) return;

        int enemyCount = EnemyManager.Instance.GetEnemyCount();

        try
        {
            // NativeArray �ʱ�ȭ
            InitializeArrays(units.Count, enemyCount);

            // ���� ������ ����
            for (int i = 0; i < units.Count; i++)
            {
                unitPositions[i] = units[i].transform.position;
                attackRanges[i] = units[i].GetStat(StatName.AttackRange);
                attackTimers[i] = units[i].attackTimer;
            }

            //enemy Ʈ������ enemyPositions�� ��ƿ��� 
            EnemyManager.Instance.GetEnemyPositions(enemyPositions);

            // Job ���� �� ����
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

            // ���� ó��
            for (int i = 0; i < units.Count; i++)
            {
                int targetIndex = targetIndices[i];
                if (targetIndex != -1 && attackTimers[i] >= 1f / units[i].GetStat(StatName.AttackSpeed))
                {
                    UnitController unit = units[i];
                    Enemy targetEnemy = EnemyManager.Instance.GetEnemyAtIndex(targetIndex);
                    if (targetEnemy != null)
                    {
                        Vector3 targetPos = unit.attackType == SkillAttackType.Projectile ?
                            targetEnemy.transform.position : unit.transform.position;

                        if (unit.attackType == SkillAttackType.Projectile)
                        {
                            unit.MoveScale();
                        }

                        AttackSkillManager.Instance.ActiveSkill(unit.unitData.f_SkillPoolingKey.f_PoolObjectAddressableKey, unit, targetPos);
                        unit.attackTimer = 0f;
                    }
                }
                else
                {
                    units[i].attackTimer = attackTimers[i];
                }
            }
        }
        finally
        {
            // �׻� NativeArray ���� ����
            DisposeArrays();
        }
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
            // Ÿ�� ��ġ�� ��ġ ������ ���·� ����            
            var tileData = new TileData(unit.tilePosition)
            { 
                isAvailable = true 
            };
            TileMapManager.Instance.SetTileData(tileData);

            units.Remove(unit);
            PoolingManager.Instance.ReturnObject(unit.gameObject);
        }
    }


    // ���� ���� ���� �� ȣ���� �޼���
    public void SetUnitsActive(bool active)
    {
        foreach (var unit in units)
        {
            unit.SetActive(active);
        }
    }


    private void DisposeArrays()
    {
        if (unitPositions.IsCreated) unitPositions.Dispose();
        if (enemyPositions.IsCreated) enemyPositions.Dispose();
        if (attackRanges.IsCreated) attackRanges.Dispose();
        if (targetIndices.IsCreated) targetIndices.Dispose();
        if (attackTimers.IsCreated) attackTimers.Dispose();
    }

    
    public void CleanUp()
    {
        // Job System �迭�� ����
        DisposeArrays();

        for (int i = units.Count - 1; i >= 0; i--)
        {
            var unit = units[i];

            if (unit != null)
            {
                PoolingManager.Instance.ReturnObject(unit.gameObject);
            }
        }

        // ���� ����Ʈ ����
        if (units != null)
        {
            units.Clear();
        }
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