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
    public TMP_Text cardLevel;

    private GameObject activeDragObject;
    private string dragObjectAddress; // Addressable 키 == tileShapeName
    

    public void InitializeCardInform(string unitData)
    {
        //tileShpaeName을 통해 데이터 얻어오는데 여기에 유닛에 맞는 이미지 주소 넣어줘서 가져오기

        dragObjectAddress= unitData;
        unitDataText.text = unitData; 
        //카드 정보, data 받아오기

        //tileShapeName만 알면됨

    }


    public void OnPointerDown(PointerEventData eventData)
    {

        // DragObject 생성
        activeDragObject = ResourceManager.Instance.Instantiate("DragObject.prefab");

        // DragObject 초기화
        DemoDragObject dragObject = activeDragObject.GetComponent<DemoDragObject>();
        if (dragObject != null)
        {
            dragObject.Initialize();
            dragObject.SetUpUnitData(dragObjectAddress);

            dragObject.TESTOnPointerDown();
        }

        // 카드 이미지를 숨김
        cardImage.enabled= false;
        cardLevelObject.SetActive(false);
        unitDataText.enabled= false;
    }


    public void OnDrag(PointerEventData eventData)
    {
        DemoDragObject dragObject = activeDragObject.GetComponent<DemoDragObject>();
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
            DemoDragObject dragScript = activeDragObject.GetComponent<DemoDragObject>();

            dragScript.TESTOnPointerUp();

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
                }

                Destroy(this.gameObject);

            }
        }
    }


}
