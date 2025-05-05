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
    private float optionSpawnDelay = 0.1f; // 각 옵션 생성 간격

    private int cardsRevealed = 0; // 뒤집힌 카드 수

    // 옵션 선택 이벤트
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
        //프리팹 생성
        GameObject optionObj = ResourceManager.Instance.Instantiate("HuntingOptionObject");

        // worldPositionStays를 false로 설정하여 로컬 값 유지
        optionObj.transform.SetParent(position, false);

        optionObj.transform.localPosition = Vector3.zero;

        //옵션 데이터 초기화
        HuntingOptionObject huntingOption = optionObj.GetComponent<HuntingOptionObject>();
        huntingOption.Initialize(optionData, OnOptionClicked);
        // 카드가 뒤집힐 때 호출될 이벤트 등록
        huntingOption.onCardRevealed = () => {
            OnCardRevealed(huntingOption);
        };
        spawnedOptions.Add(huntingOption);

        //옵션 생성 애니메이션 실행
        yield return StartCoroutine(PlayOptionSpawnAnimationCoroutine(huntingOption));
    }

    private IEnumerator PlayOptionSpawnAnimationCoroutine(HuntingOptionObject option)
    {
        option.SetSelectable(false);

        // 초기 상태: 보이지 않는 상태
        option.transform.localScale = new Vector3(0f, 0f, 0f);

        // 뿅! 하고 나타나는 애니메이션
        Sequence cardSequence = DOTween.Sequence();

        // 약간 커졌다가 원래 크기로 돌아오는 효과
        cardSequence.Append(option.transform.DOScale(new Vector3(1.2f, 1.2f, 1.2f), 0.1f)
            .SetEase(Ease.OutBack));

        // 약간 위로 튀어오르는 효과 추가
        cardSequence.Join(option.transform.DOLocalMoveY(0.2f, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetRelative(true));

        // 원래 크기와 위치로 돌아오기
        cardSequence.Append(option.transform.DOScale(Vector3.one, 0.05f)
            .SetEase(Ease.OutQuad));

        cardSequence.Join(option.transform.DOLocalMoveY(0f, 0.05f)
            .SetEase(Ease.InQuad));

        // 시퀀스 완료 대기
        yield return cardSequence.WaitForCompletion();

        // 카드 뒷면 비활성화하고 즉시 앞면 보여주기
        if (option.cardBackImage != null && option.cardBackImage.gameObject.activeSelf)
        {
            option.cardBackImage.gameObject.SetActive(false);
            // 카드 뒤집힘 이벤트 발생
            option.NotifyCardRevealed();
        }
    }

    // 카드가 뒤집혔을 때 호출되는 메소드
    private void OnCardRevealed(HuntingOptionObject card)
    {
        cardsRevealed++;

        // 모든 카드가 뒤집히면 선택 가능하게 설정
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
        // 이벤트 발생
        OnOptionSelected?.Invoke(option);

        // UI 닫기
        UIManager.Instance.CloseUI<PrizeHuntingSelectUI>();
    }

    public override void HideUI()
    {
        // 생성된 옵션들 오브젝트 풀에 반환
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
