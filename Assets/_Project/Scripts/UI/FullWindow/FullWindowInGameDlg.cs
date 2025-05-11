using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using DG.Tweening;
using static UnityEditor.PlayerSettings;


[UIInfo("FullWindowInGameDlg", "FullWindowInGameDlg", true)]
public class FullWindowInGameDlg : FullWindowBase
{
    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;
    public GameObject forthCardDeck;

    
    public Button currencyTabButton; // ��ȭ �� ��ư
    public Button equipmentTabButton; // ��� �� ��ư
    public Button characterInfoTabButton; //ĳ���� ���� �� ��ư

    //[SerializeField] private GameObject CostGauge;  
    [SerializeField] private TMP_Text shopLevelText;  // ���� ��������
    [SerializeField] private TMP_Text shopLevelUpCostText;  // ���׷��̵� ��� ǥ�� �ؽ�Ʈ
    [SerializeField] private TMP_Text currentCostText;

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

    [SerializeField] private TMP_Text progressWaveIndex; //�������� ���̺� �ε���
    [SerializeField] private TMP_Text placementUnitCount; //��ġ�� ���� ��
    [SerializeField] private TMP_Text invenCount; //�κ��丮 ���� ��

    //ī�嵦
    private List<GameObject> cardDecks;
    private List<UnitCardObject> currentCards;

    public float checkCooldown = 1f;
    private int shopLevel;
    private int shopUpgradeCost;

    [SerializeField] private Button startBattleBtn;
    [SerializeField] private Transform inventorySlotParent; // �κ��丮 ������ ������ �θ� Transform
    [SerializeField] private GameObject slotItemPrefab; // ���� ������
    
    [SerializeField] private GameObject currencyBtnBG;
    [SerializeField] private GameObject currencyPanel;
    [SerializeField] private GameObject currencyBG;

    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject equipmentBG;
    [SerializeField] private GameObject equipmentBtnBG;


    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject characterBG;
    [SerializeField] private GameObject characterBtnBG;

    [SerializeField] private Color unselectedTabColor = new Color(106f / 255f, 106f / 255f, 106f / 255f);

    private Color currencyTabOriginColor;
    private Color equipTabOriginColor;
    private Color characterTabOriginColor;

    [SerializeField] private CharacterInfo characterInfo;

    //�������� Ȯ�� ������ �ͼ� ī�� �� 4�� ��ġ 
    public override void InitializeUI()
    {
        CleanUp();

        base.InitializeUI();

        
        InitializeAssociateUI();

        InitializeCardDecks();
        UpdateShopLevelUI();

        InitializeInventoryUI();

        //�̺�Ʈ ����
        GameManager.Instance.OnCostUsed += OnCostUsed;
        UnitCardObject.OnCardUsed += OnUnitCardDestroyed;
        UnitManager.Instance.OnUnitCountChanged += UpdatePlacementCount;
        StageManager.Instance.OnWaveIndexChanged += UpdateWaveIndex;

        UpdateWaveIndex(1);
        UpdatePlacementCount(UnitManager.Instance.GetAllUnits().Count);

        currencyTabOriginColor = currencyTabButton.GetComponent<Image>().color;
        equipTabOriginColor = equipmentTabButton.GetComponent<Image>().color;

        characterTabOriginColor = characterInfoTabButton.GetComponent<Image>().color;

        
        OnCurrencyTabClicked();

        characterInfo.InitilazeCharacterInfo();

        characterInfo.OnSwitchToCharacterTab += OnCharacterInfoTabClicked;
    }


    //���� ������ �ʱ�ȭ
    private void InitializeAssociateUI()
    {
        //shopLevel �ʱ�ȭ
        shopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel);
        shopLevelText.text = $"Shop Level : {shopLevel}";

        UpdateText();

