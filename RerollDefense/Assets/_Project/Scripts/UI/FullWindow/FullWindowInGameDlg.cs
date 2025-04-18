using BGDatabaseEnum;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using DG.Tweening;


[UIInfo("FullWindowInGameDlg", "FullWindowInGameDlg", true)]
public class FullWindowInGameDlg : FullWindowBase
{
    public GameObject firstCardDeck;
    public GameObject secondCardDeck;
    public GameObject thirdCardDeck;
    public GameObject forthCardDeck;
    public GameObject fifthCardDeck;


    //[SerializeField] private GameObject CostGauge;  
    [SerializeField] private TMP_Text shopLevelText;  // 현재 상점레벨
    [SerializeField] private TMP_Text shopLevelUpCostText;  // 업그레이드 비용 표시 텍스트

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

    [SerializeField] private List<Image> cardGradeImages;  // Inspector에서 firstCardGrade, secondCardGrade, thirdCardGrade 순서대로 할당
    [SerializeField] private TMP_Text progressWaveIndex; //진행중인 웨이브 인덱스
    [SerializeField] private TMP_Text remainEnemyCount; //남은 enemy 수

    //카드덱
    private List<GameObject> cardDecks;
    [SerializeField] private List<GameObject> emptyCardObjects;
    private List<UnitCardObject> currentCards;

    private bool isChecking = false;

    //test용 변수들
    public float checkCooldown = 1f;
    private int shopLevel;
    private int shopUpgradeCost;

    //상점에서 확률 가지고 와서 카드 덱 4개 설치 
    public override void InitializeUI()
    {
        CleanUp();

        base.InitializeUI();

        
        InitializeAssociateUI();

        InitializeCardDecks();
        UpdateShopLevelUI();

        //이벤트 구독
        GameManager.Instance.OnCostUsed += OnCostUsed;  
        UnitCardObject.OnCardUsed += OnUnitCardDestroyed;
        StageManager.Instance.OnEnemyCountChanged += UpdateRemainEnemyCount;
        StageManager.Instance.OnWaveIndexChanged += UpdateWaveIndex;
    }


