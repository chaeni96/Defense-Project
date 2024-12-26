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
        activeDragObject = ResourceManager.Instance.Instantiate("DragObject.prefab");

        // DragObject �ʱ�ȭ
        DemoDragObject dragObject = activeDragObject.GetComponent<DemoDragObject>();
        if (dragObject != null)
        {
            dragObject.Initialize();
            dragObject.SetUpUnitData(dragObjectAddress);

            dragObject.TESTOnPointerDown();
        }

        // ī�� �̹����� ����
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
