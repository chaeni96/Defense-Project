using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;


[UIInfo("FullWindowInGameDlg", "FullWindowInGameDlg", true)]
public class FullWindowInGameDlg : FullWindowBase
{
    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;


    [SerializeField] private GameObject CostGauge;  
    [SerializeField] private TMP_Text shopLevelText;  // ���� ��������
    [SerializeField] private TMP_Text shopLevelUpCostText;  // ���׷��̵� ��� ǥ�� �ؽ�Ʈ

    [SerializeField] private TMP_ColorGradient normalColorGradient;
    [SerializeField] private TMP_ColorGradient rareColorGradient;
    [SerializeField] private TMP_ColorGradient epicColorGradient;
    [SerializeField] private TMP_ColorGradient legendaryColorGradient;
    [SerializeField] private TMP_ColorGradient mythicColorGradient;


    [SerializeField] private TMP_Text normalPercent;
    [SerializeField] private TMP_Text rarePercent;
    [SerializeField] private TMP_Text epicPercent;
    [SerializeField] private TMP_Text legendaryPercent;
    [SerializeField] private TMP_Text mythicPercent;

    [SerializeField] private List<Image> cardGradeImages;  // Inspector���� firstCardGrade, secondCardGrade, thirdCardGrade ������� �Ҵ�
    [SerializeField] private TMP_Text remainEnemyCount; //���� enemy ��

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
        CleanUp();

        base.InitializeUI();

        
        InitializeAssociateUI();

        InitializeCardDecks();
        UpdateShopLevelUI();

        //�̺�Ʈ ����
        GameManager.Instance.OnCostUsed += OnCostUsed;  
        UnitCardObject.OnCardUsed += OnUnitCardDestroyed;
        StageManager.Instance.OnEnemyCountChanged += UpdateRemainEnemyCount;


    }


    //���� ������ �ʱ�ȭ
    private void InitializeAssociateUI()
    {

        //cost �ʱ�ȭ
        CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
        costUI.Initialize(GameManager.Instance.GetSystemStat(StatName.StoreLevel));

        //shopLevel �ʱ�ȭ
        shopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel);
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

    private void UpdateRemainEnemyCount(int count)
    {
        remainEnemyCount.text = count.ToString();
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
                // ī�� ������ ��������
                var cardData = GetCardKeyBasedOnProbability();
                cardObject.InitializeCardInform(cardData);
                currentCards[deckIndex] = cardObject;
                // UnitCard_Empty�� ��Ȱ��ȭ
                emptyCardObjects[deckIndex].SetActive(false);


                // ī�� ��޿� ���� ���� ����
                Color gradeColor;
                switch (cardData.f_grade)
                {
                    case UnitGrade.Normal:
                        gradeColor = normalColorGradient.topLeft;
                        break;
                    case UnitGrade.Rare:
                        gradeColor = rareColorGradient.topLeft;
                        break;
                    case UnitGrade.Epic:
                        gradeColor = epicColorGradient.topLeft;
                        break;
                    case UnitGrade.Legendary:
                        gradeColor = legendaryColorGradient.topLeft;
                        break;
                    case UnitGrade.Mythic:
                        gradeColor = mythicColorGradient.topLeft;
                        break;
                    default:
                        gradeColor = Color.white;
                        break;
                }
                cardGradeImages[deckIndex].color = gradeColor;
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
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < 1) return;


        // �ڽ�Ʈ �Ҹ� ó�� �߰�
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = -1, // ���� ��� 1 �Ҹ�
            multiply = 1f
        });

        // ���� ī�� ����
        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (currentCards[i] != null)
            {
                // UnitCardObject ����
                Destroy(currentCards[i].gameObject);
                currentCards[i] = null;
            }

            cardGradeImages[i].color = Color.white;

            // UnitCard_Empty Ȱ��ȭ
            emptyCardObjects[i].SetActive(true);
        }
            // �� ī�� �� ����
            StartCoroutine(CheckAndFillCardDecks());
    }
       

    private void UpdateShopLevelUI()
    {
        // ���� ���� �ؽ�Ʈ ������Ʈ
        shopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel);
        shopLevelText.text = $"Shop Level : {shopLevel}";

        // ���׷��̵� ��� ��� �� ǥ��

        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        shopUpgradeCost = shopProbabilityData.f_upgradeCost;

        shopLevelUpCostText.text = shopUpgradeCost.ToString();

        // Ȯ�� �ؽ�Ʈ ������Ʈ �߰�
        normalPercent.text = $"{shopProbabilityData.f_normalGradeChance}%";
        rarePercent.text = $"{shopProbabilityData.f_rareGradeChance}%";
        epicPercent.text = $"{shopProbabilityData.f_epicGradeChance}%";
        legendaryPercent.text = $"{shopProbabilityData.f_legendaryGradeChance}%";
        mythicPercent.text = $"{shopProbabilityData.f_mythicGradeChance}%";
    }

    public void OnShopLevelUpgradeClick()
    {
        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        shopUpgradeCost = shopProbabilityData.f_upgradeCost;

        if (GameManager.Instance.GetSystemStat(StatName.Cost) >= shopUpgradeCost)
        {
            // ��� �Ҹ� ó��
            StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
            {
                statName = StatName.Cost,
                value = -shopUpgradeCost, // ��� �Ҹ�� ������ ����
                multiply = 1f
            });

            // ���� ���� ���� ó��
            var newShopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel) + 1;
            StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
            {
                statName = StatName.StoreLevel,
                value = newShopLevel, // ���ο� ���� ���� ����
                multiply = 1f
            });

            // UI ������Ʈ
            UpdateShopLevelUI();

            // CostGauge UI ����
            CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
            costUI.Initialize(newShopLevel);
        }
    }

    private void OnCostUsed(int amount)
    {
        UpdateShopLevelUI();  // �ڽ�Ʈ�� ����� ������ ��ư ���� ������Ʈ


        foreach(var card in currentCards)
        {
            if(card != null)
            {
                card.UpdateCostTextColor();
            }
        }
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


    public override void HideUI()
    {
        //TODO: hide�ִϸ��̼� �߰������� �ּ� ����
        //base.HideUI();

        CleanUp();

    }




    private void CleanUp()
    {
        // ī�� ���� ����
        isChecking = false;
        if (currentCards != null)
        {
            foreach (var card in currentCards.Where(c => c != null))
            {
                Destroy(card.gameObject);
            }
            currentCards.Clear();
        }

        // ����Ʈ ����
        cardDecks?.Clear();
        emptyCardObjects?.Clear();

        // �̺�Ʈ ���� ����
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed;
            StageManager.Instance.OnEnemyCountChanged -= UpdateRemainEnemyCount;

        }
    }

    public void OnClickSettingBtn()
    {
        //�Ͻ����� ���·� ��ȯ
        GameManager.Instance.ChangeState(new GamePauseState(GameManager.Instance.currentState));

    }
}
