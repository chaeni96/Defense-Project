using System.Collections;
using System.Collections.Generic;
using CatDarkGame.PerObjectRTRenderForUGUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class CharacterInfo : MonoBehaviour
{
    [SerializeField] private PerObjectRTRenderer unitRTObject;

    [SerializeField] private TMP_Text unitNameText;

    [SerializeField] private Image equipItemIcon;
    [SerializeField] private TMP_Text equipItemNameText;
    [SerializeField] private TMP_Text equipItemStatText;

    [SerializeField] private TMP_Text unitSaleCostText;

    [SerializeField] private Button unequipBtn;

    private UnitController clickUnit;


    private D_UnitData unitData;
    private D_ItemData equipData;

    // 현재 선택된 유닛 관리를 위한 이벤트 시스템
    public static event System.Action<UnitController> OnUnitSelected;

    public event System.Action OnSwitchToCharacterTab;

    private void Awake()
    {
        // 초기 상태에서는 비활성화
        gameObject.SetActive(false);
    }

    public void InitilazeCharacterInfo()
    {
        // UI 초기화 및 이벤트 구독
        UnitController.OnUnitClicked += ShowUnitInfo;
    }

    public void ClearCharacterInfo()
    {
        // 이벤트 구독 해제
        UnitController.OnUnitClicked -= ShowUnitInfo;
 
    }


    public void ShowUnitInfo(UnitController unit)
    {

        if (unit == null)
        {
            gameObject.SetActive(false);
            clickUnit = null;
            return;
        }

        // 캐릭터 탭으로 전환 이벤트 호출
        OnSwitchToCharacterTab?.Invoke();

        // 유닛 정보 저장
        clickUnit = unit;
        unitData = unit.unitData;

        // UI 활성화
        gameObject.SetActive(true);

        // 유닛 렌더링 설정
        if (unitRTObject != null)
        {
            // PerObjectRTSource 컴포넌트를 가진 게임 오브젝트 생성
            PerObjectRTSource rtSource = unit.GetComponent<PerObjectRTSource>();

            // 필요한 경우 자식 오브젝트도 복사
            // 여기서는 간단히 소스만 설정
            unitRTObject.source = rtSource;
        }

        // 유닛 이름 설정
        if (unitNameText != null && unitData != null)
        {
            unitNameText.text = unitData.f_name;
        }

        // 장착 아이템 정보 설정
        IEquipmentSystem equipSystem = InventoryManager.Instance.GetEquipmentSystem();
        if (equipSystem != null)
        {
            unequipBtn.interactable = true;
            equipData = equipSystem.GetEquippedItem(clickUnit);

            if (equipData != null)
            {
                equipItemIcon.gameObject.SetActive(true);

                // 아이템 아이콘 설정
                //if (equipData.f_iconImage != null)
                //    equipItemIcon.sprite = equipData.f_iconImage;

                // 아이템 이름 설정
                if (equipItemNameText != null)
                    equipItemNameText.text = equipData.f_name;

                // 아이템 스탯 설정
                if (equipItemStatText != null)
                {
                    string statText = "";
                    foreach (var stat in equipData.f_stats)
                    {
                        statText += $"{stat.f_statName}: +{stat.f_statValue}\n";
                        if (stat.f_valueMultiply != 1f)
                            statText += $"x{stat.f_valueMultiply}\n";
                    }
                    equipItemStatText.text = statText;
                }
            }
            else
            {
                // 장착된 아이템이 없을 경우
                equipItemIcon.gameObject.SetActive(false);

                unequipBtn.interactable = false; // 장비 해제 버튼 비활성화   

                if (equipItemNameText != null)
                    equipItemNameText.text = "";
                if (equipItemStatText != null)
                    equipItemStatText.text = "";
            }
        }

        // 판매 가격 설정
        UpdateSaleCost();

        // 이벤트 발생
        OnUnitSelected?.Invoke(unit);
    }

    // 탭 전환 시 캐릭터 정보 숨기는 메서드 추가
    public void HideCharacterInfo()
    {
        // 캐릭터 정보 패널 비활성화
        gameObject.SetActive(false);

        // 선택된 유닛 정보 초기화
        clickUnit = null;
        unitData = null;
        equipData = null;

        // UI 요소 초기화
        if (unitNameText != null)
            unitNameText.text = "";

        if (equipItemIcon != null)
            equipItemIcon.gameObject.SetActive(false);

        if (equipItemNameText != null)
            equipItemNameText.text = "";

        if (equipItemStatText != null)
            equipItemStatText.text = "";

        if (unitSaleCostText != null)
            unitSaleCostText.text = "0";

        if (unequipBtn != null)
            unequipBtn.interactable = false;
    }

    private void UpdateSaleCost()
    {
        if (clickUnit == null || unitSaleCostText == null) return;

        int starLevel = (int)clickUnit.GetStat(StatName.UnitStarLevel);
        int saleValue = (clickUnit.unitType == UnitType.Base) ? 0 : (starLevel == 1) ? 1 : starLevel - 1;

        unitSaleCostText.text = $"{saleValue}";
    }

    public void OnClickUnEquipBtn()
    {
        if (clickUnit == null) return;

        IEquipmentSystem equipSystem = InventoryManager.Instance.GetEquipmentSystem();
        if (equipSystem != null && equipData != null)
        {
            // 장비 해제
            bool success = equipSystem.UnequipItem(clickUnit);

            if (success)
            {
                // UI 업데이트
                ShowUnitInfo(clickUnit);

            }
        }
    }


    public void OnClickSaleUnitBtn()
    {
        if (clickUnit == null) return;

   
        // 유닛 삭제 및 코스트 반환
        clickUnit.DeleteUnit();

        // UI 비활성화
        gameObject.SetActive(false);
        clickUnit = null;

      
    }

    public void CloseInfo()
    {
        gameObject.SetActive(false);
        clickUnit = null;
    }

}
