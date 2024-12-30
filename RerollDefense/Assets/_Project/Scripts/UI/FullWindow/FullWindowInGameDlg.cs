using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class FullWindowInGameDlg : FullWindowBase
{

    //TODO : UIBase ��ӹ޾ƾߵ�

    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;

    [SerializeField] private Slider hpBar;
    [SerializeField] private float hpUpdateSpeed = 2f;  // HP Bar ���� �ӵ�
    [SerializeField] private GameObject CostGauge;  // HP Bar ���� �ӵ�
    [SerializeField] private TMP_Text shopLevelText;  // ���� ��������
    [SerializeField] private TMP_Text shopLevelUpCostText;  // ���׷��̵� ��� ǥ�� �ؽ�Ʈ

    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;

    private List<GameObject> cardDecks;
    private bool isChecking = false;

    //test�� ������
    public float checkCooldown = 3f;
    private int shopLevel;
    private int shopUpgradeCost;

    //�������� Ȯ�� ������ �ͼ� ī�� �� 4�� ��ġ 
    public override void InitializeUI()
    {
        base.InitializeUI();

        initUI();
        UpdateShopLevelUI();

        //�̺�Ʈ ����
        GameManager.Instance.OnHPChanged += OnHPChanged;
        GameManager.Instance.OnCostUsed += OnCostUsed;  // �߰�



    }

    public void initUI()
    {

        if (GameManager.Instance != null)
        {
            hpBar.value = GameManager.Instance.PlayerHP / GameManager.Instance.MaxHP;
            targetHPRatio = hpBar.value;
        }

        CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();

        costUI.Initialize(GameManager.Instance.StoreLevel);

        shopLevel = GameManager.Instance.StoreLevel;
        shopLevelText.text = $"Shop Level : {shopLevel}";

        cardDecks = new List<GameObject> { firstCardDeck, secondCardDeck, thirdCardDeck };
        StartCoroutine(CheckAndFillCardDecks());
    }

    // �� ���¸� �ֱ������� üũ�ϰ� ��� ������ UnitCardObject ����
    private IEnumerator CheckAndFillCardDecks()
    {
        isChecking = true;

        while (isChecking)
        {
            foreach (var cardDeck in cardDecks)
            {
                // UnitCard_Empty�� Ȱ��ȭ�� ��� ó��
                var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
                if (emptyPlaceholder != null && emptyPlaceholder.gameObject.activeSelf)
                {
                    // ���� UnitCardObject�� ��� ������ ����
                    if (cardDeck.GetComponentInChildren<UnitTileObject>() == null)
                    {
                        CreateUnitCard(cardDeck);
                    }
                }
            }

            yield return new WaitForSeconds(checkCooldown);
        }
    }

    // UnitCardObject ���� �� ���� �߰�
    private void CreateUnitCard(GameObject cardDeck)
    {
        ResourceManager.Instance.LoadAsync<GameObject>("UnitTileObject", (loadedPrefab) =>
        {
            if (loadedPrefab != null)
            {
                // ������ �ε� ���� �� �ν��Ͻ� ����
                GameObject unitCard = Instantiate(loadedPrefab, cardDeck.transform);
                unitCard.name = "UnitTileObject";

                var cardData = GetCardKeyBasedOnProbability();

                // UnitCardObject �ʱ�ȭ
                UnitTileObject cardObject = unitCard.GetComponent<UnitTileObject>();
                if (cardObject != null)
                {
                    cardObject.InitializeCardInform(cardData); // �⺻ �ʱ�ȭ ������
                }

                // UnitCard_Empty�� ��Ȱ��ȭ
                var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
                if (emptyPlaceholder != null)
                {
                    emptyPlaceholder.gameObject.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("Failed to load UnitCardObject.prefab!");
            }
        });
    }

    public D_TileShpeData GetCardKeyBasedOnProbability()
    {

        //���� ������ ���� Ȯ�� ������ �ε�
        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        // �� ��� Ȯ�� ���
        var normalProb = shopProbabilityData.f_normalGradeChance;  // 100 or 70
        var rareProb = normalProb + shopProbabilityData.f_rareGradeChance;  // 100 or 85
        var epicProb = rareProb + shopProbabilityData.f_epicGradeChance;    // 100 or 90
        var legendaryProb = epicProb + shopProbabilityData.f_legendaryGradeChance; // 100 or 95
        var mythicProb = legendaryProb + shopProbabilityData.f_mythicGradeChance;  // 100 or 100



        //������ ����
        int rand = UnityEngine.Random.Range(0, 100);

        UnitGrade slotGrade;

        //TODO :  Ȯ���� ���� ��� ����
        if (rand <= normalProb)
        {
            slotGrade = UnitGrade.Normal;
        }
        else if (rand <= rareProb)
        {
            slotGrade = UnitGrade.Rare;
        }
        else if (rand <= epicProb)
        {
            slotGrade = UnitGrade.Epic;
        }
        else if (rand <= legendaryProb)
        {
            slotGrade = UnitGrade.Legendary;
        }
        else
        {
            slotGrade = UnitGrade.Mythic;
        }
        var possibleUnitList = D_TileShpeData.FindEntities(data => data.f_grade == slotGrade);


        var selectedUnit = possibleUnitList.OrderBy(_ => Guid.NewGuid()).First();

        
        return selectedUnit; // ���õ� ������ �̸� ��ȯ
       

    }

    //TODO : ���� ��Ÿ��, Ŭ���ӵ� ���� �߰�
    public void RerollCardDecks()
    {
        if(GameManager.Instance.CurrentCost >= 1)
        {

            GameManager.Instance.UseCost(1);
            // ���� ī�� ����
            foreach (var cardDeck in cardDecks)
            {
                // UnitCardObject ����
                var existingCard = cardDeck.GetComponentInChildren<UnitTileObject>();
                if (existingCard != null)
                {
                    Destroy(existingCard.gameObject);
                }

                // UnitCard_Empty Ȱ��ȭ
                var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
                if (emptyPlaceholder != null)
                {
                    emptyPlaceholder.gameObject.SetActive(true);
                }
            }

            // �� ī�� �� ����
            StartCoroutine(CheckAndFillCardDecks());
        }
       
    }

    private void UpdateShopLevelUI()
    {
        // ���� ���� �ؽ�Ʈ ������Ʈ
        shopLevel = GameManager.Instance.StoreLevel;
        shopLevelText.text = $"Shop Level : {shopLevel}";

        // ���׷��̵� ��� ��� �� ǥ��

        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        shopUpgradeCost = shopProbabilityData.f_upgradeCost;

        shopLevelUpCostText.text = $"Upgrade {shopUpgradeCost.ToString()} Cost";

    }

    public void OnShopLevelUpgradeClick()
    {
        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        shopUpgradeCost = shopProbabilityData.f_upgradeCost;

        if (GameManager.Instance.CurrentCost >= shopUpgradeCost)
        {
            if (GameManager.Instance.UseCost(shopUpgradeCost))
            {
                shopLevel++;
                GameManager.Instance.SetStoreLevel(shopLevel);

                UpdateShopLevelUI();
                // CostGauge UI ����
                CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
                costUI.Initialize(shopLevel);


            }
        }
    }

    private void OnCostUsed(int amount)
    {
        UpdateShopLevelUI();  // �ڽ�Ʈ�� ����� ������ ��ư ���� ������Ʈ
    }


    // UnitCardObject ���� �� ȣ���Ͽ� UnitCard_Empty Ȱ��ȭ
    public void OnUnitCardDestroyed(GameObject cardDeck)
    {
        var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
        if (emptyPlaceholder != null)
        {
            emptyPlaceholder.gameObject.SetActive(true);
        }
    }


    private void OnHPChanged(float currentHp)
    {
        targetHPRatio = currentHp / GameManager.Instance.MaxHP;

        if (hpUpdateCoroutine != null)
        {
            StopCoroutine(hpUpdateCoroutine);
        }
        hpUpdateCoroutine = StartCoroutine(UpdateHPBarSmoothly());
    }

    private IEnumerator UpdateHPBarSmoothly()
    {
        while (Mathf.Abs(hpBar.value - targetHPRatio) > 0.01f)
        {
            hpBar.value = Mathf.Lerp(hpBar.value, targetHPRatio, Time.deltaTime * hpUpdateSpeed);
            yield return null;
        }
        hpBar.value = targetHPRatio;

        if(targetHPRatio <= 0)
        {
            GameManager.Instance.ChangeState(new GameResultState(GameStateType.Defeat));

        }
    }


    public override void CloseUI()
    {
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
            GameManager.Instance.OnCostUsed -= OnCostUsed;  // �߰�

        }
    }




    private void OnDestroy()
    {
        isChecking = false;

        //��������
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
            GameManager.Instance.OnCostUsed -= OnCostUsed;  // �߰�
        }
    }


}
