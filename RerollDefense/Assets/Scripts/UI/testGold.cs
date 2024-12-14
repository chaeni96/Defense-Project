using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class testGold : MonoBehaviour
{
    //테스트용
    public Text goldText;

    D_UserData userData;

    private void Start()
    {
        userData = D_UserData.GetEntity(0);
        UpdateGoldText();
    }

    public void UpdateGoldText()
    {
        // 텍스트 UI 업데이트
        if (goldText != null)
        {
            goldText.text = $"Gold: {userData.f_Gold}";
        }
    }




}
