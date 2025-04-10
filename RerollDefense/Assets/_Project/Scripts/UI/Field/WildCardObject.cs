using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WildCardObject : MonoBehaviour
{
    //와일드 카드 이미지
    //등급
    //설명

    public Image cardBackImage;

    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descripText;
    [SerializeField] private TMP_Text cardGradeText;

    private bool isSelectable;

    private D_WildCardData wildCardData;

    public System.Action onCardRevealed; // 카드가 뒤집혔을 때 호출될 이벤트

    public void Initialize(D_WildCardData cardData)
    {
        wildCardData = cardData;
        isSelectable = false;
        UpdateText();
    }

    private void UpdateText()
    {
        cardNameText.text = wildCardData.f_WildCardName;
        descripText.text = wildCardData.f_Description;
        cardGradeText.text = wildCardData.f_Grade.ToString();
    }

    // 카드를 선택 가능 상태로 설정
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    // 카드 뒷면이 비활성화되었을 때 호출될 메소드
    public void NotifyCardRevealed()
    {
        onCardRevealed?.Invoke();
    }

    public  void OnClickSelectCard()
    { 
        if(isSelectable)
        {
            UIManager.Instance.CloseUI<WildCardSelectUI>();

            WildCardManager.Instance.ApplyWildCardEffect(wildCardData);
        }
       
    }

}
