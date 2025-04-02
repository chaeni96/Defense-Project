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
    private float cardSpawnDelay = 0.03f; // �� ī�� ���� ����
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

        // �ʱ� ����: ���η� ���� ����
        card.transform.localScale = new Vector3(0.05f, 0f, 1f);

        Sequence cardSequence = DOTween.Sequence();
        
        // 1. ���η� �þ��
        cardSequence.Append(card.transform.DOScaleY(1.8f, 0.1f)
            .SetEase(Ease.OutExpo));
        
        // 2. ���η� �þ�鼭 �ణ ���� Ƣ�������
        cardSequence.Append(card.transform.DOScaleX(1.1f, 0.15f)
            .SetEase(Ease.OutBack));

        // ī�尡 �ణ ���� Ƣ������� ȿ��
        cardSequence.Join(card.transform.DOLocalMoveY(0.3f, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // 3. ���� ũ��� ���ƿ���
        cardSequence.Append(card.transform.DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.OutQuad));

        // ���� ��ġ�� ���ƿ���
        cardSequence.Join(card.transform.DOLocalMoveY(0f, 0.1f)
            .SetEase(Ease.InQuad));
        // ������ ���� ���
        yield return cardSequence.WaitForCompletion();
       
        // 4. Y�� ���� ȸ�� ȿ�� - ������ ���� ���� ȸ�� �� õõ�� ���ƿ���
        Sequence rotateSequence = DOTween.Sequence();

        // ȸ�� ���� �� ������ ��¦ ���� (���������� ȿ��)
        rotateSequence.Append(card.transform.DOScaleX(0.8f, 0.15f)
            .SetEase(Ease.InSine));

        // Y�� ���� ������ ���� ���� ȸ�� (��: 1080�� = 3����)
        rotateSequence.Append(card.transform.DOLocalRotate(new Vector3(0, 1080, 0), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuint));

        // ȸ�� �߿� ������ �ٽ� �������
        rotateSequence.Join(card.transform.DOScaleX(1f, 0.4f)
            .SetEase(Ease.OutBack));

        // ȸ�� �ִϸ��̼��� �� ���� ���� ����� ������ ī�� �޸� ��Ȱ��ȭ
        rotateSequence.InsertCallback(0.4f, () => {
            if (card.cardBackImage != null && card.cardBackImage.gameObject.activeSelf)
            {
                card.cardBackImage.gameObject.SetActive(false);
                // ī�� ������ �̺�Ʈ �߻�
                card.NotifyCardRevealed();
            }
        });

        // �ణ ������Ʈ �� õõ�� ���� ������ ���ƿ���
        rotateSequence.Insert(rotateSequence.Duration() - 0.22f,
            card.transform.DOLocalRotate(new Vector3(0, -25, 0), 0.3f)
            .SetEase(Ease.OutBack));

        // ���������� 0���� õõ�� ���ƿ���
        rotateSequence.Append(card.transform.DOLocalRotate(Vector3.zero, 0.5f)
            .SetEase(Ease.InOutSine));

        yield return rotateSequence.WaitForCompletion();
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
