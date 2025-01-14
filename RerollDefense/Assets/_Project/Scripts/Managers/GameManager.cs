using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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



public class GameManager : MonoBehaviour
{
    public static GameManager _instance;

    public GameState currentState;

    public Camera mainCamera;

    private PlayerCamp playerCamp;

    public float PlayerHP { get; private set; } = 300f;
    public float MaxHP { get; private set; } = 1500f;

    //나중에 게임씬이니셜라이즈할때 사용하기
    public int CurrentCost = 2;
    public int MaxCost = 10;
    public int StoreLevel { get; private set; } = 1;


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

    public void TakeDamage(float damage)
    {
        PlayerHP = Mathf.Max(0f, PlayerHP - damage);
        OnHPChanged?.Invoke(PlayerHP);  // HP 변경시에만 이벤트 발생

        if (PlayerHP <= 0)
        {
            // TODO : 게임오버 처리
            //hp Bar 다 닳고나면 게임오버처리하기
        }
    }

    public void AddCost(int amount)
    {
        CurrentCost += amount;
        OnCostAdd?.Invoke();
    }

    public bool UseCost(int amount)
    {
        if (CurrentCost >= amount)
        {
            CurrentCost -= amount;
            OnCostUsed?.Invoke(amount); // 이벤트 호출
            return true;
        }
        return false;
    }

   

    public void SetStoreLevel(int level)
    {
        StoreLevel = level;
        MaxCost = Mathf.Clamp(9 + level, 10, 20);

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

    }


}
