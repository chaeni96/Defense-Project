using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[UIInfo("WildCardSelectUI", "WildCardSelectUI", false)]
public class WildCardSelectUI : FloatingPopupBase
{

    [SerializeField] private Transform firstCardDeck;
    [SerializeField] private Transform secondCardDeck;
    [SerializeField] private Transform thirdCardDeck;


    private List<WildCardObject> spawnedCards;
    private float cardSpawnDelay = 0.05f; // 각 카드 생성 간격


    public override void InitializeUI()
    {
        base.InitializeUI();
        spawnedCards = new List<WildCardObject>();
    }

    public void SetWildCardDeck()
    {
        StartCoroutine(SpawnWildCardsCoroutine());

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
        spawnedCards.Add(wildCard);

        //카드 생성 애니메이션 실행
        yield return StartCoroutine(PlayCardSpawnAnimationCoroutine(wildCard));
    }

    private IEnumerator PlayCardSpawnAnimationCoroutine(WildCardObject card)
    {
        card.transform.localScale = Vector3.zero;

        card.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.3f);
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
            { CardGrade.Normal, 0.6f },
            { CardGrade.Rare, 0.3f },
            { CardGrade.Epic, 0.1f },
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
