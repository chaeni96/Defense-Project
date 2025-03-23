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

}

/// <summary>
/// �Ϲ� ���� ���̺� Ŭ����
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
        string waveInfo = "�Ϲ� ���� ���̺�\n";

        // ���� �� Ÿ�Գ��� �׷�ȭ�ؼ� ǥ��
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

        // �� �� �� ���
        remainEnemies = normalWaveData.f_enemyGroups.Sum(group => group.f_amount);
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // �� ���� ����
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
            Debug.LogError("�� �׷� ������ ����");
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
                Debug.LogError("������Ʈ Ǯ Ű�� ����");
                break;
            }

            yield return new WaitForSeconds(enemyGroupData.f_spawnInterval);
        }

        ++completedGroupCount;

        // ��� �׷��� ������ �Ϸ��ߴ��� Ȯ��
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
        // �ʿ��� ���ҽ� ����
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}

/// <summary>
/// ���� ���� ���̺� Ŭ����
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
        string waveInfo = "���� ���� ���̺�\n";
        waveInfo += $"����: {bossWaveData.f_bossEnemy.f_name}\n";

        // ����Ʈ ���� �ִٸ� ǥ��
        if (bossWaveData.f_supportEnemyGroups != null && bossWaveData.f_supportEnemyGroups.Count > 0)
        {
            var groupedEnemies = bossWaveData.f_supportEnemyGroups
                                .GroupBy(g => g.f_enemy.f_name)
                                .Select(g => new { Name = g.Key, TotalAmount = g.Sum(x => x.f_amount) });

            waveInfo += "������:";
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

        // ���� + ����Ʈ �� �� �� ���
        remainEnemies = 1; // ����
        if (bossWaveData.f_supportEnemyGroups != null)
        {
            remainEnemies += bossWaveData.f_supportEnemyGroups.Sum(group => group.f_amount);
        }

        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // ������ ����Ʈ �� ����
        SpawnBossAndSupportEnemies();
    }

    private void SpawnBossAndSupportEnemies()
    {
        // �� ������ �׷� �� ��� (���� + ����Ʈ �׷�)
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
        else
        {
            Debug.LogError("���� ������Ʈ Ǯ Ű�� ����");
        }

        ++completedGroupCount;
        CheckSpawnCompletion();
    }

    private IEnumerator CoSpawnSupportGroup(D_supportEnemyGroups groupData)
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
        // �ʿ��� ���ҽ� ����
        isSpawnDone = false;
        remainEnemies = 0;
        completedGroupCount = 0;
    }
}

/// <summary>
/// �̺�Ʈ ���ʹ� ���̺� Ŭ����
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
        string waveInfo = "�̺�Ʈ ���ʹ� ���̺�\n";

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

        // �� �� �� ���
        remainEnemies = eventWaveData.f_eventEnemyGroups.Sum(group => group.f_amount);
        StageManager.Instance.SetTotalEnemyCount(remainEnemies);

        // �̺�Ʈ �� ����
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
            Debug.LogError("�̺�Ʈ ���ʹ� �׷� ������ ����");
            yield break;
        }

        yield return new WaitForSeconds(groupData.f_startDelay);

        for (int spawnedCount = 0; spawnedCount < groupData.f_amount; spawnedCount++)
        {
            if (groupData.f_enemy.f_ObjectPoolKey != null)
            {
                // �̺�Ʈ �� ���� (��� ������ ���� ����)
                EnemyManager.Instance.SpawnEnemy(groupData.f_enemy);
                // TODO: ��� ������ ���� ����
            }
            else
            {
                Debug.LogError("�̺�Ʈ �� ������Ʈ Ǯ Ű�� ����");
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
        // �ʿ��� ���ҽ� ����
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
        //timeScheduleUID = TimeTableManager.Instance.RegisterSchedule(selectionTime);
        //TimeTableManager.Instance.AddScheduleCompleteTargetSubscriber(this, timeScheduleUID);
        //TimeTableManager.Instance.AddTimeChangeTargetSubscriber(this, timeScheduleUID);

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
            // ���ϵ�ī�� ���� �ð� ������Ʈ
            if (selectUI != null)
            {
                selectUI.UpdateSelectTime(Mathf.CeilToInt(remainTime));
            }

            // ī��Ʈ�ٿ� UI�� �ִ� ��� ������Ʈ
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
            // ���ϵ�ī�� ���� �ð� �Ϸ�
            if (selectUI != null)
            {
                UIManager.Instance.CloseUI<WildCardSelectUI>();
            }

            if (countdownUI != null)
            {
                UIManager.Instance.CloseUI<InGameCountdownUI>();
            }

            timeScheduleUID = -1;

            // ���̺� �Ϸ� ó��
            isWaveCompleted = true;
            StageManager.Instance.OnWaveComplete();
        }
    }*/

    public override void EndWave()
    {
        // ���ҽ� ����
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