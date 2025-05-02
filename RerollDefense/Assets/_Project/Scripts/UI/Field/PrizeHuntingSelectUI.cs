using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[UIInfo("PrizeHuntingSelectUI", "PrizeHuntingSelectUI", false)]
public class PrizeHuntingSelectUI : FloatingPopupBase
{
    [SerializeField] private Transform firstOptionDeck;
    [SerializeField] private Transform secondOptionDeck;

    [SerializeField] private TMP_Text highlightText;

    private List<HuntingOptionObject> spawnedOptions;
    private float optionSpawnDelay = 0.1f; // �� �ɼ� ���� ����

    private int cardsRevealed = 0; // ������ ī�� ��

    // �ɼ� ���� �̺�Ʈ
    public event Action<D_HuntingOptionData> OnOptionSelected;

    public override void InitializeUI()
    {
        base.InitializeUI();
        spawnedOptions = new List<HuntingOptionObject>();
        cardsRevealed = 0;
        StartHighlightTextAnimation();
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
    public void SetHuntingOptions(List<D_HuntingOptionData> optionDatas)
    {
        StartCoroutine(SpawnHuntingOptionsCoroutine(optionDatas));
    }
    private IEnumerator SpawnHuntingOptionsCoroutine(List<D_HuntingOptionData> optionDatas)
    {
        Transform[] positions = { firstOptionDeck, secondOptionDeck };
        int count = Mathf.Min(optionDatas.Count, positions.Length);

        for (int i = 0; i < count; i++)
        {
            yield return StartCoroutine(SpawnOptionCoroutine(positions[i], optionDatas[i]));
            yield return new WaitForSeconds(optionSpawnDelay);
        }
    }

    private IEnumerator SpawnOptionCoroutine(Transform position, D_HuntingOptionData optionData)
    {
        //������ ����
        GameObject optionObj = ResourceManager.Instance.Instantiate("HuntingOptionObject");

        // worldPositionStays�� false�� �����Ͽ� ���� �� ����
        optionObj.transform.SetParent(position, false);

        optionObj.transform.localPosition = Vector3.zero;

        //�ɼ� ������ �ʱ�ȭ
        HuntingOptionObject huntingOption = optionObj.GetComponent<HuntingOptionObject>();
        huntingOption.Initialize(optionData, OnOptionClicked);
        // ī�尡 ������ �� ȣ��� �̺�Ʈ ���
        huntingOption.onCardRevealed = () => {
            OnCardRevealed(huntingOption);
        };
        spawnedOptions.Add(huntingOption);

        //�ɼ� ���� �ִϸ��̼� ����
        yield return StartCoroutine(PlayOptionSpawnAnimationCoroutine(huntingOption));
    }

    private IEnumerator PlayOptionSpawnAnimationCoroutine(HuntingOptionObject option)
    {
        option.SetSelectable(false);

        // �ʱ� ����: ������ �ʴ� ����
        option.transform.localScale = new Vector3(0f, 0f, 0f);

        // ��! �ϰ� ��Ÿ���� �ִϸ��̼�
        Sequence cardSequence = DOTween.Sequence();

        // �ణ Ŀ���ٰ� ���� ũ��� ���ƿ��� ȿ��
        cardSequence.Append(option.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f)
            .SetEase(Ease.OutBack));

        // �ణ ���� Ƣ������� ȿ�� �߰�
        cardSequence.Join(option.transform.DOLocalMoveY(0.2f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // ���� ũ��� ��ġ�� ���ƿ���
        cardSequence.Append(option.transform.DOScale(Vector3.one, 0.05f)
            .SetEase(Ease.OutQuad));

        cardSequence.Join(option.transform.DOLocalMoveY(0f, 0.05f)
            .SetEase(Ease.InQuad));

        // ������ �Ϸ� ���
        yield return cardSequence.WaitForCompletion();

        // ī�� �޸� ��Ȱ��ȭ�ϰ� ��� �ո� �����ֱ�
        if (option.cardBackImage != null && option.cardBackImage.gameObject.activeSelf)
        {
            option.cardBackImage.gameObject.SetActive(false);
            // ī�� ������ �̺�Ʈ �߻�
            option.NotifyCardRevealed();
        }
    }

    // ī�尡 �������� �� ȣ��Ǵ� �޼ҵ�
    private void OnCardRevealed(HuntingOptionObject card)
    {
        cardsRevealed++;

        // ��� ī�尡 �������� ���� �����ϰ� ����
        if (cardsRevealed >= 2)
        {
            foreach (var spawnedOption in spawnedOptions)
            {
                spawnedOption.SetSelectable(true);
            }
        }
    }


    private void OnOptionClicked(D_HuntingOptionData option)
    {
        // �̺�Ʈ �߻�
        OnOptionSelected?.Invoke(option);

        // UI �ݱ�
        UIManager.Instance.CloseUI<PrizeHuntingSelectUI>();
    }

    public override void HideUI()
    {
        // ������ �ɼǵ� ������Ʈ Ǯ�� ��ȯ
        foreach (var option in spawnedOptions)
        {
            if (option != null)
            {
                Destroy(option.gameObject);
            }
        }
        spawnedOptions.Clear();

        base.HideUI();
    }

}
