using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[UIInfo("WildCardSelectUI", "WildCardSelectUI", false)]
public class WildCardSelectUI : FloatingPopupBase
{

    [SerializeField] private Transform firstCardDeck;
    [SerializeField] private Transform secondCardDeck;
    [SerializeField] private Transform thirdCardDeck;

    [SerializeField] private TMP_Text cardSelectTimeText;
    [SerializeField] private TMP_Text highlightText;
    private List<WildCardObject> spawnedCards;
    [SerializeField] private float cardSpawnDelay = 0.1f; // 각 카드 생성 간격
    private int cardsRevealed = 0; // 뒤집힌 카드 수


    public override void InitializeUI()
    {
        base.InitializeUI();
        spawnedCards = new List<WildCardObject>();
        cardsRevealed = 0;
        StartHighlightTextAnimation();
    }

    public void SetWildCardDeck()
    {
        StartCoroutine(SpawnWildCardsCoroutine());

    }
    private void StartHighlightTextAnimation()
    {
        // 기존 Tween이 있다면 중지
        highlightText.DOKill();

        // 초기 알파값을 1로 설정 (완전히 보이는 상태에서 시작)
        Color initialColor = highlightText.color;
        initialColor.a = 1f;
        highlightText.color = initialColor;

        // 알파값을 1에서 0으로 페이드 아웃 후 다시 페이드 인하는 애니메이션
        highlightText.DOFade(0f, 1.2f)  // 1에서 0으로 두번째 인자값 초 동안 페이드
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)  // 무한 반복 (0으로 갔다가 다시 1로)
            .SetUpdate(true);  // UI 업데이트에 맞춰 실행
    }

    private IEnumerator SpawnWildCardsCoroutine()
    {
        var cardDatas = GetWildCardDatas();
        Transform[] positions = { firstCardDeck, secondCardDeck, thirdCardDeck };

        for (int i = 0; i < positions.Length; i++)
        {
            yield return StartCoroutine(SpawnCardCoroutine(positions[i], cardDatas[i]));
            yield return new WaitForSeconds(cardSpawnDelay);
        }
    }

    private IEnumerator SpawnCardCoroutine(Transform position, D_WildCardData cardData)
    {
        //프리팹 생성
        GameObject cardObj = ResourceManager.Instance.Instantiate("WildCardObject");
        // worldPositionStays를 false로 설정하여 로컬 값 유지
        cardObj.transform.SetParent(position, false);

        cardObj.transform.localPosition = Vector3.zero;

        //카드 데이터 초기화
        WildCardObject wildCard = cardObj.GetComponent<WildCardObject>();
        wildCard.Initialize(cardData);

        // 카드가 뒤집힐 때 호출될 이벤트 등록
        wildCard.onCardRevealed = () => {
            OnCardRevealed(wildCard);
        };

        spawnedCards.Add(wildCard);

        //카드 생성 애니메이션 실행
        yield return StartCoroutine(PlayCardSpawnAnimationCoroutine(wildCard));
    }

    private IEnumerator PlayCardSpawnAnimationCoroutine(WildCardObject card)
    {
        card.SetSelectable(false);

        // 초기 상태: 보이지 않는 상태
        card.transform.localScale = new Vector3(0f, 0f, 0f);

        // 뿅! 하고 나타나는 애니메이션
        Sequence cardSequence = DOTween.Sequence();

        // 약간 커졌다가 원래 크기로 돌아오는 효과
        cardSequence.Append(card.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f)
            .SetEase(Ease.OutBack));

        // 약간 위로 튀어오르는 효과 추가
        cardSequence.Join(card.transform.DOLocalMoveY(0.2f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // 원래 크기와 위치로 돌아오기
        cardSequence.Append(card.transform.DOScale(Vector3.one, 0.05f)
            .SetEase(Ease.OutQuad));

        cardSequence.Join(card.transform.DOLocalMoveY(0f, 0.05f)
            .SetEase(Ease.InQuad));

        // 시퀀스 완료 대기
        yield return cardSequence.WaitForCompletion();

        // 카드 뒷면 비활성화하고 즉시 앞면 보여주기
        if (card.cardBackImage != null && card.cardBackImage.gameObject.activeSelf)
        {
            card.cardBackImage.gameObject.SetActive(false);
            // 카드 뒤집힘 이벤트 발생
            card.NotifyCardRevealed();
        }
    }

    //TODO : 김채현
    //회전하는 동안 글로우 이펙트 주기 
    private void ApplyCardShaderEffect(WildCardObject card)
    {
      
    }
    // 카드가 뒤집혔을 때 호출되는 메소드
    private void OnCardRevealed(WildCardObject card)
    {
        cardsRevealed++;

        // 모든 카드가 뒤집히면 선택 가능하게 설정
        if (cardsRevealed >= 3)
        {
            foreach (var spawnedCard in spawnedCards)
            {
                spawnedCard.SetSelectable(true);
            }
        }
    }


    //카드 데이터 목록 생성
    private List<D_WildCardData> GetWildCardDatas()
    {
        var result = new List<D_WildCardData>();

        //TODO : 로비에서 선택한 카드 있으면 먼저 추가해야됨

        // 나머지 슬롯을 등급에 따른 랜덤 카드로 채움
        while (result.Count < 3)
        {
            var cardData = GetRandomWildCardByGrade();
            if (cardData != null && !result.Contains(cardData))
            {
                result.Add(cardData);
            }
        }

        return result;
    }


    private D_WildCardData GetRandomWildCardByGrade()
    {
        // 등급별 출현확률 설정 -> database에서 grade enum값으로 하지말고 확률로 넣을지 고민중
        var weights = new Dictionary<CardGrade, float>
        {
            { CardGrade.Normal, 1f },
            { CardGrade.Rare, 0f },
            { CardGrade.Epic, 0f },
        };

        float random = Random.value; //0부터 1까지 사이 랜덤값
        float currentProb = 0; //누적확률

        foreach (var weight in weights)
        {
            currentProb += weight.Value; //등급 확률 더해가며 누적

            if (random <= currentProb)
            {
                // BGKey를 사용하여 해당 등급의 카드들 조회
                var cardsOfGrade = D_WildCardData.GetEntitiesByKeyKeyGrade(weight.Key);

                if (cardsOfGrade != null && cardsOfGrade.Count > 0)
                {
                    return cardsOfGrade[Random.Range(0, cardsOfGrade.Count)];
                }
            }
        }

        return null;
    }


    public void UpdateSelectTime(int leftTime)
    {
        cardSelectTimeText.text = $"카드 선택 남은 시간 : {leftTime}..";
    }


    public override void HideUI()
    {
        // 생성된 카드들 오브젝트 풀에 반환
        foreach (var card in spawnedCards)
        {
            if (card != null)
            {
                Destroy(card.gameObject);
            }
        }
        spawnedCards.Clear();

        base.HideUI();
    }

}
