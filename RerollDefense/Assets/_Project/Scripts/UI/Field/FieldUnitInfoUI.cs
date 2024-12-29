using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FieldUnitInfoUI : PopupBase
{


    //클릭했을때 해당 tileShape가지고 정보 넣어줘야됨, tileUniqueID있음
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

        //unitStat만큼 스탯생성해서 넣어주기

        // 기존에 생성된 infoObject 정리
        ClearInfoObjects();

        // 스탯 개수만큼 infoObject 생성
        var stats = unitObject.GetStats(); // UnitController에서 스탯 정보를 가져옴

        foreach (var stat in stats)
        {
            CreateInfoObject(stat.Key, stat.Value);
        }

    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // 터치 또는 클릭 위치가 이미지 외부인지 확인
            if (!IsPointerInsidePopup())
            {
                UIManager.Instance.CloseUI<FieldUnitInfoUI>();
            }
        }
    }

    private void CreateInfoObject(string statName, int statValue)
    {
        // infoObject 복제
        UnitInfoComponent newInfoObject = Instantiate(infoObject, infoLayout);
        newInfoObject.gameObject.SetActive(true);

        // 스탯 이름과 값 설정
        newInfoObject.SetStatInfo(statName, statValue);
    }

    private void ClearInfoObjects()
    {
        // infoContainer의 모든 자식 오브젝트 제거
        foreach (Transform child in infoLayout)
        {
            Destroy(child.gameObject);
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
        UnitManager.Instance.RemoveUnitsByTileID(unitObject);
        UIManager.Instance.CloseUI<FieldUnitInfoUI>();

    }

    public override void CloseUI()
    {
        base.CloseUI();
    }



}
