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

    //TODO : UIBase 상속받아야됨

    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;
    public GameObject fourthCardDeck;

    [SerializeField] private Slider hpBar;
    [SerializeField] private float hpUpdateSpeed = 2f;  // HP Bar 감소 속도
    [SerializeField] private GameObject CostGauge;  // HP Bar 감소 속도
    [SerializeField] private TMP_Text shopLevelText;  // HP Bar 감소 속도
    private float targetHPRatio;
    private Coroutine hpUpdateCoroutine;

    private List<GameObject> cardDecks;
    private bool isChecking = false;

    //test용 변수들
    public float checkCooldown = 3f;
    private int shopLevel = 1;

    //상점에서 확률 가지고 와서 카드 덱 4개 설치 
    public override void InitializeUI()
    {
        base.InitializeUI();


    }

    void Start()
    {
        //test용
        initUI();
    }

    //TODO : UIManager 만들고 InitializeUI에 넣어야할 코드
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
                    if (cardDeck.GetComponentInChildren<UnitCardObject>() == null)
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
        ResourceManager.Instance.LoadAsync<GameObject>("UnitCardObject", (loadedPrefab) =>
        {
            if (loadedPrefab != null)
            {
                // 프리팹 로드 성공 시 인스턴스 생성
                GameObject unitCard = Instantiate(loadedPrefab, cardDeck.transform);
                unitCard.name = "UnitCardObject";

                var cardData = GetCardKeyBasedOnProbability();

                // UnitCardObject 초기화
                UnitCardObject cardObject = unitCard.GetComponent<UnitCardObject>();
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

        //각 등급 확률
        var normalShopProbability = shopProbabilityData.f_normalGradeChance;
        var rareShopProbability = shopProbabilityData.f_rareGradeChance;
        var epicProbability = shopProbabilityData.f_epicGradeChance;
        var legendaryShopProbability = shopProbabilityData.f_legendaryGradeChance;
        var mythicShopProbability = shopProbabilityData.f_mythicGradeChance;

        //랜덤값 생성
        int rand = UnityEngine.Random.Range(0, 100);

        UnitGrade slotGrade;

        //TODO :  확률에 따라 등급 선택
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

        
        return selectedUnit; // 선택된 유닛의 이름 반환
       

    }

    //TODO : 리롤 쿨타임, 클릭속도 제한 추가
    public void RerollCardDecks()
    {
        // 기존 카드 제거
        foreach (var cardDeck in cardDecks)
        {
            // UnitCardObject 삭제
            var existingCard = cardDeck.GetComponentInChildren<UnitCardObject>();
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
    }



    private void OnDestroy()
    {
        isChecking = false;

        //구독해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnHPChanged -= OnHPChanged;
        }
    }


}
