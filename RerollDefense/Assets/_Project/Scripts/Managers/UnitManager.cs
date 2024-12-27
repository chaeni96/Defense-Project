using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
public class UnitManager : MonoBehaviour
{
    //job System���� ������ ���� ��� �ѹ��� ó��
    public static UnitManager _instance;

    private List<UnitController> units = new List<UnitController>();
    private NativeArray<float3> unitPositions;
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

    public void RegisterUnit(UnitController unit)
    {
        units.Add(unit);
    }

    public void UnregisterUnit(UnitController unit)
    {
        units.Remove(unit);
    }

    private void Update()
    {
        if (units.Count == 0) return;

        // NativeArray �ʱ�ȭ
        if (unitPositions.IsCreated) unitPositions.Dispose();
        if (attackRanges.IsCreated) attackRanges.Dispose();
        if (targetIndices.IsCreated) targetIndices.Dispose();
        if (attackTimers.IsCreated) attackTimers.Dispose();

        unitPositions = new NativeArray<float3>(units.Count, Allocator.TempJob);
        attackRanges = new NativeArray<float>(units.Count, Allocator.TempJob);
        targetIndices = new NativeArray<int>(units.Count, Allocator.TempJob);
        attackTimers = new NativeArray<float>(units.Count, Allocator.TempJob);

        // ������ ����
        for (int i = 0; i < units.Count; i++)
        {
            unitPositions[i] = units[i].transform.position;
            attackRanges[i] = units[i].attackRange;
            // ���� attackTimer ���� �����ͼ� ����
            attackTimers[i] = units[i].attackTimer;   // UnitController�� attackTimer �ʵ� �߰� �ʿ�
        }

        // Enemy ��ġ ������ ��������
        var enemies = EnemyManager.Instance.GetEnemies();
        var enemyPositions = new NativeArray<float3>(enemies.Count, Allocator.TempJob);
        for (int i = 0; i < enemies.Count; i++)
        {
            enemyPositions[i] = enemies[i].transform.position;
        }

        // Job ����
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

        // ��� ó��
        for (int i = 0; i < units.Count; i++)
        {
            int targetIndex = targetIndices[i];
            float currentTimer = attackTimers[i];

            if (targetIndex != -1)
            {
                if (currentTimer >= units[i].attackCoolDown)
                {
                    //AttackSkillManager.Instance.ActiveSkill(units[i], enemies[targetIndex]);
                    units[i].attackTimer = 0; // Ÿ�̸� ����
                }
                else
                {
                    units[i].attackTimer += Time.deltaTime; // Ÿ�̸� ����
                }
            }
        }

        // ����
        unitPositions.Dispose();
        enemyPositions.Dispose();
        attackRanges.Dispose();
        targetIndices.Dispose();
        attackTimers.Dispose();
    }


}



public struct UnitAttackJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float3> UnitPositions;
    [ReadOnly] public NativeArray<float3> EnemyPositions;
    [ReadOnly] public NativeArray<float> AttackRanges;
    [ReadOnly] public float DeltaTime;

    public NativeArray<int> TargetIndices;        // �� ������ Ÿ�� �ε���
    public NativeArray<float> AttackTimers;       // �� ������ ���� Ÿ�̸�

    public void Execute(int index)
    {
        float3 unitPos = UnitPositions[index];
        float attackRange = AttackRanges[index];
        float nearestDist = float.MaxValue;
        int nearestEnemyIndex = -1;

        // ���� ����� �� ã��
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
