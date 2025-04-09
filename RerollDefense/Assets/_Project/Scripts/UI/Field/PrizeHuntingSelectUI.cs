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
    private float optionSpawnDelay = 0.03f; // �� �ɼ� ���� ����

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

        // �ʱ� ����: ���η� ���� ����
        option.transform.localScale = new Vector3(0.05f, 0f, 1f);

        Sequence cardSequence = DOTween.Sequence();

        // 1. ���η� �þ��
        cardSequence.Append(option.transform.DOScaleY(1.8f, 0.1f)
            .SetEase(Ease.OutExpo));

        // 2. ���η� �þ�鼭 �ణ ���� Ƣ�������
        cardSequence.Append(option.transform.DOScaleX(1.1f, 0.15f)
            .SetEase(Ease.OutBack));

        // ī�尡 �ణ ���� Ƣ������� ȿ��
        cardSequence.Join(option.transform.DOLocalMoveY(0.3f, 0.15f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // 3. ���� ũ��� ���ƿ���
        cardSequence.Append(option.transform.DOScale(Vector3.one, 0.1f)
            .SetEase(Ease.OutQuad));

        // ���� ��ġ�� ���ƿ���
        cardSequence.Join(option.transform.DOLocalMoveY(0f, 0.1f)
            .SetEase(Ease.InQuad));
        // ������ ���� ���
        yield return cardSequence.WaitForCompletion();

        // 4. Y�� ���� ȸ�� ȿ�� - ������ ���� ���� ȸ�� �� õõ�� ���ƿ���
        Sequence rotateSequence = DOTween.Sequence();

        // ȸ�� ���� �� ������ ��¦ ���� (���������� ȿ��)
        rotateSequence.Append(option.transform.DOScaleX(0.8f, 0.15f)
            .SetEase(Ease.InSine));

        // Y�� ���� ������ ���� ���� ȸ�� (��: 1080�� = 3����)
        rotateSequence.Append(option.transform.DOLocalRotate(new Vector3(0, 1080, 0), 0.8f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuint));

        // ȸ�� �߿� ������ �ٽ� �������
        rotateSequence.Join(option.transform.DOScaleX(1f, 0.4f)
            .SetEase(Ease.OutBack));

        // ȸ�� �ִϸ��̼��� �� ���� ���� ����� ������ ī�� �޸� ��Ȱ��ȭ
        rotateSequence.InsertCallback(0.4f, () => {
            if (option.cardBackImage != null && option.cardBackImage.gameObject.activeSelf)
            {
                option.cardBackImage.gameObject.SetActive(false);
                // ī�� ������ �̺�Ʈ �߻�
                option.NotifyCardRevealed();
            }
        });

        // �ణ ������Ʈ �� õõ�� ���� ������ ���ƿ���
        rotateSequence.Insert(rotateSequence.Duration() - 0.22f,
            option.transform.DOLocalRotate(new Vector3(0, -25, 0), 0.3f)
            .SetEase(Ease.OutBack));

        // ���������� 0���� õõ�� ���ƿ���
        rotateSequence.Append(option.transform.DOLocalRotate(Vector3.zero, 0.5f)
            .SetEase(Ease.InOutSine));

        yield return rotateSequence.WaitForCompletion();
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
