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
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy, null, groupData.f_EventDummyData);

                //가지고 있는 이벤트 등록
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


public class WildcardWave : WaveBase//, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{
    private D_WildCardWaveData wildCardWaveData;
    //private float selectionTime;
    //private float minSelectionTime;
    //private int timeScheduleUID = -1;
    private WildCardSelectUI selectUI;
    //private InGameCountdownUI countdownUI;

    //TODO : 카드 선택 장수도 스탯으로 빼기
    public WildcardWave(D_WildCardWaveData data) : base(data)
    {
        wildCardWaveData = data;
        //selectionTime = wildCardWaveData.f_selectionTime;
        //minSelectionTime = wildCardWaveData.f_minSelectionTime;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        WildCardManager.Instance.OnWildCardSelected += OnCardSelected;
        ShowWildCardSelection();
    }

    public override string GetWaveInfoText()
    {
        return "와일드카드 선택 웨이브\n"
           + "카드를 선택하여 능력을 강화하세요!";
    }

    private async void ShowWildCardSelection()
    {
        // 타이머 등록
        //timeScheduleUID = TimeTableManager.Instance.RegisterSchedule(selectionTime);
        //TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, timeScheduleUID);
        //TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, timeScheduleUID);

        // 와일드카드 선택 UI 표시
        selectUI = await UIManager.Instance.ShowUI<WildCardSelectUI>();

        // 와일드카드 덱 설정, TODO :  인자값으로 나중에 와일드카드 개수 넘거야됨
        selectUI.SetWildCardDeck();

    }

    // WildCardSelectUI에서 카드 선택 시 호출할 메서드
    public void OnCardSelected()
    {
        // 웨이브 완료 처리
        isWaveCompleted = true;

        StageManager.Instance.OnWaveComplete();
    }

   /* private float GetTimeScheduleRemainingTime()
    {
        var schedule = TimeTableManager.Instance.GetSchedule(timeScheduleUID);
        if (schedule != null)
        {
            return (float)((schedule.endTime - schedule.currentTime) / 100.0);
        }
        return 0f;
    }

    public void OnChangeTime(int scheduleUID, float remainTime)
    {
        if (scheduleUID == timeScheduleUID)
        {
            // 와일드카드 선택 시간 업데이트
            if (selectUI != null)
            {
                selectUI.UpdateSelectTime(Mathf.CeilToInt(remainTime));
            }

            // 카운트다운 UI가 있는 경우 업데이트
            if (countdownUI != null)
            {
                countdownUI.UpdateCountdown(remainTime);
            }
        }
    }

    public void OnCompleteSchedule(int scheduleUID)
    {
        if (scheduleUID == timeScheduleUID)
        {
            // 와일드카드 선택 시간 완료
            if (selectUI != null)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();
            }

            if (countdownUI != null)
            {
                UIManager.Instance.CloseUI<InGameCountdownUI>();
            }

            timeScheduleUID = -1;

            // 웨이브 완료 처리
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
    }*/

    public override void EndWave()
    {
        // 리소스 정리
        if (selectUI != null)
        {
            UIManager.Instance.CloseUI<WildCardSelectUI>();
            selectUI = null;
        }

/*        if (countdownUI != null)
        {
            UIManager.Instance.CloseUI<InGameCountdownUI>();
            countdownUI = null;
        }
*/
/*        if (timeScheduleUID != -1)
        {
            TimeTableManager.Instance.RemoveScheduleCompleteTargetSubscriber(timeScheduleUID);
            TimeTableManager.Instance.RemoveTimeChangeTargetSubscriber(timeScheduleUID);
            timeScheduleUID = -1;
        }*/

        WildCardManager.Instance.OnWildCardSelected -= OnCardSelected;

    }
}

public class HuntingSelectTimeWave : WaveBase
{
    private D_HuntingSelectTimeWaveData huntingSelectData;
    private PrizeHuntingSelectUI selectUI;
    private D_HuntingOptionData selectedOption;

    public HuntingSelectTimeWave(D_HuntingSelectTimeWaveData data) : base(data)
    {
        huntingSelectData = data;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        ShowHuntingOptionSelection();
    }

    public override string GetWaveInfoText()
    {
        return "현상금 사냥꾼 선택 웨이브\n" +
               "원하는 현상금 사냥꾼 옵션을 선택하세요!";
    }

    private async void ShowHuntingOptionSelection()
    {
        // 현상금 사냥꾼 선택 UI 표시
        selectUI = await UIManager.Instance.ShowUI<PrizeHuntingSelectUI>();

        // 이벤트 구독
        selectUI.OnOptionSelected += OnOptionSelected;

        // 선택 가능한 옵션 설정
        selectUI.SetHuntingOptions(huntingSelectData.f_huntingOptions.ToList());
    }

    private void OnOptionSelected(D_HuntingOptionData option)
    {
        selectedOption = option;

        // 옵션에 연결된 PrizeHuntingWaveData 가져오기
        D_PrizeHuntingWaveData prizeWaveData = option.f_prizeHuntingData;

        if (prizeWaveData != null)
        {
            // 다음 웨이브로 PrizeHuntingWave 설정
            StageManager.Instance.SetNextWave(new PrizeHuntingWave(prizeWaveData, selectedOption));
        }
        else
        {
            Debug.LogError("선택된 현상금 사냥꾼 옵션에 연결된 PrizeHuntingWaveData가 없습니다!");
        }

        // 웨이브 완료 처리
        isWaveCompleted = true;
        StageManager.Instance.OnWaveComplete();
    }

