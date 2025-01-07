using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[UIInfo("UnitSelectFloatingUI", "UnitSelectFloatingUI", false)]
public class UnitSelectFloatingUI : PopupBase
{


    //클릭했을때 해당 tileShape가지고 정보 넣어줘야됨, tileUniqueID있음
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private RectTransform imgRect;

    private UnitController unitObject;
    private Camera uiCamera;


    public override void InitializeUI()
    {
        base.InitializeUI();

        Canvas canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas.worldCamera;
    }

    public void InitUnitInfo(UnitController unit)
    {

        unitObject = unit;

        unitNameText.text = unit.name;
        
        //월드 좌표를 스크린 좌표로 전환 
        Vector2 screenPos = GameManager.Instance.mainCamera.WorldToScreenPoint(unit.transform.position);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle( (RectTransform)transform.parent, screenPos, uiCamera,out Vector2 localPoint))
        {
            ((RectTransform)transform).anchoredPosition = localPoint;
        }


    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 터치 또는 클릭 위치가 이미지 외부인지 확인
            if (!IsPointerInsidePopup())
            {
                UIManager.Instance.CloseUI<UnitSelectFloatingUI>();
            }
        }
    }


    private bool IsPointerInsidePopup()
    {
        // 현재 마우스 또는 터치 위치
        Vector2 pointerPosition = Input.mousePosition;

        // RectTransform 영역 안에 있는지 확인
        return RectTransformUtility.RectangleContainsScreenPoint(imgRect, pointerPosition, uiCamera);
    }

    public void OnClickDeleteTile()
    {
        UnitManager.Instance.UnregisterUnit(unitObject);
        UIManager.Instance.CloseUI<UnitSelectFloatingUI>();


    }

    public override void HideUI()
    {
        base.HideUI();
    }



}
