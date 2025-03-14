using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph;
using UnityEngine;

public abstract class WaveBase
{
    protected D_WaveDummyData waveData;
    protected bool isWaveCompleted = false;

    public WaveBase(D_WaveDummyData data)
    {
        waveData = data;
    }

    // 모든 웨이브에서 공통적으로 구현해야 할 메서드
    public abstract void StartWave();
    public abstract void EndWave();

    //웨이브 설명반환
    public abstract string GetWaveInfoText();

    //웨이브 완료 체크
    public virtual void CheckWaveCompletion()
    {
        if (isWaveCompleted)
        {
            StageManager.Instance.OnWaveComplete();
        }
    }

    // 선택적으로 오버라이드할 수 있는 가상 메서드들, 시간 관련 웨이브, 체크 조건이 있는 웨이브들
    public virtual void HandleTimeChange(float remainTime) { }
    public virtual void HandleScheduleComplete(int scheduleUID) { }

    public virtual void AddEnemies(int count) { }

    public virtual void DecreaseEnemyCount() { }

}

/// <summary>
/// 일반 전투 웨이브 클래스
/// </summary>
public class NormalBattleWave : WaveBase
{
    private D_NormalBattleWaveData normalWaveData;
    private int totalGroupCount = 0;
    private int completedGroupCount = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    public NormalBattleWave(D_NormalBattleWaveData data) : base(data)
    {
        normalWaveData = data;
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "일반 전투 웨이브\n";

        // 같은 적 타입끼리 그룹화해서 표시
        var groupedEnemies = normalWaveData.f_enemyGroups
                            .GroupBy(g => g.f_enemy.f_name)
                            .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.TotalAmount}\n";
        }

        return waveInfo;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        // 총 적 수 계산
        remainEnemies = normalWaveData.f_enemyGroups.Sum(group => group.f_amount);
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // 적 스폰 시작
        SpawnWaveEnemies();
    }

    private void SpawnWaveEnemies()
    {
        totalGroupCount = normalWaveData.f_enemyGroups.Count;
        completedGroupCount = 0;

        foreach (D_enemyGroups groupData in normalWaveData.f_enemyGroups)
        {
            StageManager.Instance.StartCoroutine(CoSpawnEnemyGroup(groupData));
        }
    }

    private IEnumerator CoSpawnEnemyGroup(D_enemyGroups enemyGroupData)
    {
        if (enemyGroupData == null || enemyGroupData.f_enemy == null)
        {
            Debug.LogError("적 그룹 데이터 없음");
            yield break;
        }

        yield return new WaitForSeconds(enemyGroupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < enemyGroupData.f_amount; spawnedCount++)
        {
            if (enemyGroupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(enemyGroupData.f_enemy);
            }
            else
            {
                Debug.LogError("오브젝트 풀 키가 없음");
                break;
            }

            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }

        ++completedGroupCount;

        // 모든 그룹이 스폰을 완료했는지 확인
        if (completedGroupCount >= totalGroupCount)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }

    public override void AddEnemies(int count)
    {
        remainEnemies += count;
        StageManager.Instance.UpdateEnemyCount(count);
    }

    public override void DecreaseEnemyCount()
    {
        --remainEnemies;
        CheckWaveCompletion();
    }

    public override void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0 && !isWaveCompleted)
        {
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
    }

    public override void EndWave()
    {
        // 필요한 리소스 정리
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}