        GameManager.Instance.OnCostAdd += CostUse;
  
    }

    // InitializeUI �޼��忡 �߰�
    private void InitializeInventoryUI()
    {

        InventoryManager.Instance.LoadInventory();
        // �κ��丮 �Ŵ����� �̺�Ʈ ����
        InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
        InventoryManager.Instance.OnItemCollected += OnItemCollected;
    }


    // ��ȭ �� Ŭ�� �̺�Ʈ
    public void OnCurrencyTabClicked()
    {
        // �г� Ȱ��ȭ/��Ȱ��ȭ
        currencyPanel.SetActive(true);
        currencyBG.SetActive(true);
        currencyBtnBG.SetActive(true);


        if (equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
            equipmentBG.SetActive(false);
            equipmentBtnBG.SetActive(false);
        }

        if (characterPanel != null)
        {
            characterPanel.SetActive(false);
            characterBG.SetActive(false);
            characterBtnBG.SetActive(false);
        }



        if (characterInfo != null)
            characterInfo.HideCharacterInfo();

        if (currencyTabButton.GetComponent<Image>() != null && equipmentTabButton.GetComponent<Image>() != null)
        {
            currencyTabButton.GetComponent<Image>().color =  currencyTabOriginColor;
            equipmentTabButton.GetComponent<Image>().color = unselectedTabColor;
            characterInfoTabButton.GetComponent<Image>().color = unselectedTabColor;
        }

    }

    // ��� �� Ŭ�� �̺�Ʈ ó��
    public void OnEquipmentTabClicked()
    {
        // �г� Ȱ��ȭ/��Ȱ��ȭ

        if (currencyPanel != null)
        {
            currencyPanel.SetActive(false);
            currencyBG.SetActive(false);
            currencyBtnBG.SetActive(false);
        }

        equipmentPanel.SetActive(true);
        equipmentBG.SetActive(true);
        equipmentBtnBG.SetActive(true);


        if (characterPanel != null)
        {
            characterPanel.SetActive(false);
            characterBG.SetActive(false);
            characterBtnBG.SetActive(false);
        }


        if (characterInfo != null)
            characterInfo.HideCharacterInfo();

        if (currencyTabButton.GetComponent<Image>() != null && equipmentTabButton.GetComponent<Image>() != null)
        {
            currencyTabButton.GetComponent<Image>().color = unselectedTabColor;
            equipmentTabButton.GetComponent<Image>().color = equipTabOriginColor;
            characterInfoTabButton.GetComponent<Image>().color = unselectedTabColor;

        }

    }


    // ĳ���� ���� �� Ŭ�� �̺�Ʈ ó��
    public void OnCharacterInfoTabClicked()
    {
        // �г� Ȱ��ȭ/��Ȱ��ȭ
        if(currencyPanel != null)
        {
            currencyPanel.SetActive(false);
            currencyBG.SetActive(false);
            currencyBtnBG.SetActive(false);
        }
       
        if(equipmentPanel != null)
        {
            equipmentPanel.SetActive(false);
            equipmentBG.SetActive(false);
            equipmentBtnBG.SetActive(false);
        }

        characterPanel.SetActive(true);
        characterBG.SetActive(true);
        characterBtnBG.SetActive(true);


        if (currencyTabButton.GetComponent<Image>() != null && equipmentTabButton.GetComponent<Image>() != null)
        {
            currencyTabButton.GetComponent<Image>().color = unselectedTabColor;
            equipmentTabButton.GetComponent<Image>().color = unselectedTabColor;
            characterInfoTabButton.GetComponent<Image>().color = characterTabOriginColor;

        }

    }

    // �κ��丮 UI ����
    private void RefreshInventoryUI()
    {
        if (InventoryManager.Instance == null || inventorySlotParent == null || slotItemPrefab == null)
        {
            return;
        }


        InventoryManager.Instance.RefreshInventoryUI(inventorySlotParent, slotItemPrefab);
        
    }

    // ������ ���� �̺�Ʈ ó��
    private void OnItemCollected(D_ItemData item)
    {
        //RefreshInventoryUI();
    }


    public void UpdateText()
    {
        int currentCost = GameManager.Instance.GetSystemStat(StatName.Cost);
        currentCostText.text = currentCost.ToString();  
    }


    private void CostUse()
    {
        UpdateText();
    }

    private void InitializeCardDecks()
    {
        cardDecks = new List<GameObject> { firstCardDeck, secondCardDeck, thirdCardDeck, forthCardDeck };

      
        currentCards = new List<UnitCardObject>();

        foreach (var deck in cardDecks)
        {
            currentCards.Add(null);
        }

        CheckAndFillCardDecks();
        //StartCoroutine(CheckAndFillCardDecks());
    }

    private void UpdateWaveIndex(int currentIndex, int subIndex = 0)
    {

        progressWaveIndex.text = currentIndex.ToString();
    }
   
    private void UpdatePlacementCount(int count)
    {
        placementUnitCount.text = $"{ count} / {GameManager.Instance.GetSystemStat(StatName.UnitPlacementCount)}";
    }


    private void CheckAndFillCardDecks()
    {
        //var cardDatas = new List<D_TileCardData>();

        //// ī�� ������ �̸� �غ�
        //for (int i = 0; i < cardDecks.Count; i++)
        //{
        //    if (emptyCardObjects[i].activeSelf && currentCards[i] == null)
        //    {
        //        cardDatas.Add(GetCardKeyBasedOnProbability());
        //    }
        //    else
        //    {
        //        cardDatas.Add(null);
        //    }
        //}

        //// �� ī�忡 ���� 0.1�� �������� �ڷ�ƾ ����
        //for (int i = 0; i < cardDecks.Count; i++)
        //{
        //    if (cardDatas[i] != null)
        //    {
        //        emptyCardObjects[i].SetActive(false);
        //        StartCoroutine(PlayEffectAndSpawnCard(i, cardDatas[i]));
        //        yield return new WaitForSeconds(0.06f); // ���� ī�� ����Ʈ ����
        //    }
        //}

        //TODO : ä��
        //RT ������ �����鼭 ����� �κ�, ���� ����ī�� ��� ����Ʈ�ִ°� ���� ī�� �� ����

        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (currentCards[i] == null)
            {
                D_TileCardData cardData = GetCardKeyBasedOnProbability();
                CreateUnitCardWithData(cardDecks[i], i, cardData);
            }
        }

    }
    private IEnumerator PlayEffectAndSpawnCard(int index, D_TileCardData cardData)
    {
        var grade = cardData.f_grade;

        // ��� ���� ����
        Color gradeColor = GetGradeColor(grade);
        //cardGradeImages[index].color = gradeColor;

        // ����Ʈ ����
        //PlayCardEnterEffect(cardGradeImages[index]);

        // ����Ʈ ���� �ð���ŭ ���
        yield return new WaitForSeconds(0.24f);

        // ī�� ����
        CreateUnitCardWithData(cardDecks[index], index, cardData);
    }

    private void PlayCardEnterEffect(Image unitCard)
    {
        if (unitCard == null) return;

        //Ʈ����Ʈ, zoom
        if (unitCard.material != null && unitCard.material.HasProperty("_TwistUvAmount") && unitCard.material.HasProperty("_ZoomUvAmount"))
        {
            if (!unitCard.material.name.EndsWith("(Instance)"))
            {
                unitCard.material = Instantiate(unitCard.material);
            }

            Material mat = unitCard.material;

            DOTween.To(() => 4f, v => mat.SetFloat("_ZoomUvAmount", v), 1f, 0.16f);

            DOTween.To(() => 1f, v => mat.SetFloat("_TwistUvAmount", v), 0f, 0.16f);
        }
    }
    // ����Ʈ�� �ִٸ�..
    private void PlayCardEffect(Transform parent, UnitGrade grade)
    {
        string effectName = grade switch
        {
            UnitGrade.Normal => "Effect_Normal",
            UnitGrade.Rare => "Effect_Rare",
            UnitGrade.Epic => "Effect_Epic",
            UnitGrade.Legendary => "Effect_Legendary",
            UnitGrade.Mythic => "Effect_Mythic",
            _ => "Effect_Default"
        };

        var effect = PoolingManager.Instance.GetObject(effectName);
        effect.transform.SetParent(parent);
        effect.transform.localPosition = Vector3.zero;
    }
    private void CreateUnitCardWithData(GameObject cardDeck, int deckIndex, D_TileCardData cardData)
    {
        GameObject unitCard = PoolingManager.Instance.GetObject("UnitCardObject");

        if (unitCard != null)
        {
            unitCard.transform.SetParent(cardDeck.transform);
            unitCard.transform.localPosition = Vector3.zero;

            UnitCardObject cardObject = unitCard.GetComponent<UnitCardObject>();

            if (cardObject != null)
            {
                cardObject.InitializeCardInform(cardData);
                currentCards[deckIndex] = cardObject;

                var dragHandler = cardDeck.GetComponent<CardDeckDragHandler>();
                if (dragHandler != null)
                    dragHandler.SetUnitCard(cardObject);
            }
        }
    }

    private Color GetGradeColor(UnitGrade grade)
    {
        return grade switch
        {
            UnitGrade.Normal => normalColorGradient.topLeft,
            UnitGrade.Rare => rareColorGradient.topLeft,
            UnitGrade.Epic => epicColorGradient.topLeft,
            UnitGrade.Legendary => legendaryColorGradient.topLeft,
            UnitGrade.Mythic => mythicColorGradient.topLeft,
            _ => Color.white
        };
    }
    // UnitCardObject ���� �� ���� �߰�
    /*private void CreateUnitCard(GameObject cardDeck, int deckIndex)
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

                // CardDeckDragHandler�� ���ο� UnitCardObject ���� ����
                var dragHandler = cardDeck.GetComponent<CardDeckDragHandler>();
                if (dragHandler != null)
                {
                    dragHandler.SetUnitCard(cardObject);
                }

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
    }*/

    public D_TileCardData GetCardKeyBasedOnProbability()
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
        var possibleUnitList = D_TileCardData.FindEntities(data => data.f_grade == slotGrade);


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

            //cardGradeImages[i].color = Color.white;

            // UnitCard_Empty Ȱ��ȭ
           // emptyCardObjects[i].SetActive(true);
        }
            // �� ī�� �� ����
            CheckAndFillCardDecks();
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
            //CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
            //costUI.Initialize(newShopLevel);
        }
    }

    private void OnCostUsed(int amount)
    {
        CostUse();
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
            //emptyCardObjects[deckIndex].SetActive(true);
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
        //isChecking = false;
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

        // �̺�Ʈ ���� ����
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed;
            UnitManager.Instance.OnUnitCountChanged -= UpdatePlacementCount;
            StageManager.Instance.OnWaveIndexChanged -= UpdateWaveIndex;
            GameManager.Instance.OnCostAdd -= CostUse;

            // �� ��ư �̺�Ʈ ������ ����
            if (currencyTabButton != null)
                currencyTabButton.onClick.RemoveListener(OnCurrencyTabClicked);

            if (equipmentTabButton != null)
                equipmentTabButton.onClick.RemoveListener(OnEquipmentTabClicked);

            if(characterInfo != null)
            {

                characterInfo.OnSwitchToCharacterTab -= OnCharacterInfoTabClicked;
                characterInfo.ClearCharacterInfo();
            }

        }
    }

    public void OnClickSettingBtn()
    {
        //�Ͻ����� ���·� ��ȯ
        GameManager.Instance.ChangeState(new GamePauseState(GameManager.Instance.currentState));

    }

    public void OnClickStartBattleBtn()
    {
        // ��� ������ UnitMoveToTargetState�� ����
        List<UnitController> units = UnitManager.Instance.GetAllUnits();

        if (units.Count <= 0)
        {
            Debug.LogWarning("No unit controllers found!");
            return;
        }

        foreach (var unit in units)
        {
            // ������ �����ϰ� ���� ������ �������� Ȯ��
            if (unit != null && unit.isActive)
            {
                // UnitMoveToTargetState�� ���� ��ȯ
                unit.fsmObj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.MoveRequested);
                unit.SaveOriginalUnitPos();
                if(unit.itemSlotObject != null)
                {
                    unit.itemSlotObject.gameObject.SetActive(false);
                }
                
            }
        }

        CameraController cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.OnBattleStart();
        }

        List<Enemy> enemies = EnemyManager.Instance.GetAllEnemys();
        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                enemy.fsmObj.isEnemy = true;
                enemy.fsmObj.stateMachine.RegisterTrigger(Kylin.FSM.Trigger.MoveRequested);
            }
        }

    }


}
