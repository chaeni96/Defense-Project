using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[UIInfo("InGameCountdownUI", "InGameCountdownUI", false)]
public class InGameCountdownUI : FloatingPopupBase
{
    [SerializeField] private TMP_Text countDownText;

    public void UpdateCountdown(float remainTime)
    {
        countDownText.text = Mathf.CeilToInt(remainTime).ToString();
    }
}