    //관련 유아이 초기화
    private void InitializeAssociateUI()
    {

        //cost 초기화
        //CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
        //costUI.Initialize(GameManager.Instance.GetSystemStat(StatName.StoreLevel));

        //shopLevel 초기화
        shopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel);
        shopLevelText.text = $"Shop Level : {shopLevel}";


    }


    private void InitializeCardDecks()
    {
        cardDecks = new List<GameObject> { firstCardDeck, secondCardDeck, thirdCardDeck, forthCardDeck, fifthCardDeck };

      
        currentCards = new List<UnitCardObject>();

        foreach (var deck in cardDecks)
        {
            currentCards.Add(null);
        }

        StartCoroutine(CheckAndFillCardDecks());
    }

    private void UpdateWaveIndex(int currentIndex, int subIndex = 0)
    {

        progressWaveIndex.text = $"{GameManager.Instance.SelectedStageNumber} - {currentIndex}";
    }
    private void UpdateRemainEnemyCount(int count)
    {
        remainEnemyCount.text = count.ToString();
    }


    private IEnumerator CheckAndFillCardDecks()
    {
        var cardDatas = new List<D_TileCardData>();

        // 카드 데이터 미리 준비
        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (emptyCardObjects[i].activeSelf && currentCards[i] == null)
            {
                cardDatas.Add(GetCardKeyBasedOnProbability());
            }
            else
            {
                cardDatas.Add(null);
            }
        }

        // 각 카드에 대해 0.1초 간격으로 코루틴 실행
        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (cardDatas[i] != null)
            {
                emptyCardObjects[i].SetActive(false);
                StartCoroutine(PlayEffectAndSpawnCard(i, cardDatas[i]));
                yield return new WaitForSeconds(0.06f); // 다음 카드 이펙트 간격
            }
        }
    }
    private IEnumerator PlayEffectAndSpawnCard(int index, D_TileCardData cardData)
    {
        var grade = cardData.f_grade;

        // 등급 색상 적용
        Color gradeColor = GetGradeColor(grade);
        cardGradeImages[index].color = gradeColor;

        // 이펙트 실행
        PlayCardEnterEffect(cardGradeImages[index]);

        // 이펙트 실행 시간만큼 대기
        yield return new WaitForSeconds(0.24f);

        // 카드 생성
        CreateUnitCardWithData(cardDecks[index], index, cardData);
    }

    private void PlayCardEnterEffect(Image unitCard)
    {
        if (unitCard == null) return;

        //트위스트, zoom
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
    // 이펙트도 있다면..
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
    // UnitCardObject 생성 및 덱에 추가
    /*private void CreateUnitCard(GameObject cardDeck, int deckIndex)
    {

        GameObject unitCard = PoolingManager.Instance.GetObject("UnitCardObject");

        if(unitCard != null)
        {
            //cardDeck 부모 설정 및 로컬 포지션 초기화
            unitCard.transform.SetParent(cardDeck.transform);
            unitCard.transform.localPosition = Vector3.zero;

            // UnitCardObject 초기화
            UnitCardObject cardObject = unitCard.GetComponent<UnitCardObject>();

            if (cardObject != null)
            {
                // 카드 데이터 가져오기
                var cardData = GetCardKeyBasedOnProbability();
                cardObject.InitializeCardInform(cardData);
                currentCards[deckIndex] = cardObject;

                // CardDeckDragHandler에 새로운 UnitCardObject 참조 설정
                var dragHandler = cardDeck.GetComponent<CardDeckDragHandler>();
                if (dragHandler != null)
                {
                    dragHandler.SetUnitCard(cardObject);
                }

                // UnitCard_Empty를 비활성화
                emptyCardObjects[deckIndex].SetActive(false);


                // 카드 등급에 따른 색상 설정
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
        var possibleUnitList = D_TileCardData.FindEntities(data => data.f_grade == slotGrade);


        var selectedUnit = possibleUnitList.OrderBy(_ => Guid.NewGuid()).First();

        
        return selectedUnit; // 선택된 유닛의 이름 반환
       

    }

    //TODO : 리롤 쿨타임, 클릭속도 제한 추가
    public void RerollCardDecks()
    {
        //코스트 1보다 낮으면 불가
        if (GameManager.Instance.GetSystemStat(StatName.Cost) < 1) return;


        // 코스트 소모 처리 추가
        StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
        {
            statName = StatName.Cost,
            value = -1, // 리롤 비용 1 소모
            multiply = 1f
        });

        // 기존 카드 제거
        for (int i = 0; i < cardDecks.Count; i++)
        {
            if (currentCards[i] != null)
            {
                // UnitCardObject 삭제
                Destroy(currentCards[i].gameObject);
                currentCards[i] = null;
            }

            cardGradeImages[i].color = Color.white;

            // UnitCard_Empty 활성화
            emptyCardObjects[i].SetActive(true);
        }
            // 새 카드 덱 생성
            StartCoroutine(CheckAndFillCardDecks());
    }
       

    private void UpdateShopLevelUI()
    {
        // 상점 레벨 텍스트 업데이트
        shopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel);
        shopLevelText.text = $"Shop Level : {shopLevel}";

        // 업그레이드 비용 계산 및 표시

        var shopProbabilityData = D_UnitShopChanceData.GetEntityByKeyShopLevel(shopLevel);

        shopUpgradeCost = shopProbabilityData.f_upgradeCost;

        shopLevelUpCostText.text = shopUpgradeCost.ToString();

        // 확률 텍스트 업데이트 추가
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
            // 비용 소모 처리
            StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
            {
                statName = StatName.Cost,
                value = -shopUpgradeCost, // 비용 소모는 음수로 전달
                multiply = 1f
            });

            // 상점 레벨 증가 처리
            var newShopLevel = GameManager.Instance.GetSystemStat(StatName.StoreLevel) + 1;
            StatManager.Instance.BroadcastStatChange(StatSubject.System, new StatStorage
            {
                statName = StatName.StoreLevel,
                value = newShopLevel, // 새로운 상점 레벨 전달
                multiply = 1f
            });

            // UI 업데이트
            UpdateShopLevelUI();

            // CostGauge UI 갱신
            //CostGaugeUI costUI = CostGauge.GetComponent<CostGaugeUI>();
            //costUI.Initialize(newShopLevel);
        }
    }

    private void OnCostUsed(int amount)
    {
        UpdateShopLevelUI();  // 코스트가 변경될 때마다 버튼 상태 업데이트


        foreach(var card in currentCards)
        {
            if(card != null)
            {
                card.UpdateCostTextColor();
            }
        }
    }


    // UnitCardObject 삭제 시 호출하여 UnitCard_Empty 활성화
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
        //TODO: hide애니메이션 추가했을때 주석 해제
        //base.HideUI();

        CleanUp();

    }




    private void CleanUp()
    {
        // 카드 관련 정리
        //isChecking = false;
        if (currentCards != null)
        {
            foreach (var card in currentCards.Where(c => c != null))
            {
                Destroy(card.gameObject);
            }
            currentCards.Clear();
        }

        // 리스트 정리
        cardDecks?.Clear();

        // 이벤트 구독 해제
        if (GameManager._instance != null)
        {
            GameManager.Instance.OnCostUsed -= OnCostUsed;
            UnitCardObject.OnCardUsed -= OnUnitCardDestroyed;
            StageManager.Instance.OnEnemyCountChanged -= UpdateRemainEnemyCount;
            StageManager.Instance.OnWaveIndexChanged -= UpdateWaveIndex;

        }
    }

    public void OnClickSettingBtn()
    {
        //일시정지 상태로 전환
        GameManager.Instance.ChangeState(new GamePauseState(GameManager.Instance.currentState));

    }
}
