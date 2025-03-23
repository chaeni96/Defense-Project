using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[UIInfo("PrizeHuntingSelectUI", "PrizeHuntingSelectUI", false)]
public class PrizeHuntingSelectUI : FloatingPopupBase
{
    [SerializeField] private Transform firstOptionDeck;
    [SerializeField] private Transform secondOptionDeck;

    private List<HuntingOptionObject> spawnedOptions;
    private float optionSpawnDelay = 0.05f; // 각 옵션 생성 간격


    // 옵션 선택 이벤트
    public event Action<D_HuntingOptionData> OnOptionSelected;

    public override void InitializeUI()
    {
        base.InitializeUI();
        spawnedOptions = new List<HuntingOptionObject>();
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
        spawnedOptions.Add(huntingOption);

        //옵션 생성 애니메이션 실행
        yield return StartCoroutine(PlayOptionSpawnAnimationCoroutine(huntingOption));
    }

    private IEnumerator PlayOptionSpawnAnimationCoroutine(HuntingOptionObject option)
    {
        option.transform.localScale = Vector3.zero;

        option.transform.DOScale(Vector3.one, 0.3f)
            .SetEase(Ease.OutBack);

        yield return new WaitForSeconds(0.3f);
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
