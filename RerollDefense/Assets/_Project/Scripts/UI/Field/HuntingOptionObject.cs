using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HuntingOptionObject : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private D_HuntingOptionData optionData;
    private System.Action<D_HuntingOptionData> onOptionClicked;

    public void Initialize(D_HuntingOptionData data, System.Action<D_HuntingOptionData> callback)
    {
        optionData = data;
        onOptionClicked = callback;
        UpdateUI();
    }
    private void UpdateUI()
    {
        titleText.text = optionData.f_title;
        descriptionText.text = optionData.f_description;
    }

    public void OnClickSelectOption()
    {
        // 클릭 이벤트 콜백 실행
        onOptionClicked?.Invoke(optionData);
    }
}
