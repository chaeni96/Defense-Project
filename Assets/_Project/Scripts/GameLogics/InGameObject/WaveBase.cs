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

    // ��� ���̺꿡�� ���������� �����ؾ� �� �޼���
    public abstract void StartWave();
    public abstract void EndWave();

    //���̺� �����ȯ
    public abstract string GetWaveInfoText();

    //���̺� �Ϸ� üũ
    public virtual void CheckWaveCompletion()
    {
        if (isWaveCompleted)
        {
            StageManager.Instance.OnWaveComplete();
        }
    }

    // ���������� �������̵��� �� �ִ� ���� �޼����, �ð� ���� ���̺�, üũ ������ �ִ� ���̺��
    public virtual void HandleTimeChange(float remainTime) { }
    public virtual void HandleScheduleComplete(int scheduleUID) { }

    //����, �߰� ���� �� ȿ���� ���� �ð� ���� �ڷ�ƾ��
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

    //������ ������ ȸ��, ������ ���� ����, ���� ���� ���� ��� �ð�
    protected float interestDelay = 0.6f; //�ϴ� 0.6��

    public BattleWaveBase(D_WaveDummyData data) : base(data)
    {
        // �ʱ� ���� ����
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

        // �ڽ� Ŭ�������� �� �� �� ���
        totalGroupCount = CalculateTotalEnemies();
        StageManager.Instance.SetTotalEnemyCount(totalGroupCount);
        remainEnemies = totalGroupCount;

        // TODO:��⸰
        // ���⼭ "StartWave ��ư"�� Ȱ��ȭ�ϴ� UI�� Ȱ��ȭ �ϵ��� �ؾ���.OnStartWaveButtonClicked�����Ұ�.
        // Ȥ�� �̺�Ʈ ���� �ʿ�
        SpawnWaveEnemies();

        StageManager.Instance.UpdateEnemyCount(totalGroupCount);
        Debug.Log("���� ���̺� ���� �غ� �Ϸ�! ����� �Է��� ����մϴ�.");
    }

    //���̺� ���� ��ư Ŭ���� ȣ��� �޼���
    protected abstract void OnStartWaveButtonClicked();

    protected abstract void SpawnWaveEnemies();

    protected abstract int CalculateTotalEnemies();


    // ���� ���� �޼��� �߰�
    public virtual void CheckBattleResult()
    {
        if(!isWaveCompleted)
        {
            // ���ʹ̰� ��� �׾����� üũ - ���� �����ϴ� Ȱ��ȭ�� �� ��ü�� Ȯ��
            bool allEnemiesDead = EnemyManager.Instance.GetActiveEnemyCount() <= 0;

            // ���� ������ ������ �׾����� üũ
            bool allAttackableUnitsDead = true;

            List<UnitController> units = UnitManager.Instance.GetAllUnits();
            foreach (var unit in units)
            {
                if (unit != null && unit.isActive && unit.canAttack)
                {
                    // ���� ������ ������ �ϳ��� ���������
                    allAttackableUnitsDead = false;
                    break;
                }
            }

            // ���� allUnitsDead�� �ƴ� allAttackableUnitsDead�� �Ǵ�
            if (allEnemiesDead && !allAttackableUnitsDead)
            {
                // ���� ������ ������ ����ְ� ���ʹ̰� ��� ������ �¸�
                if (battleResult == BattleResult.None)
                {
                    battleResult = BattleResult.Victory;
                    OnBattleVictory();
                }
            }
            else if (allAttackableUnitsDead && !allEnemiesDead)
            {
                // ���� ������ ������ ��� �װ� ���ʹ̰� ��������� �й�
                if (battleResult == BattleResult.None)
                {
                    battleResult = BattleResult.Defeat;
                    OnBattleDefeat();
                }
            }
        }

      

    }

    // �¸� �� ȣ��Ǵ� �޼���
    protected virtual void OnBattleVictory()
    {
        if (!isBattleFinished && !isWaveCompleted)
        {
            isBattleFinished = true;
            isWaveCompleted = true;

            // BattleWinState�� ��ȯ
            EnterBattleWinState();
        }

    }

    // �й� �� ȣ��Ǵ� �޼���
    protected virtual void OnBattleDefeat()
    {
        if (!isBattleFinished)
        {
            isBattleFinished = true;

            isWaveCompleted = true;
            // �ٷ� ���̺� ���� �� ���� ���� ó��
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));
        }

    }

    // BattleWinState�� ��ȯ�ϴ� �޼���
    protected virtual void EnterBattleWinState()
    {
        // ��� ���� BattleWinState�� ����
        List<UnitController> units = UnitManager.Instance.GetAllUnits();
        foreach (var unit in units)
        {
            if (unit != null && unit.gameObject.activeSelf)
            {
                // FSM ���� ��ȯ
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
/// �Ϲ� ���� ���̺� Ŭ����
/// </summary>
public class NormalBattleWave : BattleWaveBase
{
    private D_NormalBattleWaveData normalWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // �׸��� �� ĭ�� ũ��

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //��ġ�Ұ� -> �����ͷ� ���α� 

    public NormalBattleWave(D_NormalBattleWaveData data) : base(data)
    {
        normalWaveData = data;

        // �ش� �� ID�� ���� ��ġ ������ �ε�
        placementData = normalWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"�� {placementData.f_name} ���ʹ� ��ġ �����Ͱ� �����ϴ�!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

    }
    /// <summary>
    /// ���̺� ���� �ؽ�Ʈ
    /// </summary>
    public override string GetWaveInfoText()
    {
        string waveInfo = "�Ϲ� ���� ���̺�\n";

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

            // �׸��� ��ǥ�� ���� ���� ��ǥ�� ��ȯ
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�ϴ� �Լ�
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // �߾��� �������� ��ǥ ���
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //��ư Ŭ�������� ���ʹ� ���º��� 
    }

}



/// <summary>
/// ���� ���� ���̺� Ŭ����
/// </summary>
/// 

public class BossBattleWave : BattleWaveBase
{
    private D_BossBattleWaveData bossWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // �׸��� �� ĭ�� ũ��

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //��ġ�Ұ� -> �����ͷ� ���α� 

    public BossBattleWave(D_BossBattleWaveData data) : base(data)
    {
        bossWaveData = data;

        // �ش� �� ID�� ���� ��ġ ������ �ε�
        placementData = bossWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"�� {placementData.f_mapID} ���ʹ� ��ġ �����Ͱ� �����ϴ�!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

    }
    /// <summary>
    /// ���̺� ���� �ؽ�Ʈ
    /// </summary>
    public override string GetWaveInfoText()
    {

        string waveInfo = "���� ���� ���̺�\n";

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

            // �׸��� ��ǥ�� ���� ���� ��ǥ�� ��ȯ
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�ϴ� �Լ�
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // �߾��� �������� ��ǥ ���
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //��ư Ŭ�������� ���ʹ� ���º��� 
    }
}

/// <summary>
/// �̺�Ʈ ���ʹ� ���̺� Ŭ����
/// </summary>
/// 

public class EventEnemyWave : BattleWaveBase
{

    private bool isEquip; 
    private D_EventEnemyWaveData eventEnemyWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.55f; // �׸��� �� ĭ�� ũ��

    private Vector2 centerOffset = new Vector2(10.18f, 1.83f); //��ġ�Ұ� -> �����ͷ� ���α� 

    public EventEnemyWave(D_EventEnemyWaveData data) : base(data)
    {
        eventEnemyWaveData = data;

        // �ش� �� ID�� ���� ��ġ ������ �ε�
        placementData = eventEnemyWaveData.f_placeEnemyMapData;

        if (placementData == null)
        {
            Debug.LogError($"�� {placementData.f_mapID} ���ʹ� ��ġ �����Ͱ� �����ϴ�!");
        }

    }

    protected override int CalculateTotalEnemies()
    {
        return placementData != null ? placementData.f_cellData.Count : 0;

    }
    /// <summary>
    /// ���̺� ���� �ؽ�Ʈ
    /// </summary>
    public override string GetWaveInfoText()
    {
        string waveInfo = "���� ���ʹ� ���� ���̺�\n";

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

            // �׸��� ��ǥ�� ���� ���� ��ǥ�� ��ȯ
            Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

            EnemyManager.Instance.SpawnEnemy(cellData.f_enemy, worldPos);
            isSpawnDone = true;
        }
    }

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�ϴ� �Լ�
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 5f;

        // �߾��� �������� ��ǥ ���
        float xPos = (gridPos.x - gridCenterX) * gridSize + centerOffset.x;
        float yPos = (gridCenterY - gridPos.y) * gridSize + centerOffset.y;

        return new Vector2(xPos, yPos);
    }

    protected override void OnStartWaveButtonClicked()
    {
        //��ư Ŭ�������� ���ʹ� ���º��� 
    }

  
}

public class WildcardWave : WaveBase//, ITimeChangeSubscriber, IScheduleCompleteSubscriber
{
    private D_WildCardWaveData wildCardWaveData;

    private WildCardSelectUI selectUI;

    //TODO : ī�� ���� ����� �������� ����
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
        return "���ϵ�ī�� ���� ���̺�\n"
           + "ī�带 �����Ͽ� �ɷ��� ��ȭ�ϼ���!";
    }

    private async void ShowWildCardSelection()
    {
        // Ÿ�̸� ���

        // ���ϵ�ī�� ���� UI ǥ��
        selectUI = await UIManager.Instance.ShowUI<WildCardSelectUI>();

        // ���ϵ�ī�� �� ����, TODO :  ���ڰ����� ���߿� ���ϵ�ī�� ���� �Ѱžߵ�
        selectUI.SetWildCardDeck();

    }

    // WildCardSelectUI���� ī�� ���� �� ȣ���� �޼���
    public void OnCardSelected()
    {
        // ���̺� �Ϸ� ó��
        isWaveCompleted = true;

        StageManager.Instance.OnWaveComplete();
    }


    public override void EndWave()
    {
        // ���ҽ� ����
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
        return "����� ��ɲ� ���� ���̺�\n" +
               "���ϴ� ����� ��ɲ� �ɼ��� �����ϼ���!";
    }

    private async void ShowHuntingOptionSelection()
    {
        // ����� ��ɲ� ���� UI ǥ��
        selectUI = await UIManager.Instance.ShowUI<PrizeHuntingSelectUI>();

        // �̺�Ʈ ����
        selectUI.OnOptionSelected += OnOptionSelected;

        // ���� ������ �ɼ� ����
        selectUI.SetHuntingOptions(huntingSelectData.f_huntingOptions.ToList());
    }

    private void OnOptionSelected(D_HuntingOptionData option)
    {
        selectedOption = option;

        // �ɼǿ� ����� PrizeHuntingWaveData ��������
        D_PrizeHuntingWaveData prizeWaveData = option.f_prizeHuntingData;

        if (prizeWaveData != null)
        {
            // ���� ���̺�� PrizeHuntingWave ����
            StageManager.Instance.SetNextWave(new PrizeHuntingWave(prizeWaveData, selectedOption));
        }
        else
        {
            Debug.LogError("���õ� ����� ��ɲ� �ɼǿ� ����� PrizeHuntingWaveData�� �����ϴ�!");
        }

        // ���̺� �Ϸ� ó��
        isWaveCompleted = true;
        StageManager.Instance.OnWaveComplete();
    }

    public override void EndWave()
    {
        // ���ҽ� ����
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
        // ������ �ɼ��� ����� ���� ��� ����
        // ���� ������ ���� �ý��ۿ� ���� �ٸ�

        // ���� ����
        if (selectedOption.f_huntingReward != null && selectedOption.f_huntingReward.Count > 0)
        {
            foreach (var reward in selectedOption.f_huntingReward)
            {
                // ��: StatManager.ApplyStat(reward.f_statName, reward.f_value, reward.f_valueMultiply * prizeHuntingData.f_rewardMuliply);
            }
        }

        // ���� ��� ����
        if (selectedOption.f_huntingRisk != null && selectedOption.f_huntingRisk.Count > 0)
        {
            foreach (var risk in selectedOption.f_huntingRisk)
            {
                // ��: StatManager.ApplyStat(risk.f_statName, risk.f_value, risk.f_valueMultiply);
            }
        }
    }


    public override string GetWaveInfoText()
    {
        string waveInfo = "����� ��ɲ� ���� ���̺�\n";

        if (selectedOption != null)
        {
            waveInfo += $"������ ��ɲ�: {selectedOption.f_title}\n";
            waveInfo += $"����: {selectedOption.f_description}\n";
        }

        return waveInfo;
    }

    protected override void SpawnWaveEnemies()
    {
        // ���õ� �ɼ��� ����� ��ɲ� ����
        if (selectedOption.f_spawnEnemy != null && selectedOption.f_spawnEnemy.f_ObjectPoolKey != null)
        {
            //TODO : ������ �����Ϳ��� spawnPos�� �������� �ִµ� ����Ʈ ���ʹ̵� �����ų�Ÿ� ���ʹ� ��ġ ������ ���
            EnemyManager.Instance.SpawnEnemy(selectedOption.f_spawnEnemy, selectedOption.f_spawnPos);

            // ����/���� ��� ���� (�ʿ��� ���)
            ApplyRewardsAndRisks();
        }
        else
        {
            Debug.LogError("����� ��ɲ� ������Ʈ Ǯ Ű�� ����");
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
        // �ʿ��� ���ҽ� ����
        remainEnemies = 0;
        selectedOption = null;
    }

}
