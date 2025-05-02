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
    [SerializeField] private float cardSpawnDelay = 0.1f; // �� ī�� ���� ����
    private int cardsRevealed = 0; // ������ ī�� ��


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
        // ���� Tween�� �ִٸ� ����
        highlightText.DOKill();

        // �ʱ� ���İ��� 1�� ���� (������ ���̴� ���¿��� ����)
        Color initialColor = highlightText.color;
        initialColor.a = 1f;
        highlightText.color = initialColor;

        // ���İ��� 1���� 0���� ���̵� �ƿ� �� �ٽ� ���̵� ���ϴ� �ִϸ��̼�
        highlightText.DOFade(0f, 1.2f)  // 1���� 0���� �ι�° ���ڰ� �� ���� ���̵�
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)  // ���� �ݺ� (0���� ���ٰ� �ٽ� 1��)
            .SetUpdate(true);  // UI ������Ʈ�� ���� ����
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

        // ī�尡 ������ �� ȣ��� �̺�Ʈ ���
        wildCard.onCardRevealed = () => {
            OnCardRevealed(wildCard);
        };

        spawnedCards.Add(wildCard);

        //ī�� ���� �ִϸ��̼� ����
        yield return StartCoroutine(PlayCardSpawnAnimationCoroutine(wildCard));
    }

    private IEnumerator PlayCardSpawnAnimationCoroutine(WildCardObject card)
    {
        card.SetSelectable(false);

        // �ʱ� ����: ������ �ʴ� ����
        card.transform.localScale = new Vector3(0f, 0f, 0f);

        // ��! �ϰ� ��Ÿ���� �ִϸ��̼�
        Sequence cardSequence = DOTween.Sequence();

        // �ణ Ŀ���ٰ� ���� ũ��� ���ƿ��� ȿ��
        cardSequence.Append(card.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f)
            .SetEase(Ease.OutBack));

        // �ణ ���� Ƣ������� ȿ�� �߰�
        cardSequence.Join(card.transform.DOLocalMoveY(0.2f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // ���� ũ��� ��ġ�� ���ƿ���
        cardSequence.Append(card.transform.DOScale(Vector3.one, 0.05f)
            .SetEase(Ease.OutQuad));

        cardSequence.Join(card.transform.DOLocalMoveY(0f, 0.05f)
            .SetEase(Ease.InQuad));

        // ������ �Ϸ� ���
        yield return cardSequence.WaitForCompletion();

        // ī�� �޸� ��Ȱ��ȭ�ϰ� ��� �ո� �����ֱ�
        if (card.cardBackImage != null && card.cardBackImage.gameObject.activeSelf)
        {
            card.cardBackImage.gameObject.SetActive(false);
            // ī�� ������ �̺�Ʈ �߻�
            card.NotifyCardRevealed();
        }
    }

    //TODO : ��ä��
    //ȸ���ϴ� ���� �۷ο� ����Ʈ �ֱ� 
    private void ApplyCardShaderEffect(WildCardObject card)
    {
      
    }
    // ī�尡 �������� �� ȣ��Ǵ� �޼ҵ�
    private void OnCardRevealed(WildCardObject card)
    {
        cardsRevealed++;

        // ��� ī�尡 �������� ���� �����ϰ� ����
        if (cardsRevealed >= 3)
        {
            foreach (var spawnedCard in spawnedCards)
            {
                spawnedCard.SetSelectable(true);
            }
        }
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
            { CardGrade.Normal, 1f },
            { CardGrade.Rare, 0f },
            { CardGrade.Epic, 0f },
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


    public void UpdateSelectTime(int leftTime)
    {
        cardSelectTimeText.text = $"ī�� ���� ���� �ð� : {leftTime}..";
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
