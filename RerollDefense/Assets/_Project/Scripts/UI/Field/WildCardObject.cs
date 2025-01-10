using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WildCardObject : MonoBehaviour
{
    //와일드 카드 이미지
    //등급
    //설명

    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descripText;
    [SerializeField] private TMP_Text cardGradeText;


    private D_WildCardData wildCardData;

    public void Initialize(D_WildCardData cardData)
    {
        wildCardData = cardData;

        UpdateText();
    }

    private void UpdateText()
    {
        cardNameText.text = wildCardData.f_WildCardName;
        descripText.text = wildCardData.f_Description;
        cardGradeText.text = wildCardData.f_Grade.ToString();
    }


    public  void OnClickSelectCard()
    {
        UIManager.Instance.CloseUI<WildCardSelectUI>();

        WildCardManager.Instance.ApplyWildCardEffect(wildCardData);
    }

}
