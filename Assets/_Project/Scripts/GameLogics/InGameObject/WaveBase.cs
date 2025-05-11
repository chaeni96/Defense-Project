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

    //보상, 추가 연출 등 효과를 위한 시간 제공 코루틴임
    public virtual IEnumerator EndWaveRoutine()
    {
        yield break;
    }
}

public enum BattleResult
{
    None,
    Victory,
    Defeat
}
public abstract class BattleWaveBase : WaveBase
{
    protected bool isSpawnDone = false;
    protected int totalGroupCount = 0;
    protected int completedGroupCount = 0;
    protected int remainEnemies = 0;
    protected BattleResult battleResult = BattleResult.None;
    protected bool isBattleFinished = false;

    //떨어진 아이템 회수, 저금통 이자 지급, 전투 종료 연출 출력 시간
    protected float interestDelay = 0.6f; //일단 0.6초

    public BattleWaveBase(D_WaveDummyData data) : base(data)
    {
        // 초기 상태 설정
        isWaveCompleted = false;
        isSpawnDone = false;
        isBattleFinished = false;
        battleResult = BattleResult.None;
    }

    public override void StartWave()
    {

        UnitManager.Instance.OnUnitDeath += CheckBattleResult;
        EnemyManager.Instance.OnEnemyDeath += CheckBattleResult;

        isWaveCompleted = false;
        isSpawnDone = false;
        isBattleFinished = false;
        battleResult = BattleResult.None;

        // 자식 클래스에서 총 적 수 계산
        totalGroupCount = CalculateTotalEnemies();
        StageManager.Instance.SetTotalEnemyCount(totalGroupCount);
        remainEnemies = totalGroupCount;

        // TODO:김기린
        // 여기서 "StartWave 버튼"을 활성화하는 UI를 활성화 하도록 해야함.OnStartWaveButtonClicked연결할것.
        // 혹은 이벤트 연결 필요
        SpawnWaveEnemies();

        StageManager.Instance.UpdateEnemyCount(totalGroupCount);
        Debug.Log("전투 웨이브 시작 준비 완료! 사용자 입력을 대기합니다.");
    }

    //웨이브 시작 버튼 클릭시 호출될 메서드
    protected abstract void OnStartWaveButtonClicked();

    protected abstract void SpawnWaveEnemies();

    protected abstract int CalculateTotalEnemies();


    // 승패 판정 메서드 추가
    public virtual void CheckBattleResult()
    {
        if(!isWaveCompleted)
        {
            // 에너미가 모두 죽었는지 체크 - 실제 존재하는 활성화된 적 객체로 확인
            bool allEnemiesDead = EnemyManager.Instance.GetActiveEnemyCount() <= 0;

            // 공격 가능한 유닛이 죽었는지 체크
            bool allAttackableUnitsDead = true;

            List<UnitController> units = UnitManager.Instance.GetAllUnits();
            foreach (var unit in units)
            {
                if (unit != null && unit.isActive && unit.canAttack)
                {
                    // 공격 가능한 유닛이 하나라도 살아있으면
                    allAttackableUnitsDead = false;
                    break;
                }
            }

            // 이제 allUnitsDead가 아닌 allAttackableUnitsDead로 판단
            if (allEnemiesDead && !allAttackableUnitsDead)
            {
                // 공격 가능한 유닛이 살아있고 에너미가 모두 죽으면 승리
                if (battleResult == BattleResult.None)
                {
                    battleResult = BattleResult.Victory;
                    OnBattleVictory();
                }
            }
            else if (allAttackableUnitsDead && !allEnemiesDead)
            {
                // 공격 가능한 유닛이 모두 죽고 에너미가 살아있으면 패배
                if (battleResult == BattleResult.None)
                {
                    battleResult = BattleResult.Defeat;
                    OnBattleDefeat();
                }
            }
        }

      

    }

    // 승리 시 호출되는 메서드
    protected virtual void OnBattleVictory()
    {
        if (!isBattleFinished && !isWaveCompleted)
        {
            isBattleFinished = true;
            isWaveCompleted = true;

            // BattleWinState로 전환
            EnterBattleWinState();
        }

    }

    // 패배 시 호출되는 메서드
    protected virtual void OnBattleDefeat()
    {
        if (!isBattleFinished)
        {
            isBattleFinished = true;

            isWaveCompleted = true;
            // 바로 웨이브 종료 및 게임 오버 처리
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));
        }

    }

    // BattleWinState로 전환하는 메서드
    protected virtual void EnterBattleWinState()
    {
        // 모든 유닛 BattleWinState로 변경
        List<UnitController> units = UnitManager.Instance.GetAllUnits();
        foreach (var unit in units)
        {
            if (unit != null && unit.gameObject.activeSelf)
            {
                // FSM 상태 전환
                unit.fsmObj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.BattleWin);
            }
        }

        CheckWaveCompletion();
    }

    public override void EndWave()
    {
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
        battleResult = BattleResult.None;
        isBattleFinished = false;
        UnitManager.Instance.OnUnitDeath -= CheckBattleResult;
        EnemyManager.Instance.OnEnemyDeath -= CheckBattleResult;


    }
}

/// <summary>
/// 일반 전투 웨이브 클래스
/// </summary>
public class NormalBattleWave : BattleWaveBase
{
    private D_NormalBattleWaveData normalWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // 그리드 한 칸의 크기

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //배치할곳 -> 데이터로 빼두기 

