using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UnitInfoComponent : MonoBehaviour
{
    [SerializeField] private TMP_Text unitStatText;
    [SerializeField] private TMP_Text statValueText;


    public void SetStatInfo(string stat, int value)
    {

        unitStatText.text = stat;
        statValueText.text = value.ToString();  
        
    }



}
