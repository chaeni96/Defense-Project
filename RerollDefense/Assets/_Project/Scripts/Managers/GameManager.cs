using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


public class GameManager : MonoBehaviour
{
    public static GameManager _instance;

    public Camera mainCamera;

    public float PlayerHP { get; private set; } = 100f;
    public float MaxHP { get; private set; } = 100f;


    public int CurrentCost { get; private set; }
    public int MaxCost;
    public int StoreLevel { get; private set; } = 0;

    public event System.Action<int> OnCostUsed; // �ڽ�Ʈ ��� �̺�Ʈ

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

        SaveLoadManager.Instance.LoadData();

        //TODO : ���߿� ������� �־��ֱ�
        InitGameManager();
    }

    public void InitGameManager()
    {
        mainCamera = Camera.main;
        CurrentCost = 0;
        MaxCost = 10;
        StoreLevel = 1;
    }

    // HP ���� �̺�Ʈ
    public event System.Action<float> OnHPChanged;

    public void TakeDamage(float damage)
    {
        PlayerHP = Mathf.Max(0f, PlayerHP - damage);
        OnHPChanged?.Invoke(PlayerHP);  // HP ����ÿ��� �̺�Ʈ �߻�

        if (PlayerHP <= 0)
        {
            // ���ӿ��� ó��
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

}
