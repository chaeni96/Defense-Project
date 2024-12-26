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
    private string dragObjectAddress; // Addressable Ű == tileShapeName
    

    public void InitializeCardInform(string unitData)
    {
        //tileShpaeName�� ���� ������ �����µ� ���⿡ ���ֿ� �´� �̹��� �ּ� �־��༭ ��������

        dragObjectAddress= unitData;
        unitDataText.text = unitData; 
        //ī�� ����, data �޾ƿ���

        //tileShapeName�� �˸��

    }


    public void OnPointerDown(PointerEventData eventData)
    {

        // DragObject ����

        activeDragObject = PoolingManager.Instance.GetObject("DragObject");

        // DragObject �ʱ�ȭ
        DragObject dragObject = activeDragObject.GetComponent<DragObject>();
        if (dragObject != null)
        {
            Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
            pointerPosition.z = 0;

            dragObject.Initialize();
            dragObject.OnClickObject(dragObjectAddress, pointerPosition);
        }

        // ī�� �̹����� ����
        cardImage.enabled= false;
        cardLevelObject.SetActive(false);
        unitDataText.enabled= false;
    }


    public void OnDrag(PointerEventData eventData)
    {
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
                // ��ġ ���� �� ī�� �̹��� ����
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
                    dlg.OnUnitCardDestroyed(cardDeck); // �� ���� ������Ʈ
                }

                Destroy(this.gameObject);

            }
        }
    }


}
