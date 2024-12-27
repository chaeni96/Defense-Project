using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnitCardObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{



    public GameObject cardLevelObject;
    public Image cardImage;

    public TMP_Text unitDataText;
    public TMP_Text cardCostText;

    private GameObject activeDragObject;
    private string dragObjectAddress; // Addressable 키 == tileShapeName

    private int cardCost;
    private bool canDrag;

    public void InitializeCardInform(D_TileShpeData unitData)
    {
        //tileShpaeName을 통해 데이터 얻어오는데 여기에 유닛에 맞는 이미지 주소 넣어줘서 가져오기

        dragObjectAddress= unitData.f_name;
        unitDataText.text = unitData.f_name;
        //카드 정보, data 받아오기
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();
        canDrag = false;
    }


    public void OnPointerDown(PointerEventData eventData)
    {
        if(GameManager.Instance.CurrentCost < cardCost)
        {
            canDrag = false;
            return;
        }
        else
        {
            canDrag = true;
        }

        // DragObject 생성

        activeDragObject = PoolingManager.Instance.GetObject("DragObject");

        // DragObject 초기화
        DragObject dragObject = activeDragObject.GetComponent<DragObject>();
        if (dragObject != null)
        {
            Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
            pointerPosition.z = 0;

            dragObject.Initialize();
            dragObject.OnClickObject(dragObjectAddress, pointerPosition);
        }

        // 카드 이미지를 숨김
        cardImage.enabled= false;
        cardLevelObject.SetActive(false);
        unitDataText.enabled= false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        if(!canDrag) { return; }


        DragObject dragObject = activeDragObject.GetComponent<DragObject>();
        if (dragObject != null)
        {
            Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
            pointerPosition.z = 0;

            dragObject.OnPointerDrag(pointerPosition);

        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (activeDragObject != null)
        {
            DragObject dragScript = activeDragObject.GetComponent<DragObject>();

            dragScript.CheckPlacedObject();

            if (dragScript != null && !dragScript.isPlaced)
            {
                // 배치 실패 시 카드 이미지 복원
                cardImage.enabled = true;
                unitDataText.enabled = true;
                cardLevelObject.SetActive(true);
                Destroy(activeDragObject);
            }
            else
            {
                GameObject cardDeck = this.transform.parent.gameObject;
                FullWindowInGameDlg dlg = cardDeck.GetComponentInParent<FullWindowInGameDlg>();
                if (dlg != null)
                {
                    dlg.OnUnitCardDestroyed(cardDeck); // 덱 상태 업데이트
                    GameManager.Instance.UseCost(cardCost);
                }

                Destroy(this.gameObject);

            }
        }
    }


}