/// <summary>
/// 보스 전투 웨이브 클래스
/// </summary>
public class BossBattleWave : WaveBase
{
    private D_BossBattleWaveData bossWaveData;
    private int totalGroupCount = 0;
    private int completedGroupCount = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    public BossBattleWave(D_BossBattleWaveData data) : base(data)
    {
        bossWaveData = data;
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "보스 전투 웨이브\n";
        waveInfo += $"보스: {bossWaveData.f_bossEnemy.f_name}\n";

        // 서포트 적이 있다면 표시
        if (bossWaveData.f_supportEnemyGroups != null && bossWaveData.f_supportEnemyGroups.Count > 0)
        {
            var groupedEnemies = bossWaveData.f_supportEnemyGroups
                                .GroupBy(g => g.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "서포터:";
            foreach (var group in groupedEnemies)
            {
                waveInfo += $"{group.Name} x{group.TotalAmount}\n";
            }
        }

        return waveInfo;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        // 보스 + 서포트 적 총 수 계산
        remainEnemies = 1; // 보스
        if (bossWaveData.f_supportEnemyGroups != null)
        {
            remainEnemies += bossWaveData.f_supportEnemyGroups.Sum(group => group.f_amount);
        }

        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // 보스와 서포트 적 스폰
        SpawnBossAndSupportEnemies();
    }

    private void SpawnBossAndSupportEnemies()
    {
        // 총 스폰할 그룹 수 계산 (보스 + 서포트 그룹)
        totalGroupCount = 1; // 보스 그룹
        if (bossWaveData.f_supportEnemyGroups != null)
        {
            totalGroupCount += bossWaveData.f_supportEnemyGroups.Count;
        }

        completedGroupCount = 0;

        // 보스 스폰
        StageManager.Instance.StartCoroutine(CoSpawnBoss());

        // 서포트 적 스폰
        if (bossWaveData.f_supportEnemyGroups != null && bossWaveData.f_supportEnemyGroups.Count > 0)
        {
            foreach (D_supportEnemyGroups group in bossWaveData.f_supportEnemyGroups)
            {
                StageManager.Instance.StartCoroutine(CoSpawnSupportGroup(group));
            }
        }
    }

    private IEnumerator CoSpawnBoss()
    {
        yield return new WaitForSeconds(bossWaveData.f_startDelay);

        if (bossWaveData.f_bossEnemy.f_ObjectPoolKey != null)
        {
            EnemyManager.Instance.SpawnEnemy(bossWaveData.f_bossEnemy);
        }
        else
        {
            Debug.LogError("보스 오브젝트 풀 키가 없음");
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private IEnumerator CoSpawnSupportGroup(D_supportEnemyGroups groupData)
    {
        if (groupData == null || groupData.f_enemy == null)
        {
            Debug.LogError("서포트 그룹 데이터 오류");
            yield break;
        }

        yield return new WaitForSeconds(groupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < groupData.f_amount; spawnedCount++)
        {
            if (groupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy);
            }
            else
            {
                Debug.LogError("서포트 적 오브젝트 풀 키가 없음");
                break;
            }

            yield return new WaitForSeconds(groupData.f_spawnInterval);
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private void CheckSpawnCompletion()
    {
        if (completedGroupCount >= totalGroupCount)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }

    public override void AddEnemies(int count)
    {
        remainEnemies += count;
        StageManager.Instance.UpdateEnemyCount(count);
    }

    public override void DecreaseEnemyCount()
    {
        --remainEnemies;
        CheckWaveCompletion();
    }

    public override void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0 && !isWaveCompleted)
        {
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
    }

    public override void EndWave()
    {
        // 필요한 리소스 정리
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}

/// <summary>
/// 이벤트 에너미 웨이브 클래스
/// </summary>
public class EventEnemyWave : WaveBase
{
    private D_EventEnemyWaveData eventWaveData;
    private int totalGroupCount = 0;
    private int completedGroupCount = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    public EventEnemyWave(D_EventEnemyWaveData data) : base(data)
    {
        eventWaveData = data;
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "이벤트 에너미 웨이브\n";

        var groupedEnemies = eventWaveData.f_eventEnemyGroups
                            .GroupBy(g => g.f_enemy.f_name)
                            .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.TotalAmount}\n";
        }

        return waveInfo;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        // 총 적 수 계산
        remainEnemies = eventWaveData.f_eventEnemyGroups.Sum(group => group.f_amount);
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // 이벤트 적 스폰
        SpawnEventEnemies();
    }

    private void SpawnEventEnemies()
    {
        totalGroupCount = eventWaveData.f_eventEnemyGroups.Count;
        completedGroupCount = 0;

        foreach (D_eventEnemyGroups groupData in eventWaveData.f_eventEnemyGroups)
        {
            StageManager.Instance.StartCoroutine(CoSpawnEventGroup(groupData));
        }
    }

    private IEnumerator CoSpawnEventGroup(D_eventEnemyGroups groupData)
    {
        if (groupData == null || groupData.f_enemy == null)
        {
            Debug.LogError("이벤트 에너미 그룹 데이터 없음");
            yield break;
        }

        yield return new WaitForSeconds(groupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < groupData.f_amount; spawnedCount++)
        {
            if (groupData.f_enemy.f_ObjectPoolKey != null)
            {
                // 이벤트 적 스폰 (드롭 아이템 정보 포함)
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy);
                // TODO: 드롭 아이템 정보 설정
            }
            else
            {
                Debug.LogError("이벤트 적 오브젝트 풀 키가 없음");
                break;
            }

            yield return new WaitForSeconds(groupData.f_spawnInterval);
        }

        ++completedGroupCount;

        if (completedGroupCount >= totalGroupCount)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }

    public override void AddEnemies(int count)
    {
        remainEnemies += count;
        StageManager.Instance.UpdateEnemyCount(count);
    }

    public override void DecreaseEnemyCount()
    {
        --remainEnemies;
        CheckWaveCompletion();
    }

    public override void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0 && !isWaveCompleted)
        {
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
    }

    public override void EndWave()
    {
        // 필요한 리소스 정리
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}