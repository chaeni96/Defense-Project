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
public class GamePlayState : GameState
{
   

    public override void EnterState()
    {
        //매개변수로 현재 스테이지 던져야됨
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

        // 유닛과 적 상태 변경
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

        // 진행 중인 모든 투사체 제거
        var projectiles = Object.FindObjectsOfType<TheProjectile>();
        foreach (var projectile in projectiles)
        {
            PoolingManager.Instance.ReturnObject(projectile.gameObject);
        }

        // TODO : 진행 중인 모든 AOE 이펙트 제거 -> 나중에 변경 list로 모아서 관리
        var aoes = Object.FindObjectsOfType<TheAOE>();
        foreach (var aoe in aoes)
        {
            PoolingManager.Instance.ReturnObject(aoe.gameObject);
        }

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

        UnitManager.Instance.CleanUp();
        EnemyManager.Instance.CleanUp();
        GameManager.Instance.CleanUp(); 

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

    private PlayerCamp playerCamp;


    // 시스템 스탯 저장소
    private Dictionary<StatName, StatStorage> systemStats = new Dictionary<StatName, StatStorage>();

    public event System.Action<float> OnHPChanged;    // HP 변경 이벤트

    public event System.Action OnCostAdd; // 코스트 추가 이벤트
    public event System.Action<int> OnCostUsed; // 코스트 사용 이벤트

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
            systemStats[stat.stat] = new StatStorage
            {
                stat = stat.stat,
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
                stat = StatName.CurrentHp,
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
        switch (statChange.stat)
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

    // 특정 스탯 값 가져오기
    public int GetSystemStat(StatName statName)
    {
        if (systemStats.TryGetValue(statName, out var stat))
        {
            return stat.value;
        }
        return 0;
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
            stat = StatName.CurrentHp,
            value = Mathf.Max(0, currentHp + statChange.value), // 체력 감소 처리
            multiply = statChange.multiply
        };

        // 체력이 0 이하인 경우 게임 오버 처리
        if (GetSystemStat(StatName.CurrentHp) <= 0)
        {
            // TODO: 게임 오버 로직
        }

        OnHPChanged?.Invoke(GetSystemStat(StatName.CurrentHp));

    }

    private void ChangeStatMaxHP(StatStorage statChange)
    {
        var maxHp = systemStats.ContainsKey(StatName.MaxHP)
            ? systemStats[StatName.MaxHP].value
            : 0;

        systemStats[StatName.MaxHP] = new StatStorage
        {
            stat = StatName.MaxHP,
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
            stat = StatName.Cost,
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


    private void ChangeStatStoreLevel(StatStorage statChange)
    {
        // StoreLevel 업데이트
        systemStats[StatName.StoreLevel] = new StatStorage
        {
            stat = StatName.StoreLevel,
            value = statChange.value, // 새로운 레벨로 덮어쓰기
            multiply = statChange.multiply
        };

        // MaxCost 업데이트
        var maxCost = Mathf.Clamp(9 + statChange.value, 10, 20);

        systemStats[StatName.MaxCost] = new StatStorage
        {
            stat = StatName.MaxCost,
            value = maxCost,
            multiply = 1f
        };

        Debug.Log($"StoreLevel updated to {statChange.value}, MaxCost updated to {maxCost}");
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

    public void CleanUp()
    {
        StatManager.Instance.Unsubscribe(this, StatSubject.System);
        systemStats.Clear();
        OnHPChanged = null;
        OnCostUsed = null;
        OnCostAdd = null;
    }


}
