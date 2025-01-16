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
    private float cardSpawnDelay = 0.05f; // �� ī�� ���� ����


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
        //������ ����
        GameObject cardObj = ResourceManager.Instance.Instantiate("WildCardObject");
        // worldPositionStays�� false�� �����Ͽ� ���� �� ����
        cardObj.transform.SetParent(position, false);

        cardObj.transform.localPosition = Vector3.zero;

        //ī�� ������ �ʱ�ȭ
        WildCardObject wildCard = cardObj.GetComponent<WildCardObject>();
        wildCard.Initialize(cardData);
        spawnedCards.Add(wildCard);

        //ī�� ���� �ִϸ��̼� ����
        yield return StartCoroutine(PlayCardSpawnAnimationCoroutine(wildCard));
    }

    private IEnumerator PlayCardSpawnAnimationCoroutine(WildCardObject card)
    {
        card.transform.localScale = Vector3.zero;

        card.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.3f);
    }


    //ī�� ������ ��� ����
    private List<D_WildCardData> GetWildCardDatas()
    {
        var result = new List<D_WildCardData>();

        //TODO : �κ񿡼� ������ ī�� ������ ���� �߰��ؾߵ�

        // ������ ������ ��޿� ���� ���� ī��� ä��
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
        // ��޺� ����Ȯ�� ���� -> database���� grade enum������ �������� Ȯ���� ������ �����
        var weights = new Dictionary<CardGrade, float>
        {
            { CardGrade.Normal, 0.6f },
            { CardGrade.Rare, 0.3f },
            { CardGrade.Epic, 0.1f },
        };

        float random = Random.value; //0���� 1���� ���� ������
        float currentProb = 0; //����Ȯ��

        foreach (var weight in weights)
        {
            currentProb += weight.Value; //��� Ȯ�� ���ذ��� ����

            if (random <= currentProb)
            {
                // BGKey�� ����Ͽ� �ش� ����� ī��� ��ȸ
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
        // ������ ī��� ������Ʈ Ǯ�� ��ȯ
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
