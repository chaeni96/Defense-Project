using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDeckDragHandler : MonoBehaviour
{
    private UnitCardObject unitCard;

    // UnitCardObject�� ���� ������ �� ȣ��� �޼���
    public void SetUnitCard(UnitCardObject newCard)
    {
        unitCard = newCard;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("�巡��");
        if (unitCard != null)
        {
            // UnitCardObject�� �巡�� ���� �Լ� ȣ��
           // unitCard.StartDragFromDeck(eventData);
        }
    }

}
