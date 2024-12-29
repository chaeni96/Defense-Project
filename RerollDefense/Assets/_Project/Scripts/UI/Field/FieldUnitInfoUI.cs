using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FieldUnitInfoUI : PopupBase
{


    //Ŭ�������� �ش� tileShape������ ���� �־���ߵ�, tileUniqueID����
    [SerializeField] private TMP_Text unitNameText;
    [SerializeField] private Image unitImage;
    [SerializeField] private RectTransform imgRect;

    [SerializeField] private UnitInfoComponent infoObject;
    [SerializeField] private RectTransform infoLayout; 

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

        unitImage.sprite = unit.unitSprite.sprite;

        //unitStat��ŭ ���Ȼ����ؼ� �־��ֱ�

        // ������ ������ infoObject ����
        ClearInfoObjects();

        // ���� ������ŭ infoObject ����
        var stats = unitObject.GetStats(); // UnitController���� ���� ������ ������

        foreach (var stat in stats)
        {
            CreateInfoObject(stat.Key, stat.Value);
        }

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // ��ġ �Ǵ� Ŭ�� ��ġ�� �̹��� �ܺ����� Ȯ��
            if (!IsPointerInsidePopup())
            {
                UIManager.Instance.CloseUI<FieldUnitInfoUI>();
            }
        }
    }

    private void CreateInfoObject(string statName, int statValue)
    {
        // infoObject ����
        UnitInfoComponent newInfoObject = Instantiate(infoObject, infoLayout);
        newInfoObject.gameObject.SetActive(true);

        // ���� �̸��� �� ����
        newInfoObject.SetStatInfo(statName, statValue);
    }

    private void ClearInfoObjects()
    {
        // infoContainer�� ��� �ڽ� ������Ʈ ����
        foreach (Transform child in infoLayout)
        {
            Destroy(child.gameObject);
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
        UnitManager.Instance.RemoveUnitsByTileID(unitObject);
        UIManager.Instance.CloseUI<FieldUnitInfoUI>();

    }

    public override void CloseUI()
    {
        base.CloseUI();
    }



}
