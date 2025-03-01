using BGDatabaseEnum;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class UnitManager : MonoBehaviour
{
    private static UnitManager _instance;

    private List<UnitController> units = new List<UnitController>();
    private List<UnitController> activeUnits = new List<UnitController>();

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

        // 기존 데이터 정리
        CleanUp();

        units = new List<UnitController>();
        activeUnits = new List<UnitController>();

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

        activeUnits.Clear();

        //공격 가능한 유닛만 담아두기
        for (int i = 0; i < units.Count; i++)
        {

            if (units[i] != null && units[i].canAttack)
            {
                activeUnits.Add(units[i]);
            }
        }

        // 공격 가능한 유닛이 없으면 리턴
        if (activeUnits.Count == 0) return;

        int enemyCount = EnemyManager.Instance.GetEnemyCount();

        try
        {
            // NativeArray 초기화
            InitializeArrays(activeUnits.Count, enemyCount);

            // 유닛 데이터 설정
            for (int i = 0; i < activeUnits.Count; i++)
            {
                unitPositions[i] = activeUnits[i].transform.position;
                attackRanges[i] = activeUnits[i].GetStat(StatName.AttackRange);
                attackTimers[i] = activeUnits[i].attackTimer;
            }

            //enemy 트랜스폼 enemyPositions에 담아오기 
            EnemyManager.Instance.GetEnemyPositions(enemyPositions);

            // Job 생성 및 실행
            var attackJob = new UnitAttackJob
            {
                UnitPositions = unitPositions,
                EnemyPositions = enemyPositions,
                AttackRanges = attackRanges,
                TargetIndices = targetIndices,
                AttackTimers = attackTimers,
                DeltaTime = Time.deltaTime
            };

            JobHandle jobHandle = attackJob.Schedule(activeUnits.Count, 64);
            jobHandle.Complete();

            // 공격 처리
            for (int i = 0; i < activeUnits.Count; i++)
            {
                int targetIndex = targetIndices[i];
                if (targetIndex != -1 && attackTimers[i] >= 1f / activeUnits[i].GetStat(StatName.AttackSpeed))
                {
                    UnitController unit = activeUnits[i];
                    Enemy targetEnemy = EnemyManager.Instance.GetEnemyAtIndex(targetIndex);
                    if (targetEnemy != null)
                    {
                        Vector3 targetPos = unit.attackType == SkillAttackType.Projectile ?
                            targetEnemy.transform.position : unit.transform.position;

                        AttackSkillManager.Instance.ActiveSkill(unit.unitData.f_SkillPoolingKey.f_PoolObjectAddressableKey, unit, targetPos);
                        unit.attackTimer = 0f;
                    }
                }
                else
                {
                    activeUnits[i].attackTimer = attackTimers[i];
                }
            }
        }
        finally
        {
            // 항상 NativeArray 정리 보장
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
            // 먼저 기존 타일 데이터 가져오기
            var existingTileData = TileMapManager.Instance.GetTileData(unit.tilePosition);

            if (existingTileData != null)
            {
                // 기존 타일 데이터의 placedUnit 초기화
                existingTileData.isAvailable = true;
                existingTileData.placedUnit = null;

                // 타일 데이터 업데이트
                TileMapManager.Instance.SetTileData(existingTileData);
            }


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

    
    public void CleanUp()
    {
        // Job System 배열들 정리
        DisposeArrays();

        for (int i = units.Count - 1; i >= 0; i--)
        {
            var unit = units[i];

            if (unit != null)
            {
                PoolingManager.Instance.ReturnObject(unit.gameObject);
            }
        }

        // 유닛 리스트 정리
        if (units != null)
        {
            units.Clear();
        }

        if (activeUnits != null)
        {
            activeUnits.Clear();
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