using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UnityEngine.CullingGroup;

public enum GameStateType
{
    Ready,
    Playing,
    Victory,
    Defeat
}


public class GameState
{
    public virtual void EnterState()
    {

    }
    public virtual void UpdateState()
    {

    }
    public virtual void ExitState()
    {

    }
}

public class GameLobbyState : GameState
{
    public override void EnterState()
    {
        GameSceneManager.Instance.LoadScene(SceneKind.Lobby);
    }

    public override void UpdateState()
    {
     
    }


    public override void ExitState()
    {

    }
}
public class GamePlayState : GameState
{
    public override void EnterState()
    {
        //�Ű������� ���� �������� �����ߵ�
        GameManager.Instance.gameState = "Gema Play!";
       
    }

    public override void UpdateState()
    {
        if (EnemyManager.Instance.GetAllEnemys().Count == 0 && StageManager.Instance.IsLastWave())
        {
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Victory));
        }
    }

  
    public override void ExitState()
    {
       
    }
}

public class GamePauseState : GameState
{
    private GameState previousState;

    public GamePauseState(GameState priorState)
    {
        previousState = priorState;
    }

    public async override void EnterState()
    {
        // ���� �Ͻ�����
        Time.timeScale = 0;

        // TODO : �Ͻ����� UI ǥ��
        await UIManager.Instance.ShowUI<FieldGameSettingPopup>();
        
    }

    public override void ExitState()
    {
        // ���� �ӵ� ����
        Time.timeScale = 1;

        UIManager.Instance.CloseUI<FieldGameSettingPopup>();

    }

    // �������� ���ư���
    public void ResumeGame()
    {
        GameManager.Instance.ChangeState(new GamePlayState());
    }

    // �κ�� ���ư���
    public void ReturnToLobby()
    {
        GameManager.Instance.ClearGameScene();
        GameManager.Instance.ChangeState(new GameLobbyState()); 
    }
}

public class GameResultState : GameState
{
    private GameStateType resultType;

    public GameResultState(GameStateType type)
    {
        resultType = type;
    }

    public async  override void EnterState()
    {
        // ��� �������� �����÷��� ����
        Time.timeScale = 0;  // ���� �Ͻ�����

       

        // ��� UI ǥ��
        if (resultType == GameStateType.Victory)
        {
            // �¸� UI ǥ��
            GameManager.Instance.gameState = "Player Win";
            Debug.Log("�÷��̾� �¸�");
        }
        else
        {
            // �й� UI ǥ��
            GameManager.Instance.gameState = "Player Lose";
            Debug.Log("�÷��̾� �й�");
        }


        GameManager.Instance.ClearGameScene();

     

        await UIManager.Instance.ShowUI<FieldGameResultPopup>();

    }

    public override void ExitState()
    {
        Time.timeScale = 1;  // ���� �ӵ� ����
    }
}



public class GameManager : MonoBehaviour, IStatSubscriber
{
    public static GameManager _instance;

    public GameState currentState;

    public Camera mainCamera;

    public int SelectedStageNumber { get; private set; }

    private PlayerCamp playerCamp;


    // �ý��� ���� �����
    private Dictionary<StatName, StatStorage> systemStats = new Dictionary<StatName, StatStorage>();

    public event System.Action<float> OnHPChanged;    // HP ���� �̺�Ʈ

    public event System.Action OnCostAdd; // �ڽ�Ʈ �߰� �̺�Ʈ
    public event System.Action<int> OnCostUsed; // �ڽ�Ʈ ��� �̺�Ʈ

    //test��

    public string gameState;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();

                if (_instance == null)
                {
                    GameObject singleton = new GameObject("GameManager");
                    _instance = singleton.AddComponent<GameManager>();
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
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

    }

    public void InitializeManager()
    {
        // �̺�Ʈ ����
        OnHPChanged = null;  
        OnCostUsed = null;
        OnCostAdd = null;


        playerCamp = null;

        mainCamera = Camera.main;
   
    }

    // StatManager�κ��� �ý��� ���� �ε�
    public void LoadSystemStats()
    {
        var stats = StatManager.Instance.GetAllStatsForSubject(StatSubject.System);
        foreach (var stat in stats)
        {
            systemStats[stat.statName] = new StatStorage
            {
                statName = stat.statName,
                value = stat.value,
                multiply = stat.multiply
            };
        }

        // currentHP�� maxHP�� �ʱ�ȭ
        if (!systemStats.ContainsKey(StatName.CurrentHp))
        {
            var maxHp = GetSystemStat(StatName.MaxHP);
            systemStats[StatName.CurrentHp] = new StatStorage
            {
                statName = StatName.CurrentHp,
                value = Mathf.FloorToInt(maxHp),
                multiply = 1f
            };
        }

        // �ý��� ���� ���� ����
        StatManager.Instance.Subscribe(this, StatSubject.System);
    }


    // StatManager�κ��� ���� ���� �˸� �ޱ�
    public virtual void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (subject != StatSubject.System) return;

