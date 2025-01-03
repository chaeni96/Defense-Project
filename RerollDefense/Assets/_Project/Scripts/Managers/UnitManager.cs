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

    //job System 변수
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

        //enemy가 한마리도 없으면 return
        if (EnemyManager.Instance.GetEnemyCount() == 0) return;

        int enemyCount = EnemyManager.Instance.GetEnemyCount();
        InitializeArrays(units.Count, enemyCount);

        for (int i = 0; i < units.Count; i++)
        {
            unitPositions[i] = units[i].transform.position;
            attackRanges[i] = units[i].attackRange;
            attackTimers[i] = units[i].attackTimer;
        }

        //enemy 트랜스폼 enemyPositions에 담아오기 
        //NativeArray는 참조타입이므로 EnemyManager에서 값 변경되면 똑같이 적용됨
        EnemyManager.Instance.GetEnemyPositions(enemyPositions);


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
        // 공격 처리
        for (int i = 0; i < units.Count; i++)
        {
            int targetIndex = targetIndices[i];
            if (targetIndex != -1 && attackTimers[i] >= units[i].attackCoolDown)
            {
                UnitController unit = units[i];
                Enemy targetEnemy = EnemyManager.Instance.GetEnemyAtIndex(targetIndex);
                if (targetEnemy != null)
                {
                    string skillId = unit.attackType == SkillAttackType.Projectile ? "ProjectileObject" : "AoeRangeObject";
                    Vector3 targetPos = unit.attackType == SkillAttackType.Projectile ?
                        targetEnemy.transform.position : unit.transform.position;

                    if (unit.attackType == SkillAttackType.Projectile)
                    {
                        unit.MoveScale();
                    }

                    AttackSkillManager.Instance.ActiveSkill(skillId, unit, targetPos);
                    unit.attackTimer = 0f;
                }
            }
            else
            {
                units[i].attackTimer = attackTimers[i];
            }
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
            // 타일 위치를 배치 가능한 상태로 변경
            Vector3Int basePosition = TileMapManager.Instance.tileMap.WorldToCell(unit.transform.position);
            TileMapManager.Instance.SetTileData(basePosition, true);

            units.Remove(unit);
            PoolingManager.Instance.ReturnObject(unit.gameObject);
        }
    }


    // 게임 상태 변경 시 호출할 메서드
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

    private void OnDestroy()
    {
        CleanUp();
    }

    private void CleanUp()
    {
        DisposeArrays();
        units.Clear();
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