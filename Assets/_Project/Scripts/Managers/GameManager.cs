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
        //매개변수로 현재 스테이지 던져야됨
        GameManager.Instance.gameState = "Gema Play!";
       
    }

    public override void UpdateState()
    {
      
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
        // 게임 일시정지
        Time.timeScale = 0;

        // TODO : 일시정지 UI 표시
        await UIManager.Instance.ShowUI<FieldGameSettingPopup>();
        
    }

    public override void ExitState()
    {
        // 게임 속도 복구
        Time.timeScale = 1;

        UIManager.Instance.CloseUI<FieldGameSettingPopup>();

    }

    // 게임으로 돌아가기
    public void ResumeGame()
    {
        GameManager.Instance.ChangeState(new GamePlayState());
    }

    // 로비로 돌아가기
    public void ReturnToLobby()
    {
        GameManager.Instance.ClearGameScene();
        UIManager.Instance.CloseUI<FullWindowInGameDlg>();
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
        // 모든 진행중인 게임플레이 중지
        Time.timeScale = 0;  // 게임 일시정지

       

        // 결과 UI 표시
        if (resultType == GameStateType.Victory)
        {
            // 승리 UI 표시
            GameManager.Instance.gameState = "Player Win";
            Debug.Log("플레이어 승리");
        }
        else
        {
            // 패배 UI 표시
            GameManager.Instance.gameState = "Player Lose";
            Debug.Log("플레이어 패배");
        }


        GameManager.Instance.ClearGameScene();

     

        await UIManager.Instance.ShowUI<FieldGameResultPopup>();

    }

    public override void ExitState()
    {
        Time.timeScale = 1;  // 게임 속도 복구
    }
}



public class GameManager : MonoBehaviour, IStatSubscriber
{
    public static GameManager _instance;

    public GameState currentState;

    public Camera mainCamera;

    public int SelectedEpisodeNumber { get; private set; }
    public int SelectedStageNumber { get; set; }

    private PlayerCamp playerCamp;


    // 시스템 스탯 저장소
    private Dictionary<StatName, StatStorage> systemStats = new Dictionary<StatName, StatStorage>();

    public event System.Action<float> OnHPChanged;    // HP 변경 이벤트

    public event System.Action OnCostAdd; // 코스트 추가 이벤트
    public event System.Action<int> OnCostUsed; // 코스트 사용 이벤트
    public event System.Action<int> OnCostUsePrePare; // 코스트 사용 이벤트
    public event System.Action OnCostUsePrePareCancle; // 코스트 사용 이벤트


    //test용

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
        // 이벤트 정리
        OnHPChanged = null;  
        OnCostUsed = null;
        OnCostAdd = null;


        playerCamp = null;

        mainCamera = Camera.main;
   
    }

    // StatManager로부터 시스템 스탯 로드
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

        // currentHP를 maxHP로 초기화
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

        // 시스템 스탯 변경 구독
        StatManager.Instance.Subscribe(this, StatSubject.System);
    }


    // StatManager로부터 스탯 변경 알림 받기
    public virtual void OnStatChanged(StatSubject subject, StatStorage statChange)
    {
        if (subject != StatSubject.System) return;

        // 변경된 스탯에 따른 이벤트 발생
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
            case StatName.CostChargingSpeed:
                ChangeStatCostChargingSpeed(statChange);
                break;
        }
    }

    // 특정 스탯 값 가져오기
    public int GetSystemStat(StatName statName)
    {
        if (systemStats.TryGetValue(statName, out var stat))
        {
            return stat.value;
        }
        return 0;
    }

    // 스테이지 선택 메서드
    public bool SelectEpisode(int episodeNumber)
    {
        // 해금된 스테이지인지 확인
        if (IsEpisodeUnlocked(episodeNumber))
        {
            SelectedEpisodeNumber = episodeNumber;
            return true;
        }
        return false;
    }
    // 스테이지 해금 상태 확인
    private bool IsEpisodeUnlocked(int episodeNumber)
    {
        var userData = D_LocalUserData.GetEntity(0); // 첫 번째 엔티티 사용

        return episodeNumber == 1 || episodeNumber <= userData.f_clearEpisodeNumber + 1;
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
            value = Mathf.Max(0, currentHp + statChange.value), // 체력 감소 처리
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
            value = maxHp + statChange.value, // 최대 체력 증가
            multiply = statChange.multiply
        };

        OnHPChanged?.Invoke(GetSystemStat(StatName.CurrentHp));
    }

    private void ChangeStatCost(StatStorage statChange)
    {
        var currentCost = systemStats.ContainsKey(StatName.Cost)? systemStats[StatName.Cost].value: 0;

        var newCost = currentCost + statChange.value;

        // 시스템 스탯 업데이트
        systemStats[StatName.Cost] = new StatStorage
        {
            statName = StatName.Cost,
            value = newCost,
            multiply = statChange.multiply
        };

        // 이벤트 호출: value가 양수인지 음수인지에 따라 결정
        if (statChange.value > 0)
        {
            OnCostAdd?.Invoke(); // 코스트 증가 이벤트
        }
        else if (statChange.value < 0)
        {
            OnCostUsed?.Invoke(Mathf.Abs(statChange.value)); // 코스트 소모 이벤트
        }
    }

    public void PrepareUseCost(int value)
    {
        OnCostUsePrePare?.Invoke(value);
    }

    public void CanclePrepareUseCost()
    {
        OnCostUsePrePareCancle?.Invoke();
    }
    private void ChangeStatStoreLevel(StatStorage statChange)
    {
        // StoreLevel 업데이트
        systemStats[StatName.StoreLevel] = new StatStorage
        {
            statName = StatName.StoreLevel,
            value = statChange.value, // 새로운 레벨로 덮어쓰기
            multiply = statChange.multiply
        };

        // MaxCost 업데이트
        var maxCost = Mathf.Clamp(9 + statChange.value, 10, 20);

        systemStats[StatName.MaxCost] = new StatStorage
        {
            statName = StatName.MaxCost,
            value = maxCost,
            multiply = 1f
        };

    }

    private void ChangeStatCostChargingSpeed(StatStorage statChange)
    {
        systemStats[StatName.CostChargingSpeed] = new StatStorage
        {
            statName = StatName.CostChargingSpeed,
            value = statChange.value,
            multiply = statChange.multiply
        };
    }
    public void InitializePlayerCamp(Vector2 endTile)
    {
        //endTile에 playerCamp 설치
        GameObject obj = ResourceManager.Instance.Instantiate("PlayerCamp");
        playerCamp = obj.GetComponent<PlayerCamp>();

        playerCamp.InitializeObect();

        Vector3 campPosition = TileMapManager.Instance.GetTileToWorldPosition(endTile);
        playerCamp.transform.position = campPosition;
    }

    public void ClearGameScene()
    {
        // 유닛과 적 상태 변경
        var units = UnitManager.Instance.GetAllUnits();
        foreach (var unit in units)
        {
            unit.SetActive(false);
        }

        var enemies = EnemyManager.Instance.GetAllEnemys();
        foreach (var enemy in enemies)
        {
            enemy.SetActive(false);
        }

        UnitManager.Instance.CleanUp();
        EnemyManager.Instance.CleanUp();
        StageManager.Instance.CleanUp();
        BuffManager.Instance.CleanUp();
        TimeTableManager.Instance.CleanUp();
        SkillManager.Instance.ClearAllSkills();

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
