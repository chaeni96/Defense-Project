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
    private float optionSpawnDelay = 0.05f; // �� �ɼ� ���� ����


    // �ɼ� ���� �̺�Ʈ
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
        //������ ����
        GameObject optionObj = ResourceManager.Instance.Instantiate("HuntingOptionObject");

        // worldPositionStays�� false�� �����Ͽ� ���� �� ����
        optionObj.transform.SetParent(position, false);

        optionObj.transform.localPosition = Vector3.zero;

        //�ɼ� ������ �ʱ�ȭ
        HuntingOptionObject huntingOption = optionObj.GetComponent<HuntingOptionObject>();
        huntingOption.Initialize(optionData, OnOptionClicked);
        spawnedOptions.Add(huntingOption);

        //�ɼ� ���� �ִϸ��̼� ����
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
