using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
       
        GameManager.Instance.gaemState = "Gema Play!";
       
    }

    public override void UpdateState()
    {
        if (EnemyManager.Instance.GetEnemies().Count == 0 && StageManager.Instance.IsLastWave())
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

    public override void EnterState()
    {
        // ��� �������� �����÷��� ����
        Time.timeScale = 0;  // ���� �Ͻ�����

        // ���ְ� �� ���� ����
        var units = UnitManager.Instance.GetUnits();
        foreach (var unit in units)
        {
            unit.SetActive(false);
        }

        var enemies = EnemyManager.Instance.GetEnemies();
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

        // ���� ���� ��� AOE ����Ʈ ���� -> ���߿� ����
        var aoes = Object.FindObjectsOfType<TheAOE>();
        foreach (var aoe in aoes)
        {
            PoolingManager.Instance.ReturnObject(aoe.gameObject);
        }

        // ��� UI ǥ��
        if (resultType == GameStateType.Victory)
        {
            // �¸� UI ǥ��
            GameManager.Instance.gaemState = "Player Win";
            Debug.Log("�÷��̾� �¸�");
        }
        else
        {
            // �й� UI ǥ��
            GameManager.Instance.gaemState = "Player Lose";
            Debug.Log("�÷��̾� �й�");
        }

        UIManager.Instance.ShowUI<GameResultUI>("GameResultPopup");

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

    public float PlayerHP { get; private set; } = 100f;
    public float MaxHP { get; private set; } = 100f;

    public int CurrentCost { get; private set; }
    public int MaxCost;
    public int StoreLevel { get; private set; } = 0;


    public event System.Action<float> OnHPChanged;    // HP ���� �̺�Ʈ
    public event System.Action<int> OnCostUsed; // �ڽ�Ʈ ��� �̺�Ʈ

    //test��

    public string gaemState;
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

    public void InitGameManager()
    {
        mainCamera = Camera.main;
        CurrentCost = 0;
        MaxCost = 10;
        StoreLevel = 1;
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

    public void AddMana()
    {
        if (CurrentCost < MaxCost)
        {
            CurrentCost++;
        }
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
        // ���� �ý��� UI ������Ʈ �ʿ�
    }


    private void OnDestroy()
    {
        OnHPChanged = null;  // �̺�Ʈ ����
        OnCostUsed = null;
    }



}
