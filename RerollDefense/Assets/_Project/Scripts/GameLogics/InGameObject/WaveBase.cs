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

    public virtual void AddEnemies(int count) { }

    public virtual void DecreaseEnemyCount() { }

    //����, �߰� ���� �� ȿ���� ���� �ð� ���� �ڷ�ƾ��
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


    //������ ������ ȸ��, ������ ���� ����, ���� ���� ���� ��� �ð�
    protected float interestDelay = 0.6f; //�ϴ� 0.6��
    public BattleWaveBase(D_WaveDummyData data) : base(data)
    {
    }
    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        // �ڽ� Ŭ�������� �� �� �� ���
        remainEnemies = CalculateTotalEnemies();
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // TODO:��⸰
        // ���⼭ "StartWave ��ư"�� Ȱ��ȭ�ϴ� UI�� Ȱ��ȭ �ϵ��� �ؾ���.OnStartWaveButtonClicked�����Ұ�.
        // Ȥ�� �̺�Ʈ ���� �ʿ�

        SpawnWaveEnemies();

        Debug.Log("���� ���̺� ���� �غ� �Ϸ�! ����� �Է��� ����մϴ�.");
    }

    //���̺� ���� ��ư Ŭ���� ȣ��� �޼���
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
/// �Ϲ� ���� ���̺� Ŭ����
/// </summary>
public class NormalBattleWave : BattleWaveBase
{
    private D_NormalBattleWaveData normalWaveData;

    private D_EnemyPlacementData placementData;
    private float gridSize = 0.6f; // �׸��� �� ĭ�� ũ��


    private int totalEnemies = 0;
    private int remainEnemies = 0;
    private bool isSpawnDone = false;

    private int mapId;
    private Vector2 centerOffset = new Vector2(0f, 2.5f); // �߾� ������ ������

