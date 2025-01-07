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

    //ī�嵦
    private List<GameObject> cardDecks;
    private List<GameObject> emptyCardObjects;
    private List<UnitCardObject> currentCards;

    private bool isChecking = false;

    //test�� ������
    public float checkCooldown = 1f;
    private int shopLevel;
    private int shopUpgradeCost;

    //�������� Ȯ�� ������ �ͼ� ī�� �� 4�� ��ġ 
    public override void InitializeUI()
    {
        base.InitializeUI();

        
        InitializeAssociateUI();

        InitializeCardDecks();
        UpdateShopLevelUI();

        //�̺�Ʈ ����
        GameManager.Instance.OnHPChanged += OnHPChanged;
        GameManager.Instance.OnCostUsed += OnCostUsed;  
        UnitCardObject.OnCardUsed += OnUnitCardDestroyed; 


    }

    //���� ������ �ʱ�ȭ
    private void InitializeAssociateUI()
    {
        //hp �ʱ�ȭ
        if (GameManager.Instance != null)
        {
            hpBar.value = GameManager.Instance.PlayerHP / GameManager.Instance.MaxHP;
            targetHPRatio = hpBar.value;
        }

        //cost �ʱ�ȭ
        CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
        costUI.Initialize(GameManager.Instance.StoreLevel);

        //shopLevel �ʱ�ȭ
        shopLevel = GameManager.Instance.StoreLevel;
        shopLevelText.text = $"Shop Level : {shopLevel}";
    }


    private void InitializeCardDecks()
    {
        cardDecks = new List<GameObject> { firstCardDeck, secondCardDeck, thirdCardDeck };

        // Empty ������Ʈ�� ���� ī�� �迭 �ʱ�ȭ
        emptyCardObjects = new List<GameObject>();
        currentCards = new List<UnitCardObject>();

        foreach (var deck in cardDecks)
        {
            emptyCardObjects.Add(deck.transform.GetChild(1).gameObject);
            currentCards.Add(null);
        }

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
                for (int i = 0; i < cardDecks.Count; i++)
                {
                    if (emptyCardObjects[i].activeSelf && currentCards[i] == null)
                    {
                        // ���� UnitCardObject�� ��� ������ ����

                        CreateUnitCard(cardDecks[i], i);
                    }
                }
                yield return new WaitForSeconds(checkCooldown);
            }
        }
    }

    // UnitCardObject ���� �� ���� �߰�
    private void CreateUnitCard(GameObject cardDeck, int deckIndex)
    {

        GameObject unitCard = PoolingManager.Instance.GetObject("UnitCardObject");

        if(unitCard != null)
        {
            //cardDeck �θ� ���� �� ���� ������ �ʱ�ȭ
            unitCard.transform.SetParent(cardDeck.transform);
            unitCard.transform.localPosition = Vector3.zero;

            // UnitCardObject �ʱ�ȭ
            UnitCardObject cardObject = unitCard.GetComponent<UnitCardObject>();
            if (cardObject != null)
            {
                cardObject.InitializeCardInform(GetCardKeyBasedOnProbability());
                currentCards[deckIndex] = cardObject;
                // UnitCard_Empty�� ��Ȱ��ȭ
                emptyCardObjects[deckIndex].SetActive(false);
            }
        }
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

        //�ڽ�Ʈ 1���� ������ �Ұ�
        if (!GameManager.Instance.UseCost(1)) return;

        // ���� ī�� ����
        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (currentCards[i] != null)
            {
                // UnitCardObject ����
                Destroy(currentCards[i].gameObject);
                currentCards[i] = null;
            }

            // UnitCard_Empty Ȱ��ȭ
            emptyCardObjects[i].SetActive(true);
        }
            // �� ī�� �� ����
            StartCoroutine(CheckAndFillCardDecks());
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
        int deckIndex = cardDecks.IndexOf(cardDeck);
        if (deckIndex != -1)
        {
            currentCards[deckIndex] = null;
            emptyCardObjects[deckIndex].SetActive(true);
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
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed; 

        }
    }




    private void OnDestroy()
    {
        isChecking = false;

        //��������
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed;
        }
    }


}
