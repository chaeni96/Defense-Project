using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class testGold : MonoBehaviour
{
    //테스트용
    public Text goldText;

    private void Start()
    {
        UpdateGoldText();
    }

    public void UpdateGoldText()
    {
        // 텍스트 UI 업데이트
        if (goldText != null)
        {
            //goldText.text = $"Gold: {userData.f_Gold}";
        }
    }




}