    public NormalBattleWave(D_NormalBattleWaveData data) : base(data)
    {
        normalWaveData = data;

        // �ش� �� ID�� ���� ��ġ ������ �ε�
        placementData = D_EnemyPlacementData.FindEntity(p => p.f_mapID == normalWaveData.f_mapID);

        if (placementData == null)
        {
            Debug.LogError($"�� ID {mapId}�� ���� ���ʹ� ��ġ �����Ͱ� �����ϴ�!");
        }

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

    public override void StartWave()
    {
        isWaveCompleted = false;
        isSpawnDone = false;

        if (placementData == null)
        {
            Debug.LogError($"�� ID {mapId}�� ���� ���ʹ� ��ġ �����Ͱ� �����ϴ�!");
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
            return;
        }

        // �� ���ʹ� �� ���
        totalEnemies = placementData.f_cellData.Count;
        remainEnemies = totalEnemies;
        StageManager.Instance.SetTotalEnemyCount(totalEnemies);

        // ���ʹ� ����
        SpawnWaveEnemies();
    }

    protected override void SpawnWaveEnemies()
    {
        foreach (var cellData in placementData.f_cellData)
        {
            if (cellData.f_enemy != null && cellData.f_enemy.f_ObjectPoolKey != null)
            {
                // �׸��� ��ǥ�� ���� ���� ��ǥ�� ��ȯ
                Vector2 worldPos = ConvertGridToWorldPosition(cellData.f_position);

                // ���ʹ� ���� ����
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

    // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�ϴ� �Լ�
    private Vector2 ConvertGridToWorldPosition(Vector2 gridPos)
    {
        float gridCenterX = 0f;
        float gridCenterY = 4f;

        // �߾��� �������� ��ǥ ���
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
/// ���� ���� ���̺� Ŭ����
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
        string waveInfo = "���� ���� ���̺�\n";
        waveInfo += $"����: {bossWaveData.f_bossEnemy.f_name}\n";

        if (bossWaveData.f_supportEnemyGroups != null && bossWaveData.f_supportEnemyGroups.Count > 0)
        {
            var groupedEnemies = bossWaveData.f_supportEnemyGroups
                                    .GroupBy(g => g.f_enemy.f_name)
                                    .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "������:\n";
            foreach (var group in groupedEnemies)
            {
                waveInfo += $"{group.Name} x{group.TotalAmount}\n";
            }
        }

        return waveInfo;
    }

    protected override int CalculateTotalEnemies()
    {
        int total = 1; // ���� 1����
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
        totalGroupCount = 1; // ���� �׷�
        if (bossWaveData.f_supportEnemyGroups != null)
        {
            totalGroupCount += bossWaveData.f_supportEnemyGroups.Count;
        }

        completedGroupCount = 0;

        // ���� ����
        StageManager.Instance.StartCoroutine(CoSpawnBoss());

        // ����Ʈ �� ����
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
/// �̺�Ʈ ���ʹ� ���̺� Ŭ����
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
        string waveInfo = "���� ��� ���̺�\n";
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
                // �̺�Ʈ �� ���� (��� ������ ���� ���� ����)
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
            //�������� �����ؾ� �ѱ�� 
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
            Debug.LogError("����� ��ɲ� �ɼ� �����Ͱ� �����ϴ�!");
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
            return;
        }

        // ����� ��ɲ� + ����Ʈ �� �� �� ���
        remainEnemies = 1; // ���� ����� ��ɲ�
        if (prizeHuntingData.f_supportEnemys != null && prizeHuntingData.f_supportEnemys.Count > 0)
        {
            remainEnemies += prizeHuntingData.f_supportEnemys.Sum(group => group.f_amount);
        }

        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // ����� ��ɲ۰� ����Ʈ �� ����
        SpawnHuntingAndSupportEnemies();
    }

    private void SpawnHuntingAndSupportEnemies()
    {
        // �� ������ �׷� �� ��� (����� ��ɲ� + ����Ʈ �׷�)
        totalGroupCount = 1; // ����� ��ɲ�
        if (prizeHuntingData.f_supportEnemys != null)
        {
            totalGroupCount += prizeHuntingData.f_supportEnemys.Count;
        }

        completedGroupCount = 0;

        // ���� ����� ��ɲ� ����
        StageManager.Instance.StartCoroutine(CoSpawnHuntingEnemy());

        // ����Ʈ �� ����
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
        yield return new WaitForSeconds(0.5f); // �ణ�� ����

        // ���õ� �ɼ��� ����� ��ɲ� ����
        if (selectedOption.f_spawnEnemy != null && selectedOption.f_spawnEnemy.f_ObjectPoolKey != null)
        {
            // ���õ� �ɼǿ� ���� ����� ��ɲ� ����
            EnemyManager.Instance.SpawnEnemy(selectedOption.f_spawnEnemy);

            // ����/���� ��� ���� (�ʿ��� ���)
            ApplyRewardsAndRisks();
        }
        else
        {
            Debug.LogError("����� ��ɲ� ������Ʈ Ǯ Ű�� ����");
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private IEnumerator CoSpawnSupportGroup(D_supportEnemys groupData)
    {
        if (groupData == null || groupData.f_enemy == null)
        {
            Debug.LogError("����Ʈ �׷� ������ ����");
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
                Debug.LogError("����Ʈ �� ������Ʈ Ǯ Ű�� ����");
                break;
            }

            yield return new WaitForSeconds(groupData.f_spawnInterval);
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
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
        string waveInfo = "����� ��ɲ� ���� ���̺�\n";

        if (selectedOption != null)
        {
            waveInfo += $"������ ��ɲ�: {selectedOption.f_title}\n";
            waveInfo += $"����: {selectedOption.f_description}\n";
        }

        // ����Ʈ ���� �ִٸ� ǥ��
        if (prizeHuntingData.f_supportEnemys != null && prizeHuntingData.f_supportEnemys.Count > 0)
        {
            var groupedEnemies = prizeHuntingData.f_supportEnemys
                                .GroupBy(g => g.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "����Ʈ ��:";
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
        // �ʿ��� ���ҽ� ����
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
        selectedOption = null;
    }
}
