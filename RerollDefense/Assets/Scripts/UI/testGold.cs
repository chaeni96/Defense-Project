using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class testGold : MonoBehaviour
{
    //�׽�Ʈ��
    public Text goldText;

    private void Start()
    {
        UpdateGoldText();
    }

    public void UpdateGoldText()
    {
        // �ؽ�Ʈ UI ������Ʈ
        if (goldText != null)
        {
            //goldText.text = $"Gold: {userData.f_Gold}";
        }
    }




}
