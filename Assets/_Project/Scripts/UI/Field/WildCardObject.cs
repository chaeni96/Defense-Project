using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WildCardObject : MonoBehaviour
{
    //���ϵ� ī�� �̹���
    //���
    //����

    public Image cardBackImage;

    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text descripText;
    [SerializeField] private TMP_Text cardGradeText;

    private bool isSelectable;

    private D_WildCardData wildCardData;

    public System.Action onCardRevealed; // ī�尡 �������� �� ȣ��� �̺�Ʈ

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

    // ī�带 ���� ���� ���·� ����
    public void SetSelectable(bool selectable)
    {
        isSelectable = selectable;
    }

    // ī�� �޸��� ��Ȱ��ȭ�Ǿ��� �� ȣ��� �޼ҵ�
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
