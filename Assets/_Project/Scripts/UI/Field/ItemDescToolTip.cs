using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;


[UIInfo("ItemDescToolTip", "ItemDescToolTip", false)]

public class ItemDescToolTip : FloatingPopupBase
{
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TMP_Text itemName;
    [SerializeField] private TMP_Text descriptionText;


    private float descriptionDuration = 1f;


    public override void InitializeUI()
    {
        base.InitializeUI();
    }

    public void InitializeItemDescUI(D_ItemData item)
    {

        itemName.text = item.f_name;

        if (descriptionText != null)
        {
            string statText = "";
            foreach (var stat in item.f_stats)
            {
                statText += $"{stat.f_statName}: +{stat.f_statValue}\n";
                if (stat.f_valueMultiply != 1f)
                    statText += $"x{stat.f_valueMultiply}\n";
            }
            descriptionText.text = statText;
        }

        descriptionPanel.SetActive(true);

        // 기존 트윈이 있다면 중지
        DOTween.Kill(descriptionPanel);


        DOVirtual.DelayedCall(descriptionDuration, () => {
            HideUI();
        }).SetId(descriptionPanel);

    }

    public override void HideUI()
    {
        base.HideUI();
    }

}

