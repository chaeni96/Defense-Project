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

    //보상, 추가 연출 등 효과를 위한 시간 제공 코루틴임
    public virtual IEnumerator EndWaveRoutine()
    {
        yield break;
    }
}

public abstract class BattleWaveBase : WaveBase
{
    protected bool isSpawnDone = false;
    protected int totalGroupCount = 0;
    protected int completedGroupCount = 0;
    protected int remainEnemies = 0;


    //떨어진 아이템 회수, 저금통 이자 지급, 전투 종료 연출 출력 시간
    protected float interestDelay = 0.6f; //일단 0.6초
    public BattleWaveBase(D_WaveDummyData data) : base(data)
    {
    }
    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        // 자식 클래스에서 총 적 수 계산
        remainEnemies = CalculateTotalEnemies();
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // TODO:김기린
        // 여기서 "StartWave 버튼"을 활성화하는 UI를 활성화 하도록 해야함.OnStartWaveButtonClicked연결할것.
        // 혹은 이벤트 연결 필요

        SpawnWaveEnemies();

        Debug.Log("전투 웨이브 시작 준비 완료! 사용자 입력을 대기합니다.");
    }

    //웨이브 시작 버튼 클릭시 호출될 메서드
    protected abstract void OnStartWaveButtonClicked();

    protected abstract void SpawnWaveEnemies();

    protected abstract int CalculateTotalEnemies();
    public override void AddEnemies(int count)
    {
        remainEnemies += count;
        StageManager.Instance.UpdateEnemyCount(count);
    }

    public override void DecreaseEnemyCount()
    {
        remainEnemies--;
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
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}

/// <summary>
/// 일반 전투 웨이브 클래스
/// </summary>
public class NormalBattleWave : BattleWaveBase
{
    private D_NormalBattleWaveData normalWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.6f; // 그리드 한 칸의 크기


    private int totalEnemies = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    private int mapId;
    private Vector2 centerOffset = new Vector2(0f, 2.5f); // 중앙 기준점 오프셋

    public NormalBattleWave(D_NormalBattleWaveData data) : base(data)
    {
        normalWaveData = data;

        // 해당 맵 ID에 대한 배치 데이터 로드
        placementData = D_EnemyPlacementData.FindEntity(p => p.f_mapID == normalWaveData.f_mapID);

        if (placementData == null)
        {
            Debug.LogError($"맵 ID {mapId}에 대한 에너미 배치 데이터가 없습니다!");
        }

    }

    /// <summary>
    /// 웨이브 설명 텍스트
    /// </summary>
    public override string GetWaveInfoText()
    {
        string waveInfo = "일반 전투 웨이브\n";

        var groupedEnemies = placementData.f_cellData
                                .GroupBy(cell => cell.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, Count = g.Count() });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.Count}\n";
        }

        return waveInfo;
    }

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        if (placementData == null)
        {
            Debug.LogError($"맵 ID {mapId}에 대한 에너미 배치 데이터가 없습니다!");
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
            return;
        }

        // 총 에너미 수 계산
        totalEnemies = placementData.f_cellData.Count;
        remainEnemies = totalEnemies;
        StageManager.Instance.SetTotalEnemyCount(totalEnemies);

        // 에너미 생성
        SpawnWaveEnemies();
    }

    protected override void SpawnWaveEnemies()
    {
        foreach (var cellData in placementData.f_cellData)
        {
            if (cellData.f_enemy != null && cellData.f_enemy.f_ObjectPoolKey != null)
            {
                // 그리드 좌표를 게임 월드 좌표로 변환
                Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

                // 에너미 직접 생성
                GameObject enemyObj = PoolingManager.Instance.GetObject(
                    cellData.f_enemy.f_ObjectPoolKey.f_PoolObjectAddressableKey,
                    worldPos,
                    (int)ObjectLayer.Enemy
                );

                if (enemyObj != null)
                {
                    Enemy enemy = enemyObj.GetComponent<Enemy>();
                    enemy.transform.position = worldPos;
                    enemy.Initialize();
                    enemy.InitializeEnemyInfo(cellData.f_enemy);
                }
            }
        }

        isSpawnDone = true;
        CheckWaveCompletion();
    }

    // 그리드 좌표를 월드 좌표로 변환하는 함수
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 4f;

        // 중앙을 기준으로 좌표 계산
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    { 
    }

 

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;
    }


}

   

