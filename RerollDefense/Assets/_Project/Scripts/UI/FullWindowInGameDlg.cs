using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;

public class FullWindowInGameDlg : UIBase
{

    //TODO : UIBase ��ӹ޾ƾߵ�

    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;
    public GameObject fourthCardDeck;

    [SerializeField] private Slider hpBar;
    [SerializeField] private float hpUpdateSpeed = 2f;  // HP Bar ���� �ӵ�
    [SerializeField] private GameObject CostGauge;  // HP Bar ���� �ӵ�
    [SerializeField] private TMP_Text shopLevelText;  // HP Bar ���� �ӵ�
    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;

    private List<GameObject> cardDecks;
    private bool isChecking = false;

    //test�� ������
    public float checkCooldown = 3f;
    private int shopLevel = 1;

    //�������� Ȯ�� ������ �ͼ� ī�� �� 4�� ��ġ 
    public override void InitializeUI()
    {
        base.InitializeUI();


    }

    void Start()
    {
        //test��
        initUI();
    }

    //TODO : UIManager ����� InitializeUI�� �־���� �ڵ�
    public void initUI()
    {

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHPChanged += OnHPChanged;
            hpBar.value = GameManager.Instance.PlayerHP / GameManager.Instance.MaxHP;
            targetHPRatio = hpBar.value;
        }

        CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();

        costUI.Initialize(GameManager.Instance.StoreLevel);

        int shopLevel = GameManager.Instance.StoreLevel;
        shopLevelText.text = $"Shop Level : {shopLevel}";

        cardDecks = new List<GameObject> { firstCardDeck, secondCardDeck, thirdCardDeck, fourthCardDeck };
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
                    if (cardDeck.GetComponentInChildren<UnitCardObject>() == null)
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
        ResourceManager.Instance.LoadAsync<GameObject>("UnitCardObject", (loadedPrefab) =>
        {
            if (loadedPrefab != null)
            {
                // ������ �ε� ���� �� �ν��Ͻ� ����
                GameObject unitCard = Instantiate(loadedPrefab, cardDeck.transform);
                unitCard.name = "UnitCardObject";

                var cardData = GetCardKeyBasedOnProbability();

                // UnitCardObject �ʱ�ȭ
                UnitCardObject cardObject = unitCard.GetComponent<UnitCardObject>();
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

        //�� ��� Ȯ��
        var normalShopProbability = shopProbabilityData.f_normalGradeChance;
        var rareShopProbability = shopProbabilityData.f_rareGradeChance;
        var epicProbability = shopProbabilityData.f_epicGradeChance;
        var legendaryShopProbability = shopProbabilityData.f_legendaryGradeChance;
        var mythicShopProbability = shopProbabilityData.f_mythicGradeChance;

        //������ ����
        int rand = UnityEngine.Random.Range(0, 100);

        UnitGrade slotGrade;

        //TODO :  Ȯ���� ���� ��� ����
        if (rand <= normalShopProbability)
        {
            slotGrade = UnitGrade.Normal;
        }
        else if (rand <= rareShopProbability)
        {
            slotGrade = UnitGrade.Rare;
        }
        else if (rand <= epicProbability)
        {
            slotGrade = UnitGrade.Epic;
        }
        else if (rand <= legendaryShopProbability)
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
        // ���� ī�� ����
        foreach (var cardDeck in cardDecks)
        {
            // UnitCardObject ����
            var existingCard = cardDeck.GetComponentInChildren<UnitCardObject>();
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
    }



    private void OnDestroy()
    {
        isChecking = false;

        //��������
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
        }
    }


}
