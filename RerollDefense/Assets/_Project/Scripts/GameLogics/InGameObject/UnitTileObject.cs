using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

using BGDatabaseEnum;

public class UnitTileObject : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private List<Image> tileImages = new List<Image>();
    [SerializeField] private Image backgroundImage; // 전체 영역을 덮는 이미지

    public TMP_Text cardCostText;

    private GameObject activeDragObject;
    private string dragObjectAddress;
    private int cardCost;

    private void Awake()
    {
        // 배경 이미지가 레이캐스트를 받을 수 있도록 설정
        backgroundImage.raycastTarget = true;

        // 타일 이미지들은 레이캐스트 무시
        foreach (var tileImage in tileImages)
        {
            tileImage.raycastTarget = false;
        }
    }

    public void InitializeCardInform(D_TileShpeData unitData)
    {
        dragObjectAddress = unitData.f_name;
        cardCost = unitData.f_Cost;
        cardCostText.text = cardCost.ToString();

        UpdateCostTextColor(); // 초기 코스트 텍스트 색상 설정

        CreateTilePreview(unitData);
    }

    private void UpdateCostTextColor()
    {
        if (GameManager.Instance.CurrentCost >= cardCost)
        {
            cardCostText.color = Color.white;
        }
        else
        {
            cardCostText.color = Color.red;
        }
    }

    private void Update()
    {
        UpdateCostTextColor(); // 코스트 변경될 때마다 색상 업데이트, 이거 게임매니저에서 addCost할때 이벤트 구독하면됨 추후 수정
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GameManager.Instance.CurrentCost < cardCost)
        {
            return;
        }

        activeDragObject = PoolingManager.Instance.GetObject("DragObject");

        activeDragObject.transform.position = eventData.position;

        DragObject dragObject = activeDragObject.GetComponent<DragObject>();
        if (dragObject != null)
        {
            Vector3 pointerPosition = GameManager.Instance.mainCamera.ScreenToWorldPoint(eventData.position);
            pointerPosition.z = 0;

            dragObject.Initialize();
            dragObject.OnClickObject(dragObjectAddress, pointerPosition);
        }

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (activeDragObject == null)
        {
            return;
        }

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
                Destroy(activeDragObject);
            }
            else
            {
                GameObject cardDeck = this.transform.parent.gameObject;
                FullWindowInGameDlg dlg = cardDeck.GetComponentInParent<FullWindowInGameDlg>();
                if (dlg != null)
                {
                    dlg.OnUnitCardDestroyed(cardDeck);
                    GameManager.Instance.UseCost(cardCost);
                }

                Destroy(this.gameObject);
            }
        }
    }

    private void CreateTilePreview(D_TileShpeData tileShapeData)
    {
        foreach (var image in tileImages)
        {
            image.gameObject.SetActive(false);
        }

        foreach (var buildData in tileShapeData.f_unitBuildData)
        {
            Vector2 tilePos = buildData.f_TilePos.f_TilePos;

            // 중앙(인덱스 4)을 (0,0)으로 보고 계산
            int x = Mathf.RoundToInt(tilePos.x) + 1;  
            int y = Mathf.RoundToInt(-tilePos.y) + 1; 
            int index = y * 3 + x;

            if (index >= 0 && index < tileImages.Count)
            {
                GameObject tempUnit = PoolingManager.Instance.GetObject(buildData.f_unitData.f_unitPrefabKey);
                UnitController unitController = tempUnit.GetComponent<UnitController>();

                if (unitController != null && unitController.unitSprite != null)
                {
                    tileImages[index].gameObject.SetActive(true);

                    if (buildData.f_unitData.f_SkillAttackType != SkillAttackType.None)
                    {
                        tileImages[index].sprite = unitController.unitSprite.sprite;
                    }
                    
                }

                PoolingManager.Instance.ReturnObject(tempUnit);
            }
        }
    }

    private void OnDestroy()
    {
        if (activeDragObject != null)
        {
            Destroy(activeDragObject);
        }
    }
}