/// <summary>
/// 보스 전투 웨이브 클래스
/// </summary>
public class BossBattleWave : BattleWaveBase
{
    private D_BossBattleWaveData bossWaveData;

    public BossBattleWave(D_BossBattleWaveData data) : base(data)
    {
        bossWaveData = data;
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "보스 전투 웨이브\n";
        waveInfo += $"보스: {bossWaveData.f_bossEnemy.f_name}\n";

        if (bossWaveData.f_supportEnemyGroups != null && bossWaveData.f_supportEnemyGroups.Count > 0)
        {
            var groupedEnemies = bossWaveData.f_supportEnemyGroups
                                    .GroupBy(g => g.f_enemy.f_name)
                                    .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "서포터:\n";
            foreach (var group in groupedEnemies)
            {
                waveInfo += $"{group.Name} x{group.TotalAmount}\n";
            }
        }

        return waveInfo;
    }

    protected override int CalculateTotalEnemies()
    {
        int total = 1; // 보스 1마리
        if (bossWaveData.f_supportEnemyGroups != null)
        {
            total += bossWaveData.f_supportEnemyGroups.Sum(group => group.f_amount);
        }
        return total;
    }
    protected override void OnStartWaveButtonClicked()
    {
    }

    protected override void SpawnWaveEnemies()
    {
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

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private IEnumerator CoSpawnSupportGroup(D_supportEnemyGroups groupData)
    {
        if (groupData == null || groupData.f_enemy == null)
        {
            yield break;
        }

        yield return new WaitForSeconds(groupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < groupData.f_amount; spawnedCount++)
        {
            if (groupData.f_enemy.f_ObjectPoolKey != null)
            {
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy);
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
}

/// <summary>
/// 이벤트 에너미 웨이브 클래스
/// </summary>
public class EventEnemyWave : BattleWaveBase
{
    private D_EventEnemyWaveData eventWaveData;

    private bool isGetItem;

    public EventEnemyWave(D_EventEnemyWaveData data) : base(data)
    {
        eventWaveData = data;
    }

    public override string GetWaveInfoText()
    {
        string waveInfo = "보물 고블린 웨이브\n";
        var groupedEnemies = eventWaveData.f_eventEnemyGroups
                            .GroupBy(g => g.f_enemy.f_name)
                            .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.TotalAmount}\n";
        }

        return waveInfo;
    }

    protected override int CalculateTotalEnemies()
    {
        return eventWaveData.f_eventEnemyGroups.Sum(group => group.f_amount);
    }

    protected override void OnStartWaveButtonClicked()
    {
    }
    protected override void SpawnWaveEnemies()
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
            yield break;
        }

        yield return new WaitForSeconds(groupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < groupData.f_amount; spawnedCount++)
        {
            if (groupData.f_enemy.f_ObjectPoolKey != null)
            {
                // 이벤트 적 스폰 (드롭 아이템 정보 포함 가능)
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy, null, null,groupData.f_EventDummyData);
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

    public override void CheckWaveCompletion()
    {
        if (isSpawnDone && remainEnemies <= 0 && !isWaveCompleted && isGetItem)
        {
            //아이템을 습득해야 넘기기 
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
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


    public override void EndWave()
    {
        // 리소스 정리
        if (selectUI != null)
        {
            UIManager.Instance.CloseUI<WildCardSelectUI>();
            selectUI = null;
        }

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
