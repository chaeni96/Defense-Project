using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDeckDragHandler : MonoBehaviour
{
    private UnitCardObject unitCard;

    // UnitCardObject가 새로 생성될 때 호출될 메서드
    public void SetUnitCard(UnitCardObject newCard)
    {
        unitCard = newCard;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("드래그");
        if (unitCard != null)
        {
            // UnitCardObject의 드래그 시작 함수 호출
           // unitCard.StartDragFromDeck(eventData);
        }
    }

}
