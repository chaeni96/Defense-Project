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

    // ���� ���õ� ���� ������ ���� �̺�Ʈ �ý���
    public static event System.Action<UnitController> OnUnitSelected;

    public event System.Action OnSwitchToCharacterTab;

    private void Awake()
    {
        // �ʱ� ���¿����� ��Ȱ��ȭ
        gameObject.SetActive(false);
    }

    public void InitilazeCharacterInfo()
    {
        // UI �ʱ�ȭ �� �̺�Ʈ ����
        UnitController.OnUnitClicked += ShowUnitInfo;
    }

    public void ClearCharacterInfo()
    {
        // �̺�Ʈ ���� ����
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

        // ĳ���� ������ ��ȯ �̺�Ʈ ȣ��
        OnSwitchToCharacterTab?.Invoke();

        // ���� ���� ����
        clickUnit = unit;
        unitData = unit.unitData;

        // UI Ȱ��ȭ
        gameObject.SetActive(true);

        // ���� ������ ����
        if (unitRTObject != null)
        {
            // PerObjectRTSource ������Ʈ�� ���� ���� ������Ʈ ����
            PerObjectRTSource rtSource = unit.GetComponent<PerObjectRTSource>();

            // �ʿ��� ��� �ڽ� ������Ʈ�� ����
            // ���⼭�� ������ �ҽ��� ����
            unitRTObject.source = rtSource;
        }

        // ���� �̸� ����
        if (unitNameText != null && unitData != null)
        {
            unitNameText.text = unitData.f_name;
        }

        // ���� ������ ���� ����
        IEquipmentSystem equipSystem = InventoryManager.Instance.GetEquipmentSystem();
        if (equipSystem != null)
        {
            unequipBtn.interactable = true;
            equipData = equipSystem.GetEquippedItem(clickUnit);

            if (equipData != null)
            {
                equipItemIcon.gameObject.SetActive(true);

                // ������ ������ ����
                //if (equipData.f_iconImage != null)
                //    equipItemIcon.sprite = equipData.f_iconImage;

                // ������ �̸� ����
                if (equipItemNameText != null)
                    equipItemNameText.text = equipData.f_name;

                // ������ ���� ����
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
                // ������ �������� ���� ���
                equipItemIcon.gameObject.SetActive(false);

                unequipBtn.interactable = false; // ��� ���� ��ư ��Ȱ��ȭ   

                if (equipItemNameText != null)
                    equipItemNameText.text = "";
                if (equipItemStatText != null)
                    equipItemStatText.text = "";
            }
        }

        // �Ǹ� ���� ����
        UpdateSaleCost();

        // �̺�Ʈ �߻�
        OnUnitSelected?.Invoke(unit);
    }

    // �� ��ȯ �� ĳ���� ���� ����� �޼��� �߰�
    public void HideCharacterInfo()
    {
        // ĳ���� ���� �г� ��Ȱ��ȭ
        gameObject.SetActive(false);

        // ���õ� ���� ���� �ʱ�ȭ
        clickUnit = null;
        unitData = null;
        equipData = null;

        // UI ��� �ʱ�ȭ
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
            // ��� ����
            bool success = equipSystem.UnequipItem(clickUnit);

            if (success)
            {
                // UI ������Ʈ
                ShowUnitInfo(clickUnit);

            }
        }
    }


    public void OnClickSaleUnitBtn()
    {
        if (clickUnit == null) return;

   
        // ���� ���� �� �ڽ�Ʈ ��ȯ
        clickUnit.DeleteUnit();

        // UI ��Ȱ��ȭ
        gameObject.SetActive(false);
        clickUnit = null;

      
    }

    public void CloseInfo()
    {
        gameObject.SetActive(false);
        clickUnit = null;
    }

}
