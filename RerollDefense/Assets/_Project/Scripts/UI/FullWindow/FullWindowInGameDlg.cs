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

    //TODO : UIBase 상속받아야됨

    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;

    [SerializeField] private Slider hpBar;
    [SerializeField] private float hpUpdateSpeed = 2f;  // HP Bar 감소 속도
    [SerializeField] private GameObject CostGauge;  // HP Bar 감소 속도
    [SerializeField] private TMP_Text shopLevelText;  // 현재 상점레벨
    [SerializeField] private TMP_Text shopLevelUpCostText;  // 업그레이드 비용 표시 텍스트

    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;

    private List<GameObject> cardDecks;
    private bool isChecking = false;

    //test용 변수들
    public float checkCooldown = 3f;
    private int shopLevel;
    private int shopUpgradeCost;

    //상점에서 확률 가지고 와서 카드 덱 4개 설치 
    public override void InitializeUI()
    {
        base.InitializeUI();

        initUI();
        UpdateShopLevelUI();

        //이벤트 구독
        GameManager.Instance.OnHPChanged += OnHPChanged;
        GameManager.Instance.OnCostUsed += OnCostUsed;  // 추가



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

    // 덱 상태를 주기적으로 체크하고 비어 있으면 UnitCardObject 생성
    private IEnumerator CheckAndFillCardDecks()
    {
        isChecking = true;

        while (isChecking)
        {
            foreach (var cardDeck in cardDecks)
            {
                // UnitCard_Empty가 활성화된 경우 처리
                var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
                if (emptyPlaceholder != null && emptyPlaceholder.gameObject.activeSelf)
                {
                    // 덱에 UnitCardObject가 비어 있으면 생성
                    if (cardDeck.GetComponentInChildren<UnitTileObject>() == null)
                    {
                        CreateUnitCard(cardDeck);
                    }
                }
            }

            yield return new WaitForSeconds(checkCooldown);
        }
    }

    // UnitCardObject 생성 및 덱에 추가
    private void CreateUnitCard(GameObject cardDeck)
    {
        ResourceManager.Instance.LoadAsync<GameObject>("UnitTileObject", (loadedPrefab) =>
        {
            if (loadedPrefab != null)
            {
                // 프리팹 로드 성공 시 인스턴스 생성
                GameObject unitCard = Instantiate(loadedPrefab, cardDeck.transform);
                unitCard.name = "UnitTileObject";

                var cardData = GetCardKeyBasedOnProbability();

                // UnitCardObject 초기화
                UnitTileObject cardObject = unitCard.GetComponent<UnitTileObject>();
                if (cardObject != null)
                {
                    cardObject.InitializeCardInform(cardData); // 기본 초기화 데이터
                }

                // UnitCard_Empty를 비활성화
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

        //상점 레벨에 따른 확률 데이터 로드
        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        // 각 등급 확률 계산
        var normalProb = shopProbabilityData.f_normalGradeChance;  // 100 or 70
        var rareProb = normalProb + shopProbabilityData.f_rareGradeChance;  // 100 or 85
        var epicProb = rareProb + shopProbabilityData.f_epicGradeChance;    // 100 or 90
        var legendaryProb = epicProb + shopProbabilityData.f_legendaryGradeChance; // 100 or 95
        var mythicProb = legendaryProb + shopProbabilityData.f_mythicGradeChance;  // 100 or 100



        //랜덤값 생성
        int rand = UnityEngine.Random.Range(0, 100);

        UnitGrade slotGrade;

        //TODO :  확률에 따라 등급 선택
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

        
        return selectedUnit; // 선택된 유닛의 이름 반환
       

    }

    //TODO : 리롤 쿨타임, 클릭속도 제한 추가
    public void RerollCardDecks()
    {
        if(GameManager.Instance.CurrentCost >= 1)
        {

            GameManager.Instance.UseCost(1);
            // 기존 카드 제거
            foreach (var cardDeck in cardDecks)
            {
                // UnitCardObject 삭제
                var existingCard = cardDeck.GetComponentInChildren<UnitTileObject>();
                if (existingCard != null)
                {
                    Destroy(existingCard.gameObject);
                }

                // UnitCard_Empty 활성화
                var emptyPlaceholder = cardDeck.transform.Find("UnitCard_Empty");
                if (emptyPlaceholder != null)
                {
                    emptyPlaceholder.gameObject.SetActive(true);
                }
            }

            // 새 카드 덱 생성
            StartCoroutine(CheckAndFillCardDecks());
        }
       
    }

    private void UpdateShopLevelUI()
    {
        // 상점 레벨 텍스트 업데이트
        shopLevel = GameManager.Instance.StoreLevel;
        shopLevelText.text = $"Shop Level : {shopLevel}";

        // 업그레이드 비용 계산 및 표시

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
                // CostGauge UI 갱신
                CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
                costUI.Initialize(shopLevel);


            }
        }
    }

    private void OnCostUsed(int amount)
    {
        UpdateShopLevelUI();  // 코스트가 변경될 때마다 버튼 상태 업데이트
    }


    // UnitCardObject 삭제 시 호출하여 UnitCard_Empty 활성화
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
            GameManager.Instance.OnCostUsed -= OnCostUsed;  // 추가

        }
    }




    private void OnDestroy()
    {
        isChecking = false;

        //구독해제
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
            GameManager.Instance.OnCostUsed -= OnCostUsed;  // 추가
        }
    }


}