        // ����� ���ȿ� ���� �̺�Ʈ �߻�
        switch (statChange.statName)
        {
            case StatName.CurrentHp:
                ChangeStatHP(statChange);
                break;
            case StatName.MaxHP:
                ChangeStatMaxHP(statChange);
                break;
            case StatName.Cost:
                ChangeStatCost(statChange);
                break;
            case StatName.StoreLevel:
                ChangeStatStoreLevel(statChange);
                break;
        }
    }

    // Ư�� ���� �� ��������
    public int GetSystemStat(StatName statName)
    {
        if (systemStats.TryGetValue(statName, out var stat))
        {
            return stat.value;
        }
        return 0;
    }

    // �������� ���� �޼���
    public bool SelectStage(int stageNumber)
    {
        // �رݵ� ������������ Ȯ��
        if (IsStageUnlocked(stageNumber))
        {
            SelectedStageNumber = stageNumber;
            return true;
        }
        return false;
    }
    // �������� �ر� ���� Ȯ��
    private bool IsStageUnlocked(int stageNumber)
    {
        // TODO  : ���⿡ �������� �ر� ���� ����
        // ex) ����� �����ͳ� ���� ���� ��Ȳ Ȯ��
        return stageNumber <= 2; // ����� 2������ �ر�
    }


    private void Update()
    {
        currentState?.UpdateState();
    }

    public void ChangeState<T>(T state) where T : GameState
    {
        currentState?.ExitState();
        currentState = state;
        currentState?.EnterState();

    }

    public void ChangeStatHP(StatStorage statChange)
    {
        var currentHp = systemStats.ContainsKey(StatName.CurrentHp)? systemStats[StatName.CurrentHp].value : 0;
        OnHPChanged?.Invoke(GetSystemStat(StatName.CurrentHp));
        
        systemStats[StatName.CurrentHp] = new StatStorage
        {
            statName = StatName.CurrentHp,
            value = Mathf.Max(0, currentHp + statChange.value), // ü�� ���� ó��
            multiply = statChange.multiply
        };

        OnHPChanged?.Invoke(GetSystemStat(StatName.CurrentHp));

    }

    private void ChangeStatMaxHP(StatStorage statChange)
    {
        var maxHp = systemStats.ContainsKey(StatName.MaxHP)
            ? systemStats[StatName.MaxHP].value
            : 0;

        systemStats[StatName.MaxHP] = new StatStorage
        {
            statName = StatName.MaxHP,
            value = maxHp + statChange.value, // �ִ� ü�� ����
            multiply = statChange.multiply
        };

        OnHPChanged?.Invoke(GetSystemStat(StatName.CurrentHp));
    }

    private void ChangeStatCost(StatStorage statChange)
    {
        var currentCost = systemStats.ContainsKey(StatName.Cost)? systemStats[StatName.Cost].value: 0;

        var newCost = currentCost + statChange.value;

        // �ý��� ���� ������Ʈ
        systemStats[StatName.Cost] = new StatStorage
        {
            statName = StatName.Cost,
            value = newCost,
            multiply = statChange.multiply
        };

        // �̺�Ʈ ȣ��: value�� ������� ���������� ���� ����
        if (statChange.value > 0)
        {
            OnCostAdd?.Invoke(); // �ڽ�Ʈ ���� �̺�Ʈ
        }
        else if (statChange.value < 0)
        {
            OnCostUsed?.Invoke(Mathf.Abs(statChange.value)); // �ڽ�Ʈ �Ҹ� �̺�Ʈ
        }
    }


    private void ChangeStatStoreLevel(StatStorage statChange)
    {
        // StoreLevel ������Ʈ
        systemStats[StatName.StoreLevel] = new StatStorage
        {
            statName = StatName.StoreLevel,
            value = statChange.value, // ���ο� ������ �����
            multiply = statChange.multiply
        };

        // MaxCost ������Ʈ
        var maxCost = Mathf.Clamp(9 + statChange.value, 10, 20);

        systemStats[StatName.MaxCost] = new StatStorage
        {
            statName = StatName.MaxCost,
            value = maxCost,
            multiply = 1f
        };

        Debug.Log($"StoreLevel updated to {statChange.value}, MaxCost updated to {maxCost}");
    }
    public void InitializePlayerCamp(Vector2 endTile)
    {
        //endTile�� playerCamp ��ġ
        GameObject obj = ResourceManager.Instance.Instantiate("PlayerCamp");
        playerCamp = obj.GetComponent<PlayerCamp>();

        playerCamp.InitializeObect();

        Vector3 campPosition = TileMapManager.Instance.GetTileToWorldPosition(endTile);
        playerCamp.transform.position = campPosition;
    }

    public void ClearGameScene()
    {
        // ���ְ� �� ���� ����
        var units = UnitManager.Instance.GetUnits();
        foreach (var unit in units)
        {
            unit.SetActive(false);
        }

        var enemies = EnemyManager.Instance.GetAllEnemys();
        foreach (var enemy in enemies)
        {
            enemy.SetActive(false);
        }



        // ���� ���� ��� ����ü ����
        ProjectileManager.Instance.CleanUp();

        // �������� ��� ��ų ����
        AttackSkillManager.Instance.CleanUp();  

        UnitManager.Instance.CleanUp();
        EnemyManager.Instance.CleanUp();
        StageManager.Instance.CleanUp();
        BuffManager.Instance.CleanUp();
        TimeTableManager.Instance.CleanUp();

        CleanUp();
    }

    public void CleanUp()
    {
        StatManager.Instance.Unsubscribe(this, StatSubject.System);
        systemStats.Clear();
        OnHPChanged = null;
        OnCostUsed = null;
        OnCostAdd = null;
    }


}