    public NormalBattleWave(D_NormalBattleWaveData data) : base(data)
    {
        normalWaveData = data;

        // 해당 맵 ID에 대한 배치 데이터 로드
        placementData = normalWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"맵 {placementData.f_name} 에너미 배치 데이터가 없습니다!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

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
    protected override void SpawnWaveEnemies()
    {
        foreach (var cellData in placementData.f_cellData)
        {

            // 그리드 좌표를 게임 월드 좌표로 변환
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // 그리드 좌표를 월드 좌표로 변환하는 함수
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // 중앙을 기준으로 좌표 계산
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //버튼 클릭했을때 에너미 상태변경 
    }

}



/// <summary>
/// 보스 전투 웨이브 클래스
/// </summary>
/// 

public class BossBattleWave : BattleWaveBase
{
    private D_BossBattleWaveData bossWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // 그리드 한 칸의 크기

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //배치할곳 -> 데이터로 빼두기 

    public BossBattleWave(D_BossBattleWaveData data) : base(data)
    {
        bossWaveData = data;

        // 해당 맵 ID에 대한 배치 데이터 로드
        placementData = bossWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"맵 {placementData.f_mapID} 에너미 배치 데이터가 없습니다!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

    }
    /// <summary>
    /// 웨이브 설명 텍스트
    /// </summary>
    public override string GetWaveInfoText()
    {

        string waveInfo = "보스 전투 웨이브\n";

        var groupedEnemies = placementData.f_cellData
                                .GroupBy(cell => cell.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, Count = g.Count() });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.Count}\n";
        }

        return waveInfo;
    }
    protected override void SpawnWaveEnemies()
    {
        foreach (var cellData in placementData.f_cellData)
        {

            // 그리드 좌표를 게임 월드 좌표로 변환
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // 그리드 좌표를 월드 좌표로 변환하는 함수
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // 중앙을 기준으로 좌표 계산
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //버튼 클릭했을때 에너미 상태변경 
    }
}

/// <summary>
/// 이벤트 에너미 웨이브 클래스
/// </summary>
/// 

public class EventEnemyWave : BattleWaveBase
{

    private bool isEquip; 
    private D_EventEnemyWaveData eventEnemyWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // 그리드 한 칸의 크기

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //배치할곳 -> 데이터로 빼두기 

    public EventEnemyWave(D_EventEnemyWaveData data) : base(data)
    {
        eventEnemyWaveData = data;

        // 해당 맵 ID에 대한 배치 데이터 로드
        placementData = eventEnemyWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"맵 {placementData.f_mapID} 에너미 배치 데이터가 없습니다!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

    }
    /// <summary>
    /// 웨이브 설명 텍스트
    /// </summary>
    public override string GetWaveInfoText()
    {
        string waveInfo = "보물 에너미 등장 웨이브\n";

        var groupedEnemies = placementData.f_cellData
                                .GroupBy(cell => cell.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, Count = g.Count() });

        foreach (var group in groupedEnemies)
        {
            waveInfo += $"{group.Name} x{group.Count}\n";
        }

        return waveInfo;
    }
    protected override void SpawnWaveEnemies()
    {
        foreach (var cellData in placementData.f_cellData)
        {

            // 그리드 좌표를 게임 월드 좌표로 변환
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // 그리드 좌표를 월드 좌표로 변환하는 함수
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // 중앙을 기준으로 좌표 계산
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //버튼 클릭했을때 에너미 상태변경 
    }

  
}

public class WildcardWave : WaveBase//, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{
    private D_WildCardWaveData wildCardWaveData;

    private WildCardSelectUI selectUI;

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


public class PrizeHuntingWave : BattleWaveBase
{
    private D_PrizeHuntingWaveData prizeHuntingData;
    private D_HuntingOptionData selectedOption;

    public PrizeHuntingWave(D_PrizeHuntingWaveData data, D_HuntingOptionData option) : base(data)
    {
        prizeHuntingData = data;
        selectedOption = option;
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


    public override string GetWaveInfoText()
    {
        string waveInfo = "현상금 사냥꾼 전투 웨이브\n";

        if (selectedOption != null)
        {
            waveInfo += $"선택한 사냥꾼: {selectedOption.f_title}\n";
            waveInfo += $"설명: {selectedOption.f_description}\n";
        }

        return waveInfo;
    }

    protected override void SpawnWaveEnemies()
    {
        // 선택된 옵션의 현상금 사냥꾼 스폰
        if (selectedOption.f_spawnEnemy != null && selectedOption.f_spawnEnemy.f_ObjectPoolKey != null)
        {
            //TODO : 지금은 데이터에서 spawnPos로 가져오고 있는데 서포트 에너미도 등장시킬거면 에너미 배치 데이터 사용
            EnemyManager.Instance.SpawnEnemy(selectedOption.f_spawnEnemy, selectedOption.f_spawnPos);

            // 보상/위험 요소 적용 (필요한 경우)
            ApplyRewardsAndRisks();
        }
        else
        {
            Debug.LogError("현상금 사냥꾼 오브젝트 풀 키가 없음");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return 1;
    }

    protected override void OnStartWaveButtonClicked()
    {
    }

    public override void EndWave()
    {
        base.EndWave();
        // 필요한 리소스 정리
        remainEnemies = 0;
        selectedOption = null;
    }

}
