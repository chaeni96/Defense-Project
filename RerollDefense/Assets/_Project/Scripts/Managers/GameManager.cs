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
        var projectiles = Object.FindObjectsOfType<TheProjectile>();
        foreach (var projectile in projectiles)
        {
            PoolingManager.Instance.ReturnObject(projectile.gameObject);
        }

        // TODO : ���� ���� ��� AOE ����Ʈ ���� -> ���߿� ���� list�� ��Ƽ� ����
        var aoes = Object.FindObjectsOfType<TheAOE>();
        foreach (var aoe in aoes)
        {
            PoolingManager.Instance.ReturnObject(aoe.gameObject);
        }

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

        UnitManager.Instance.CleanUp();
        EnemyManager.Instance.CleanUp();
        GameManager.Instance.CleanUp(); 

        await UIManager.Instance.ShowUI<FieldGameResultPopup>();

    }

    public override void ExitState()
    {
        Time.timeScale = 1;  // ���� �ӵ� ����
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

    //���߿� ���Ӿ��̴ϼȶ������Ҷ� ����ϱ�
    public int CurrentCost = 2;
    public int MaxCost = 10;
    public int StoreLevel { get; private set; } = 1;


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
        OnHPChanged?.Invoke(PlayerHP);  // HP ����ÿ��� �̺�Ʈ �߻�

        if (PlayerHP <= 0)
        {
            // TODO : ���ӿ��� ó��
            //hp Bar �� ����� ���ӿ���ó���ϱ�
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
            OnCostUsed?.Invoke(amount); // �̺�Ʈ ȣ��
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
        //endTile�� playerCamp ��ġ
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
