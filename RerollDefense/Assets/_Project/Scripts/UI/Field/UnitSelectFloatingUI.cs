using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[UIInfo("UnitSelectFloatingUI", "UnitSelectFloatingUI", false)]
public class UnitSelectFloatingUI : PopupBase
{


    //Ŭ�������� �ش� tileShape������ ���� �־���ߵ�, tileUniqueID����
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
        
        //���� ��ǥ�� ��ũ�� ��ǥ�� ��ȯ 
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
            // ��ġ �Ǵ� Ŭ�� ��ġ�� �̹��� �ܺ����� Ȯ��
            if (!IsPointerInsidePopup())
            {
                UIManager.Instance.CloseUI<UnitSelectFloatingUI>();
            }
        }
    }


    private bool IsPointerInsidePopup()
    {
        // ���� ���콺 �Ǵ� ��ġ ��ġ
        Vector2 pointerPosition = Input.mousePosition;

        // RectTransform ���� �ȿ� �ִ��� Ȯ��
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