    public override void EndWave()
    {
        // 리소스 정리
        if (selectUI != null)
        {
            selectUI.OnOptionSelected -= OnOptionSelected;
            selectUI = null;
        }

    }
}


public class PrizeHuntingWave : WaveBase
{
    private D_PrizeHuntingWaveData prizeHuntingData;
    private D_HuntingOptionData selectedOption;

    private int totalGroupCount = 0;
    private int completedGroupCount = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    public PrizeHuntingWave(D_PrizeHuntingWaveData data, D_HuntingOptionData option ) : base(data)
    {
        prizeHuntingData = data;
        selectedOption = option;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        if (selectedOption == null)
        {
            Debug.LogError("현상금 사냥꾼 옵션 데이터가 없습니다!");
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
            return;
        }

        // 현상금 사냥꾼 + 서포트 적 총 수 계산
        remainEnemies = 1; // 메인 현상금 사냥꾼
        if (prizeHuntingData.f_supportEnemys != null && prizeHuntingData.f_supportEnemys.Count > 0)
        {
            remainEnemies += prizeHuntingData.f_supportEnemys.Sum(group => group.f_amount);
        }

        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // 현상금 사냥꾼과 서포트 적 스폰
        SpawnHuntingAndSupportEnemies();
    }

    private void SpawnHuntingAndSupportEnemies()
    {
        // 총 스폰할 그룹 수 계산 (현상금 사냥꾼 + 서포트 그룹)
        totalGroupCount = 1; // 현상금 사냥꾼
        if (prizeHuntingData.f_supportEnemys != null)
        {
            totalGroupCount += prizeHuntingData.f_supportEnemys.Count;
        }

        completedGroupCount = 0;

        // 메인 현상금 사냥꾼 스폰
        StageManager.Instance.StartCoroutine(CoSpawnHuntingEnemy());

        // 서포트 적 스폰
        if (prizeHuntingData.f_supportEnemys != null && prizeHuntingData.f_supportEnemys.Count > 0)
        {
            foreach (D_supportEnemys group in prizeHuntingData.f_supportEnemys)
            {
                StageManager.Instance.StartCoroutine(CoSpawnSupportGroup(group));
            }
        }
    }

    private IEnumerator CoSpawnHuntingEnemy()
    {
        yield return new WaitForSeconds(0.5f); // 약간의 지연

        // 선택된 옵션의 현상금 사냥꾼 스폰
        if (selectedOption.f_spawnEnemy != null && selectedOption.f_spawnEnemy.f_ObjectPoolKey != null)
        {
            // 선택된 옵션에 따른 현상금 사냥꾼 스폰
            EnemyManager.Instance.SpawnEnemy(selectedOption.f_spawnEnemy);

            // 보상/위험 요소 적용 (필요한 경우)
            ApplyRewardsAndRisks();
        }
        else
        {
            Debug.LogError("현상금 사냥꾼 오브젝트 풀 키가 없음");
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private IEnumerator CoSpawnSupportGroup(D_supportEnemys groupData)
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

    private void ApplyRewardsAndRisks()
    {
        // 선택한 옵션의 보상과 위험 요소 적용
        // 실제 구현은 게임 시스템에 따라 다름

        // 보상 적용
        if (selectedOption.f_huntingReward != null && selectedOption.f_huntingReward.Count > 0)
        {
            foreach (var reward in selectedOption.f_huntingReward)
            {
                // 예: StatManager.ApplyStat(reward.f_statName, reward.f_value, reward.f_valueMultiply * prizeHuntingData.f_rewardMuliply);
            }
        }

        // 위험 요소 적용
        if (selectedOption.f_huntingRisk != null && selectedOption.f_huntingRisk.Count > 0)
        {
            foreach (var risk in selectedOption.f_huntingRisk)
            {
                // 예: StatManager.ApplyStat(risk.f_statName, risk.f_value, risk.f_valueMultiply);
            }
        }
    }

    private void CheckSpawnCompletion()
    {
        if (completedGroupCount >= totalGroupCount)
        {
            isSpawnDone = true;
            CheckWaveCompletion();
        }
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "현상금 사냥꾼 전투 웨이브\n";

        if (selectedOption != null)
        {
            waveInfo += $"선택한 사냥꾼: {selectedOption.f_title}\n";
            waveInfo += $"설명: {selectedOption.f_description}\n";
        }

        // 서포트 적이 있다면 표시
        if (prizeHuntingData.f_supportEnemys != null && prizeHuntingData.f_supportEnemys.Count > 0)
        {
            var groupedEnemies = prizeHuntingData.f_supportEnemys
                                .GroupBy(g => g.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "서포트 적:";
            foreach (var group in groupedEnemies)
            {
                waveInfo += $"{group.Name} x{group.TotalAmount}\n";
            }
        }

        return waveInfo;
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
        selectedOption = null;
    }
}