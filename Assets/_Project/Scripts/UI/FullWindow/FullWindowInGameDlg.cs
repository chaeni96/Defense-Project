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

    [SerializeField] private Button rerollBtn;
    [SerializeField] private Button shopUpgradeBtn;
    [SerializeField] private Button unEquipBtn;
    [SerializeField] private Button saleUnitBtn;


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


    [SerializeField] private GameObject startBattleBtn;
    [SerializeField] private Transform inventorySlotParent; // �κ��丮 ������ ������ �θ� Transform
    [SerializeField] private GameObject slotItemPrefab; // ���� ������
    
    [SerializeField] private Color unselectedTabColor = new Color(106f / 255f, 106f / 255f, 106f / 255f);
    [SerializeField] private GameObject currencyBtnBG;
    [SerializeField] private GameObject currencyPanel;
    [SerializeField] private GameObject currencyBG;

    [SerializeField] private GameObject equipmentPanel;
    [SerializeField] private GameObject equipmentBG;
    [SerializeField] private GameObject equipmentBtnBG;


    [SerializeField] private GameObject characterPanel;
    [SerializeField] private GameObject characterBG;
    [SerializeField] private GameObject characterBtnBG;

    [SerializeField] private CharacterInfo characterInfo;

    //ī�嵦
    private List<GameObject> cardDecks;
    private List<UnitCardObject> currentCards;

    private int shopLevel;
    private int shopUpgradeCost;

    private Color currencyTabOriginColor;
    private Color equipTabOriginColor;
    private Color characterTabOriginColor;

    //���̺� ���� ui ���� ������
    [SerializeField] private Transform progressInfoLayout; // HorizontalLayoutGroup�� ���� ������Ʈ
    [SerializeField] private GameObject gameProgressPrefab; // ������ ����
    private List<GameProgressObject> progressObjects = new List<GameProgressObject>();
    private int maxProgressObjects = 6; // �� ���� ǥ���� �ִ� ��ü ��
    private int currentGroupStartIndex = 0; // ���� ǥ�� ���� ���̺� �׷��� ���� �ε���
    private bool uiInteractionEnabled = true;


    //�������� Ȯ�� ������ �ͼ� ī�� �� 4�� ��ġ 
    public override void InitializeUI()
    {
        CleanUp();

        base.InitializeUI();

        currencyTabOriginColor = currencyTabButton.GetComponent<Image>().color;
        equipTabOriginColor = equipmentTabButton.GetComponent<Image>().color;
        characterTabOriginColor = characterInfoTabButton.GetComponent<Image>().color;

        InitializeAssociateUI();
        InitializeCardDecks();
        UpdateShopLevelUI();
        InitializeInventoryUI();
        UpdatePlacementCount(UnitManager.Instance.GetAllUnits().Count);
        OnCurrencyTabClicked();

        characterInfo.InitilazeCharacterInfo();

        InitializeWaveProgressUI();
        if (startBattleBtn != null)
        {
            startBattleBtn.gameObject.SetActive(false);
        }

        //�̺�Ʈ ����
        GameManager.Instance.OnCostUsed += OnCostUsed;
        UnitCardObject.OnCardUsed += OnUnitCardDestroyed;
        UnitManager.Instance.OnUnitCountChanged += UpdatePlacementCount;
        StageManager.Instance.OnWaveIndexChanged += OnWaveIndexChanged;
        StageManager.Instance.OnBattleReady += UpdateBattleButtonState;
        StageManager.Instance.OnBattleStateChanged += OnBattleStateChanged;

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
        // �κ��丮 �Ŵ����� �̺�Ʈ ����
        InventoryManager.Instance.OnInventoryChanged += RefreshInventoryUI;
        InventoryManager.Instance.OnItemCountUpdate += OnUpdateSlotCount;

        InventoryManager.Instance.LoadInventory();
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

    private void OnBattleStateChanged(bool isBattleActive)
    {
        // ���� �߿��� UI ��ȣ�ۿ� ��Ȱ��ȭ
        uiInteractionEnabled = !isBattleActive;

        // UI ��ư ��ȣ�ۿ� ����
        SetUIInteraction(uiInteractionEnabled);
    }

    private void SetUIInteraction(bool enabled)
    {
        // �� ��ư ��ȣ�ۿ� ����
        if (currencyTabButton != null)
            currencyTabButton.interactable = enabled;

        if (equipmentTabButton != null)
            equipmentTabButton.interactable = enabled;

        if (characterInfoTabButton != null)
            characterInfoTabButton.interactable = enabled;

        if (rerollBtn != null)
            rerollBtn.interactable = enabled;

        if (shopUpgradeBtn != null)
            shopUpgradeBtn.interactable = enabled;

        if (unEquipBtn != null)
            unEquipBtn.interactable = enabled;

        if (saleUnitBtn != null)
            saleUnitBtn.interactable = enabled;

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

    // ������ ���� ������Ʈ
    private void OnUpdateSlotCount(int count)
    {
        // �κ��丮 ���� �� ������Ʈ

        invenCount.text = $"{count} / {GameManager.Instance.GetSystemStat(StatName.InventoryCount)}";

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
                currentCards[i].CleanUp();
                Destroy(currentCards[i].gameObject);
                currentCards[i] = null;
            }
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
        int nextShopLevel = shopLevel + 1;
        var nextLevelData = D_UnitShopChanceData.GetEntityByKeyShopLevel(nextShopLevel);

        // ���� ���� �����Ͱ� ������ �ִ� ������ ������ ��
        if (nextLevelData == null)
        {
            Debug.Log("�̹� �ִ� ���� ������ �����߽��ϴ�.");
            return;
        }

        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        // shopProbabilityData�� null���� Ȯ��
        if (shopProbabilityData == null)
        {
            Debug.LogError($"���� ���� {shopLevel}�� ���� �����͸� ã�� �� �����ϴ�.");
            return;
        }

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


    // ���̺� ���α׷��� UI �ʱ�ȭ
    private void InitializeWaveProgressUI()
    {
        // ���α׷��� ������Ʈ ����
        CreateProgressObjects();

        // StageManager���� ���̺� ��� ��������
        List<WaveBase> allWaves = StageManager.Instance.GetWaveList();
        if (allWaves != null && allWaves.Count > 0)
        {
            // �ʱ� ���̺� ����
            int currentWaveIndex = StageManager.Instance.currentWaveIndex;
            currentGroupStartIndex = (currentWaveIndex / maxProgressObjects) * maxProgressObjects;

            // UI ������Ʈ
            UpdateProgressUI(allWaves, currentWaveIndex);
        }
    }

    // ���α׷��� ������Ʈ ����
    private void CreateProgressObjects()
    {
        // ���� ��ü ����
        foreach (var obj in progressObjects)
        {
            if (obj != null)
                Destroy(obj.gameObject);
        }
        progressObjects.Clear();

        // �� ��ü ����
        for (int i = 0; i < maxProgressObjects; i++)
        {
            GameObject newObj = Instantiate(gameProgressPrefab, progressInfoLayout);
            GameProgressObject progressObj = newObj.GetComponent<GameProgressObject>();
            progressObjects.Add(progressObj);

            // �ʱ⿡�� ��Ȱ��ȭ
            newObj.SetActive(false);
        }
    }

    private void OnWaveIndexChanged(int newIndex, int totalWaves)
    {
        // ���� �ؽ�Ʈ ������Ʈ
        progressWaveIndex.text = newIndex.ToString();

        // ���̺� ���α׷��� UI ������Ʈ
        List<WaveBase> allWaves = StageManager.Instance.GetWaveList();
        if (allWaves != null && allWaves.Count > 0)
        {
            // �ε����� 0���� ���������� UI ǥ�ô� 1���� ����
            int waveIndex = newIndex - 1;

            // �׷� ���� �ε��� ���
            int newGroupStartIndex = (waveIndex / maxProgressObjects) * maxProgressObjects;

            // �׷��� ����Ǿ����� UI ��ü ������Ʈ
            if (newGroupStartIndex != currentGroupStartIndex)
            {
                currentGroupStartIndex = newGroupStartIndex;
                UpdateProgressUI(allWaves, waveIndex);
            }
            else
            {
                // ���� �׷� �������� Ȱ��ȭ ���¸� ������Ʈ
                UpdateActiveState(allWaves, waveIndex);
            }
        }
    }

    // ���α׷��� UI ��ü ������Ʈ
    private void UpdateProgressUI(List<WaveBase> allWaves, int currentWaveIndex)
    {
        for (int i = 0; i < maxProgressObjects; i++)
        {
            int waveIndex = currentGroupStartIndex + i;

            // ���̺� �ε����� ��ȿ���� Ȯ��
            if (waveIndex < allWaves.Count)
            {
                // ������ �� Ȱ��ȭ ���� ����
                progressObjects[i].gameObject.SetActive(true);
                progressObjects[i].SetIcon(allWaves[waveIndex]);
                progressObjects[i].SetActive(waveIndex == currentWaveIndex);
            }
            else
            {
                // ���� ������ ��Ȱ��ȭ
                progressObjects[i].gameObject.SetActive(false);
            }
        }
    }

    // Ȱ��ȭ ���¸� ������Ʈ (���� �׷� �� ���̺� ���� ��)
    private void UpdateActiveState(List<WaveBase> allWaves, int currentWaveIndex)
    {
        for (int i = 0; i < maxProgressObjects; i++)
        {
            int waveIndex = currentGroupStartIndex + i;
            if (waveIndex < allWaves.Count)
                progressObjects[i].SetActive(waveIndex == currentWaveIndex);
        }
    }
    private void UpdateBattleButtonState(bool isBattleWave)
    {
        if (startBattleBtn != null)
        {
            startBattleBtn.gameObject.SetActive(isBattleWave);
        }
    }

    public override void HideUI()
    {
        CleanUp();

        base.HideUI();
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

        // ���̺� ���α׷��� ������Ʈ ����
        foreach (var obj in progressObjects)
        {
            if (obj != null)
                Destroy(obj.gameObject);
        }
        progressObjects.Clear();

        // �̺�Ʈ ���� ����
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed;
            UnitManager.Instance.OnUnitCountChanged -= UpdatePlacementCount;
            StageManager.Instance.OnWaveIndexChanged -= OnWaveIndexChanged;
            StageManager.Instance.OnBattleReady -= UpdateBattleButtonState;
            StageManager.Instance.OnBattleStateChanged -= OnBattleStateChanged;

            GameManager.Instance.OnCostAdd -= CostUse;
            InventoryManager.Instance.OnItemCountUpdate -= OnUpdateSlotCount;
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
            if (startBattleBtn != null)
            {
                startBattleBtn.gameObject.SetActive(false);
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
        // ���� ���� ���̺갡 �ƴϸ� ����
        if (!StageManager.Instance.IsBattleWave())
        {
            return;
        }


        if (startBattleBtn != null)
        {
            startBattleBtn.gameObject.SetActive(false);
        }

        // ���� ���� ���·� ����
        StageManager.Instance.StartBattle();

        // ��� ������ UnitMoveToTargetState�� ����
        List<UnitController> units = UnitManager.Instance.GetAllUnits();

        if (units.Count <= 0)
        {
            Debug.LogWarning("No unit controllers found!");
            return;
        }

        // ���� ������ ���� �� Ȯ��
        int attackableUnitsCount = 0;

        foreach (var unit in units)
        {
            if (unit != null && unit.isActive && unit.canAttack)
            {
                attackableUnitsCount++;
            }
        }

        // ���� ������ ������ ������ �޽��� ǥ���ϰ� ����
        if (attackableUnitsCount <= 0)
        {
            Debug.LogWarning("���� ������ ������ �����ϴ�!");
            return;
        }

        foreach (var unit in units)
        {
            // ������ �����ϰ� ���� ������ �������� Ȯ��
            if (unit != null && unit.isActive && unit.canAttack)
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
